using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core.CitationProtection;
using AcademicParaphraser.Core.RuleEngine;

namespace AcademicParaphraser.Core.Rewriting
{
    public interface IRewriteModel
    {
        Task<IRewriteSession> OpenAsync(CancellationToken cancellation);
    }
    public interface IRewriteSession : IDisposable
    {
        Task<IReadOnlyDictionary<string, string>> RewriteAsync(RewritePlan plan, Strength strength, string feedback, CancellationToken cancellation);
        Task<MeaningReview> ReviewAsync(string source, string target, CancellationToken cancellation);
    }
    public sealed class MeaningReview
    {
        public bool Equivalent { get; set; }
        public bool Natural { get; set; }
        public bool AddedInformation { get; set; }
        public bool OmittedInformation { get; set; }
        public bool ActorsChanged { get; set; }
        public bool NegationChanged { get; set; }
        public bool CausalityChanged { get; set; }
        public bool CertaintyChanged { get; set; }
        public string Reasons { get; set; } = "";
        public bool Accepted => Equivalent && Natural && !AddedInformation && !OmittedInformation
            && !ActorsChanged && !NegationChanged && !CausalityChanged && !CertaintyChanged;
    }
    public sealed class RewriteDiagnostics
    {
        public int Paragraphs { get; set; }
        public int Rewritten { get; set; }
        public int Rejected { get; set; }
        public List<string> Reasons { get; } = new List<string>();
        public List<string> AttemptedTexts { get; } = new List<string>();
    }
    // Generation and inspection are separate passes. Neither the small model nor these
    // deterministic guards establish semantic equivalence; Word always requires preview.
    public sealed class LocalRewriteEngine
    {
        private readonly IRuleCatalog catalog;
        private readonly ITurkishNlp nlp;
        private readonly IRewriteModel model;
        private readonly ILexicalSource? knowledge;
        private readonly IStructureSource? structures;
        public LocalRewriteEngine(IRuleCatalog catalog, ITurkishNlp nlp, IRewriteModel model, ILexicalSource? knowledge = null,IStructureSource? structures=null)
        { this.catalog = catalog; this.nlp = nlp; this.model = model; this.knowledge = knowledge; this.structures=structures; }

        public async Task<IReadOnlyList<Candidate>> GenerateAsync(string source, UserSettings settings,
            IEnumerable<TextSpan> writable, IEnumerable<TextSpan> external, CancellationToken cancellation,
            IProgress<string>? progress = null, RewriteDiagnostics? diagnostics = null)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Length > 80000)
                throw new ArgumentException("Önce en fazla 80.000 karakterlik bir metin seçin.");
            diagnostics = diagnostics ?? new RewriteDiagnostics();
            var allowed = writable.ToList();
            var blocked = new ProtectionDetector().Detect(source, settings, catalog.GetLockedTerms(), catalog.GetLexicon(), external).ToList();
            // Quantities include their unit; certainty/scope words are fixed anchors.
            blocked.AddRange(FactGuard.Anchors(source));
            var enrichment = new EnrichmentContext { Progress = progress };
            var paragraphs = Regex.Matches(source, @"[^\r\n\a\v\f]+", RegexOptions.None, TimeSpan.FromMilliseconds(150)).Cast<Match>().ToList();
            var edits = new List<TextEdit>();
            int rewrittenSentences = 0;
            progress?.Report("Gelişmiş yerel motor hazırlanıyor; metin bilgisayarınızda kalır…");
            IRewriteSession? session = null;
            try
            {
                foreach (var paragraph in paragraphs)
                {
                    cancellation.ThrowIfCancellationRequested();
                    diagnostics.Paragraphs++;
                    if (paragraph.Length > 2400)
                    {
                        diagnostics.Reasons.Add("Uzun paragraf atlandı: en fazla 2.400 karakterlik paragraflar seçin.");
                        continue;
                    }
                    string text = paragraph.Value;
                    IReadOnlyList<LexiconEntry> lexical = Array.Empty<LexiconEntry>();
                    bool english = LanguageProfile.IsEnglish(text, settings.InputLanguage);
                    IReadOnlyList<MorphToken> before = english ? Array.Empty<MorphToken>() : await nlp.AnalyzeAsync(text, cancellation).ConfigureAwait(false);
                    var protectedSpans = Clip(blocked, paragraph.Index, paragraph.Length);
                    if (settings.PreserveNames)
                        protectedSpans.AddRange(before.Where(t => t.Proper && !t.AmbiguousProper && t.Start >= 0 && t.Start + t.Length <= text.Length)
                            .Select(t => new TextSpan { Start = t.Start, Length = t.Length, Reason = "özel isim" }));
                    if (knowledge != null && !english)
                    {
                        var eligible = before.Where(t => !t.Proper && !protectedSpans.Any(p => p.Intersects(t.Start,t.Length))).ToList();
                        lexical = await knowledge.FindAsync(text, eligible, settings, cancellation, enrichment).ConfigureAwait(false);
                        if (settings.PreserveTechnicalTerms)
                            protectedSpans = new ProtectionDetector().Detect(text, settings, Array.Empty<string>(), lexical, protectedSpans).ToList();
                    }
                    if (settings.PreserveNames && english) protectedSpans.AddRange(LanguageProfile.EnglishNames(text));
                    if(english) protectedSpans.AddRange(Regex.Matches(text,@"\b(?:survey|study|authors|researchers|team|committee|panel|commission|participants|students|patients)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)
                        .Cast<Match>().Select(m=>new TextSpan{Start=m.Index,Length=m.Length,Reason="eyleyen/veri kaynağı"}));
                    var plan = RewritePlan.Create(text, Clip(allowed, paragraph.Index, paragraph.Length), protectedSpans);
                    plan.Language = english ? "en" : "tr";
                    plan.Analysis = SentenceFrame.Analyze(text, before);
                    if(text.Length<=1000)
                        plan.DictionaryClues=lexical.Where(e=>e.Examples.Count==1&&e.Confidence>=.93).Take(6)
                            .GroupBy(e=>e.Lemma).ToDictionary(g=>g.Key,g=>g.First().Examples[0].Substring(0,Math.Min(160,g.First().Examples[0].Length)));
                    if (!plan.Blocks.Any(b => b.Editable)) continue;
                    if (plan.Blocks.Count > 80) { diagnostics.Reasons.Add("Çok sayıda biçim/koruma sınırı bulunan paragraf atlandı."); continue; }
                    if(!english && plan.CanCompileNaturalText)
                    {
                        // Deterministic syntax is a scaffold, never an unreviewed answer.
                        // It is local, does not do thesaurus substitutions, and must fit
                        // the same protected Word ranges before the model can use it.
                        var seedSettings=new UserSettings { DefaultStrength=settings.DefaultStrength,Domain=settings.Domain,
                            Alternatives=1,MinimumConfidence=settings.MinimumConfidence,EnableWordChoice=false,
                            PreserveNames=settings.PreserveNames,PreserveNumbers=settings.PreserveNumbers,
                            PreserveLinks=settings.PreserveLinks,PreserveCitations=settings.PreserveCitations,
                            PreserveTechnicalTerms=settings.PreserveTechnicalTerms,CustomProtectionPatterns=settings.CustomProtectionPatterns };
                        seedSettings.InternetEnabled=settings.InternetEnabled;seedSettings.OfflineMode=settings.OfflineMode;
                        foreach(var seed in await new TransformationEngine(catalog,nlp,null,structures).GenerateAsync(text,seedSettings,protectedSpans,cancellation,enrichment).ConfigureAwait(false))
                        {
                            if(seed.Edits.Count==0)continue;
                            try { plan.CompileNaturalText(seed.Text);plan.StructuralScaffold=seed.Text;break; }
                            catch(ArgumentException){ }
                        }
                    }
                    if (session == null) session = await model.OpenAsync(cancellation).ConfigureAwait(false);
                    string feedback = "";
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        progress?.Report($"Paragraf {diagnostics.Paragraphs}/{paragraphs.Count}: {(attempt == 0 ? "cümle ve paragraf yeniden kuruluyor" : "denetim sonucuna göre yeniden yazılıyor")}…");
                        Candidate candidate;
                        try
                        {
                            var replacements = await session.RewriteAsync(plan, settings.DefaultStrength, feedback, cancellation).ConfigureAwait(false);
                            candidate = plan.BuildCandidate(replacements, "local-model:structured-paragraph");
                        }
                        catch (ArgumentException ex)
                        {
                            feedback = (plan.CanCompileNaturalText
                                ? "Yanıt Word metin sınırlarına uymadı. Düz metin paragrafını paragraph adlı JSON alanında döndür. Korunan gerçek metinleri aynen ve aynı sırada tut; yeni işaret/başlık ekleme. "
                                : "Yanıt Word koruma işaretlerine uymadı. Paragrafı bir bütün olarak döndür; [[AP_B0000]] biçimindeki işaretleri eksiksiz, bire bir ve aynı sırada koru. İşaretin temsil ettiği asıl metni tekrar yazma. ") + "Denetim: " + ex.Message;
                            Reject(diagnostics, feedback); continue;
                        }
                        if (candidate.Edits.Count == 0)
                        { feedback = "Metin değişmedi. Anlamı koruyarak cümle kuruluşunu değiştir."; Reject(diagnostics, feedback); continue; }
                        diagnostics.AttemptedTexts.Add(candidate.Text);
                        var problems = FactGuard.Check(text, candidate.Text);
                        problems.AddRange(plan.CheckFixedTextMultiplicity(candidate.Text));
                        if (LanguageProfile.HasClearLanguageChange(candidate.Text, english))
                            problems.Add("Kaynağın dili değiştirildi; aynı dilde yeniden yazılmalı.");
                        IReadOnlyList<MorphToken> after = english ? Array.Empty<MorphToken>() : await nlp.AnalyzeAsync(candidate.Text, cancellation).ConfigureAwait(false);
                        problems.AddRange(FactGuard.CheckPredicateFeatures(before,after));
                        problems.AddRange(FactGuard.CheckObjects(text,candidate.Text,before,after,lexical));
                        problems.AddRange(FactGuard.CheckEmbeddedFutureSubjects(text,candidate.Text,before,after));
                        if (!StructuralDiversity.HasReconstruction(text, candidate.Text) || !StructuralDiversity.HasSyntaxChange(text, candidate.Text, before, after))
                        { feedback = "Öneri çoğunlukla kelime değişiminden ibaret. Cümlenin öğelerinin sırasını, yan cümle kuruluşunu veya isim/fiil anlatımını anlamı değiştirmeden yeniden düzenle. Sadece yüklemin eş anlamlısını kullanma."; Reject(diagnostics, feedback); continue; }
                        // A new first/second-person actor is not licensed by a third-person source.
                        if (!HasPersonalActor(before) && HasPersonalActor(after)) problems.Add("Yeni bir birinci/ikinci kişi öznesi eklendi.");
                        if (problems.Count > 0)
                        { feedback = string.Join(" ", problems); Reject(diagnostics, feedback); continue; }
                        progress?.Report($"Paragraf {diagnostics.Paragraphs}/{paragraphs.Count}: yeni bilgi, eksiltme ve anlam kayması denetleniyor…");
                        MeaningReview review;
                        try { review = await session.ReviewAsync(text, candidate.Text, cancellation).ConfigureAwait(false); }
                        catch (ArgumentException) { feedback = "Anlam denetimi yanıtı tamamlanamadı. Daha kısa ve açık bir yeniden yazım yap."; Reject(diagnostics, feedback); continue; }
                        if (!review.Accepted)
                        { feedback = "Önceki öneri anlam/doğallık denetiminden geçmedi: " + review.Reasons; Reject(diagnostics, feedback); continue; }
                        foreach (var edit in candidate.Edits) { edit.Start += paragraph.Index; edits.Add(edit); }
                        diagnostics.Rewritten++;
                        rewrittenSentences += SentenceSegmentation.Find(text, protectedSpans).Count(s => candidate.Text.IndexOf(text.Substring(s.Start,s.Length).Trim(),StringComparison.OrdinalIgnoreCase)<0);
                        break;
                    }
                }
            }
            finally { session?.Dispose(); }
            if (edits.Count == 0) return new List<Candidate>();
            var result = new Candidate { Text = EditApplication.Apply(source, edits), Edits = edits,
                SentenceCount = SentenceSegmentation.Find(source, blocked).Count, RewrittenSentences = rewrittenSentences,
                Enrichment = enrichment.Summary,
                ReviewNote = $"Yerel model + sayı/birim/özne denetimi + ayrı anlam incelemesi. {diagnostics.Rewritten}/{diagnostics.Paragraphs} paragraf yeniden yazıldı; {diagnostics.Rejected} öneri elendi. Anlam garantisi değildir." };
            // A paragraph is accepted atomically; no individual edit is discarded here.
            if (!CandidateIntegrity.IsApplicable(source, result, e => allowed.Any(a => e.Start >= a.Start && e.Start + e.Length <= a.End)
                && !blocked.Any(p => p.Intersects(e.Start, e.Length))))
                throw new InvalidOperationException("Öneri Word sınırlarını aşmaktadır; belge değiştirilmedi.");
            return new[] { result };
        }
        private static List<TextSpan> Clip(IEnumerable<TextSpan> spans, int start, int length) => spans
            .Where(s => s.Intersects(start, length)).Select(s => new TextSpan { Start = Math.Max(start, s.Start) - start,
                Length = Math.Min(start + length, s.End) - Math.Max(start, s.Start), Reason = s.Reason }).ToList();
        private static bool HasPersonalActor(IEnumerable<MorphToken> tokens) => tokens.Any(t => !t.AmbiguousPersonal && t.Morphemes
            .Any(m => m == "A1sg" || m == "A1pl" || m == "A2sg" || m == "A2pl" || m == "P1sg" || m == "P1pl" || m == "P2sg" || m == "P2pl")
            && !t.Morphemes.Contains("Imp") && (t.Pos == "Verb" || t.Morphemes.Contains("P1sg") || t.Morphemes.Contains("P1pl")));
        private static void Reject(RewriteDiagnostics d, string reason) { d.Rejected++; d.Reasons.Add(reason); }
    }
    public static class FactGuard
    {
        private static readonly Regex Numbers = new Regex(@"\d+(?:[.,:/–\-]\d+)*", RegexOptions.None, TimeSpan.FromMilliseconds(150));
        private static readonly Regex Anchored = new Regex(@"\b\d+(?:[.,]\d+)?\s*(?:%|°[CF]|kN|MPa|GHz|MHz|GB|MB|kg|mg|km|cm|mm|ms|m²|m³|m3|m|s|gün|hafta|ay|yıl|days?|weeks?|months?|years?)(?![\p{L}\p{N}])|\b(?:yalnızca|sadece|henüz|mutlaka|olabilir|muhtemelen|kesinlikle|only|possibly|may|might|must|if|unless)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(150));
        private static readonly Regex[] NegativeEvidence = new[] {
            @"\bgöster(?:mem|medi|miyor|mez)[\p{L}]*\b", @"\bkanıtla(?:mam|madı|mıyor|maz)[\p{L}]*\b", @"\bdoğrula(?:mam|madı|mıyor|maz)[\p{L}]*\b"
        }.Select(p=>new Regex(p,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(150))).ToArray();
        public static IEnumerable<TextSpan> Anchors(string text) => Anchored.Matches(text).Cast<Match>()
            .Select(m => new TextSpan { Start = m.Index, Length = m.Length, Reason = "miktar/birim/kapsam" });
        public static List<string> Check(string source, string target)
        {
            var problems = new List<string>();
            var a = Numbers.Matches(source).Cast<Match>().Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal);
            var b = Numbers.Matches(target).Cast<Match>().Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal);
            if (!a.SequenceEqual(b)) problems.Add("Sayısal veri eklendi, eksildi veya değişti.");
            foreach(var evidence in NegativeEvidence)
                if(evidence.Matches(source).Count!=evidence.Matches(target).Count)
                    problems.Add("Olumsuz kanıt yüklemi başka bir iddia/söylem fiiline dönüştü veya eksildi.");
            // A duplicate protected quantity/scope cue may be introduced in another writable block.
            var anchors = Anchored.Matches(source).Cast<Match>().Select(m => m.Value).Distinct(StringComparer.Ordinal);
            foreach (string anchor in anchors)
                if (Regex.Matches(source, Regex.Escape(anchor), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count != Regex.Matches(target, Regex.Escape(anchor), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count)
                    problems.Add("Birim veya kapsam ifadesi tekrarlandı/değişti: " + anchor);
            return problems;
        }
        public static IReadOnlyList<string> CheckPredicateFeatures(IReadOnlyList<MorphToken> before,IReadOnlyList<MorphToken> after)
        {
            // Only compare the same verb root when it has one predicate occurrence on
            // each side. This is morphological evidence, not argument/role alignment.
            bool Predicate(MorphToken t)=>!t.Morphemes.Contains("Imp")&&!t.RuntimeGuess&&!t.Proper
                &&(t.Pos=="Verb"||t.Morphemes.Contains("PastPart")||t.Morphemes.Contains("FutPart")||t.Morphemes.Contains("Inf1")||t.Morphemes.Contains("Inf2")||t.Morphemes.Contains("Inf3"));
            var source=before.Where(Predicate).GroupBy(t=>t.Lemma).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.First());
            var target=after.Where(Predicate).GroupBy(t=>t.Lemma).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.First());
            var problems=new List<string>();
            foreach(var pair in source.Where(p=>target.ContainsKey(p.Key)))
            {
                var a=pair.Value.Morphemes;var b=target[pair.Key].Morphemes;
                if(a.Contains("Neg")!=b.Contains("Neg"))problems.Add("Aynı eylemin olumsuzluğu değişti: "+pair.Key);
                if((a.Contains("Fut")||a.Contains("FutPart"))!=(b.Contains("Fut")||b.Contains("FutPart")))
                    problems.Add("Aynı eylemin gelecek zaman/anlam ilişkisi değişti: "+pair.Key);
                if(a.Contains("Neces")!=b.Contains("Neces"))problems.Add("Aynı eyleme gereklilik/öneri anlamı eklendi veya çıkarıldı: "+pair.Key);
            }
            return problems;
        }
        public static IReadOnlyList<string> CheckObjects(string source,string target,IReadOnlyList<MorphToken> before,IReadOnlyList<MorphToken> after,IReadOnlyList<LexiconEntry> lexical)
        {
            bool Finite(MorphToken t)=>t.Pos=="Verb"&&!t.Morphemes.Contains("Imp")&&!t.RuntimeGuess&&!t.Proper;
            var a=before.Where(Finite).GroupBy(t=>t.Lemma).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.First());
            var b=after.Where(Finite).GroupBy(t=>t.Lemma).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.First());
            var sourceSentences=SentenceSegmentation.Find(source,Array.Empty<TextSpan>());
            var targetSentences=SentenceSegmentation.Find(target,Array.Empty<TextSpan>());
            var problems=new List<string>();
            foreach(var pair in a.Where(p=>b.ContainsKey(p.Key)))
            {
                var original=sourceSentences.FirstOrDefault(s=>pair.Value.Start>=s.Start&&pair.Value.Start<s.End);
                var rewritten=targetSentences.FirstOrDefault(s=>b[pair.Key].Start>=s.Start&&b[pair.Key].Start<s.End);
                if(original==null||rewritten==null)continue;
                var words=after.Where(t=>t.Start>=rewritten.Start&&t.Start<rewritten.End).ToList();
                var originalWords=before.Where(t=>t.Start>=original.Start&&t.Start<original.End).ToList();
                if(pair.Value.Morphemes.Contains("Pass")&&!b[pair.Key].Morphemes.Contains("Pass")
                    &&!originalWords.Any(t=>t.Surface.Equals("tarafından",StringComparison.OrdinalIgnoreCase)||t.Morphemes.Contains("Equ")))
                    problems.Add("Eyleyeni belirtilmeyen edilgen cümleye etken bir eyleyen atandı: "+pair.Key);
                foreach(var obj in before.Where(t=>t.Start>=original.Start&&t.Start<original.End&&t.Pos=="Noun"&&t.Morphemes.Contains("Acc")&&!t.Morphemes.Contains("Verb")&&!t.RuntimeGuess))
                {
                    var aliases=lexical.Where(e=>e.Lemma==obj.Lemma&&e.Pos=="Noun"&&e.Confidence>=.93).SelectMany(e=>e.Synonyms);
                    if(!words.Any(t=>t.Lemma==obj.Lemma||aliases.Contains(t.Lemma)))
                        problems.Add("Aynı yüklemin belirtili nesnesi yeni cümlede kayboldu/değişti: "+obj.Lemma);
                }
            }
            return problems;
        }
        public static IReadOnlyList<string> CheckEmbeddedFutureSubjects(string source,string target,IReadOnlyList<MorphToken> before,IReadOnlyList<MorphToken> after)
        {
            var problems=new List<string>();
            var sourceSentences=SentenceSegmentation.Find(source,Array.Empty<TextSpan>());
            var targetSentences=SentenceSegmentation.Find(target,Array.Empty<TextSpan>());
            foreach(var predicate in before.Where(t=>t.Morphemes.Contains("FutPart")))
            {
                var targets=after.Where(t=>t.Lemma==predicate.Lemma&&t.Morphemes.Contains("FutPart")).ToList();
                if(targets.Count!=1)continue;
                var sentence=sourceSentences.FirstOrDefault(s=>predicate.Start>=s.Start&&predicate.Start<s.End);
                var rewritten=targetSentences.FirstOrDefault(s=>targets[0].Start>=s.Start&&targets[0].Start<s.End);
                if(sentence==null||rewritten==null)continue;
                var genitive=before.Where(t=>t.Start>=sentence.Start&&t.Start<predicate.Start&&t.Pos=="Noun"&&t.Morphemes.Contains("Gen")).OrderByDescending(t=>t.Start).FirstOrDefault();
                if(genitive==null||before.Any(t=>t.Start>genitive.Start&&t.Start<predicate.Start&&t.Morphemes.Contains("Verb")))continue;
                var arguments=after.Where(t=>t.Start>=rewritten.Start&&t.Start<rewritten.End&&t.Lemma==genitive.Lemma).ToList();
                if(arguments.Count==1&&!arguments[0].Morphemes.Contains("Gen"))
                    problems.Add("Gelecek zamanlı isim cümleciğinin tamlayan/özne eki kayboldu: "+genitive.Lemma);
            }
            return problems;
        }
    }
}

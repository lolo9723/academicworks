using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Core.RuleEngine;

namespace AcademicParaphraser.Core.Backends
{
    public sealed class RuleConstructionBackend : ISupplementalConstructionBackend
    {
        private readonly TransformationEngine engine;
        public RuleConstructionBackend(IRuleCatalog catalog,ITurkishNlp nlp,IStructureSource? structures=null)
        { engine=new TransformationEngine(catalog,nlp,null,structures); }

        public async Task<IReadOnlyList<Candidate>> GenerateAsync(BackendContext context,UserSettings settings,CancellationToken cancellation,EnrichmentContext enrichment)
        {
            if(context.Source.Language!="tr")return System.Array.Empty<Candidate>();
            // Word choice stays disabled even if enabled in another mode. This
            // producer supplies sentence constructions, not synonym substitutions.
            var structural=new UserSettings{DefaultStrength=settings.DefaultStrength,Domain=settings.Domain,
                MinimumConfidence=settings.MinimumConfidence,Alternatives=3,EnableWordChoice=false,
                PreserveNames=settings.PreserveNames,PreserveNumbers=settings.PreserveNumbers,
                PreserveCitations=settings.PreserveCitations,PreserveTechnicalTerms=settings.PreserveTechnicalTerms,
                PreserveLinks=settings.PreserveLinks,InternetEnabled=settings.InternetEnabled,OfflineMode=settings.OfflineMode,
                CacheDictionary=settings.CacheDictionary,CustomProtectionPatterns=settings.CustomProtectionPatterns.ToList()};
            var fixedSpans=context.Plan.Blocks.Where(b=>!b.Editable&&b.Text.Length>0)
                .Select(b=>new TextSpan{Start=b.Start,Length=b.Text.Length}).ToArray();
            var candidates=await engine.GenerateAsync(context.Source.Text,structural,fixedSpans,cancellation,enrichment).ConfigureAwait(false);
            return candidates.Where(c=>c.Edits.Count>0&&c.Text!=context.Source.Text)
                .Where(c=>!System.Text.RegularExpressions.Regex.IsMatch(c.Text,@"\bolarak,\s+(?:bu|mevcut|sunulan)\s+(?:çalışmada|araştırmada|incelemede)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase|System.Text.RegularExpressions.RegexOptions.CultureInvariant))
                .OrderByDescending(c=>c.RewrittenSentences).ThenByDescending(c=>c.Score.StructuralDifference).Take(3).ToArray();
        }
    }
}

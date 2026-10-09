using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AcademicParaphraser.Core.Rewriting;
namespace AcademicParaphraser.Core.Backends
{
    // Moves complete constituents identified by an actual dependency tree. No thesaurus.
    // A recorded operation is replayed independently by the meaning backend.
    public sealed class DependencyConstructionBackend : IConstructionBackend
    {
        public IReadOnlyList<ConstructionProposal> Generate(BackendContext context,UserSettings settings,CancellationToken cancellation)
        {
            var choices=new List<List<ConstructionOperation>>();
            for(int i=0;i<context.Source.Sentences.Count;i++)
            {
                cancellation.ThrowIfCancellationRequested();var sentence=context.Source.Sentences[i];var root=sentence.Root;
                var available=new List<ConstructionOperation>();
                if(root!=null)
                {
                    foreach(var argument in sentence.Words.Where(w=>w.Head==root.Id))
                    {
                        string? kind=null;
                        if(context.Source.Language=="tr"&&new[]{"obj","ccomp","obl"}.Contains(argument.Relation.Split(':')[0]))kind="tr-front-constituent";
                        if(context.Source.Language=="en"&&argument.Relation=="obj")kind="en-active-passive";
                        if(context.Source.Language=="en"&&argument.Relation=="advcl")kind="en-postpose-cause";
                        if(kind==null)continue;
                        var op=new ConstructionOperation{SentenceIndex=i,ArgumentId=argument.Id,Kind=kind};
                        var target=ConstructionRenderer.RenderSentence(context.Source,op);
                        if(target!=null&&target!=context.Source.Text.Substring(sentence.Start,sentence.Length))available.Add(op);
                    }
                }
                choices.Add(available);
            }
            var result=new List<ConstructionProposal>();var seen=new HashSet<string>(StringComparer.Ordinal);
            // Whole-paragraph variants combine independently certified sentence operations.
            // The bound is independent of document length and protects CPU/memory on 8 GB PCs.
            for(int variant=0;variant<6;variant++)
            {
                cancellation.ThrowIfCancellationRequested();var proposal=new ConstructionProposal();
                for(int i=0;i<choices.Count;i++)if(choices[i].Count>0)proposal.Operations.Add(choices[i][variant%choices[i].Count]);
                string? target=ConstructionRenderer.Replay(context.Source,proposal.Operations);
                if(target==null||target==context.Source.Text||!seen.Add(target))continue;
                proposal.Text=target;result.Add(proposal);
            }
            for(int i=0;i<choices.Count&&result.Count<6;i++)
                foreach(var op in choices[i].Take(2))
                {
                    var proposal=new ConstructionProposal{Operations=new List<ConstructionOperation>{op}};
                    var target=ConstructionRenderer.Replay(context.Source,proposal.Operations);
                    if(target!=null&&target!=context.Source.Text&&seen.Add(target)){proposal.Text=target;result.Add(proposal);}
                    if(result.Count>=6)break;
                }
            return result;
        }
    }
    public static class ConstructionRenderer
    {
        private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
        public static string? Replay(LinguisticAnalysis source,IReadOnlyList<ConstructionOperation> operations)
        {
            if(operations.Count==0||operations.Count>source.Sentences.Count||operations.GroupBy(o=>o.SentenceIndex).Any(g=>g.Count()!=1))return null;
            string result=source.Text;
            foreach(var op in operations.OrderByDescending(o=>o.SentenceIndex))
            {
                if(op.SentenceIndex<0||op.SentenceIndex>=source.Sentences.Count)return null;
                var sentence=source.Sentences[op.SentenceIndex];string? rewritten=RenderSentence(source,op);
                if(rewritten==null)return null;
                result=result.Remove(sentence.Start,sentence.Length).Insert(sentence.Start,rewritten);
            }
            return result;
        }
        public static string? RenderSentence(LinguisticAnalysis source,ConstructionOperation operation)
        {
            if(operation.SentenceIndex<0||operation.SentenceIndex>=source.Sentences.Count)return null;
            var s=source.Sentences[operation.SentenceIndex];var root=s.Root;
            // Focus particles and coordinated predicates can change scope under movement.
            // Reject these ambiguous cases instead of relocating a bare word.
            if(s.Words.Any(w=>new[]{"yalnızca","sadece","bile","dahi","only","even"}.Contains(w.Form.ToLowerInvariant()))
                ||s.Words.Any(w=>w.Relation=="conj"&&w.Pos=="VERB")
                ||s.Words.Any(w=>w.Pos=="PUNCT"&&new[]{"\"","“","”","«","»"}.Contains(w.Form)))return null;
            var arg=s.Words.SingleOrDefault(w=>w.Id==operation.ArgumentId);
            if(root==null||arg==null||arg.Head!=root.Id||s.Words.Count>100||s.Words.Any(w=>w.Pos=="SYM"&&w.Form!="°"&&w.Form!="%"))return null;
            var branch=s.Subtree(arg.Id).Where(w=>w.Pos!="PUNCT").ToList();
            if(branch.Count==0)return null;
            int lo=branch.Min(w=>w.Start),hi=branch.Max(w=>w.End);
            // A non-projective or overlapping subtree cannot be cut as one phrase.
            if(s.Words.Any(w=>w.Start>=lo&&w.End<=hi&&w.Pos!="PUNCT"&&!branch.Contains(w)))return null;
            if(source.Text.Substring(lo,hi-lo).Any(char.IsControl))return null;
            switch(operation.Kind)
            {
                case "tr-front-constituent":
                    if(source.Language!="tr"||!new[]{"obj","ccomp","obl"}.Contains(arg.Relation.Split(':')[0])||lo<=s.Start||hi>root.Start)return null;
                    // Discourse connectives stay outside the moved clause, in their original scope.
                    string original=source.Text.Substring(s.Start,s.Length);
                    string leading="";int offset=s.Start;
                    var connector=Regex.Match(original,@"^(Ancak|Fakat|Bununla birlikte)\s+",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
                    if(connector.Success){leading=connector.Value;offset+=connector.Length;}
                    if(lo<offset)return null;
                    string front=source.Text.Substring(lo,hi-lo).Trim();
                    string prefix=source.Text.Substring(offset,lo-offset).TrimEnd(' ',',');
                    string suffix=source.Text.Substring(hi,s.End-hi).TrimStart(' ',',');
                    if(prefix.Length==0)return null;
                    string remaining=prefix+(suffix.Length>0?" "+suffix:"");
                    remaining=FirstCase(remaining,false,s.Words.First(w=>w.Start>=offset).Pos,Turkish);
                    front=FirstCase(front,leading.Length==0,branch[0].Pos,Turkish);
                    return leading+front+" "+remaining;
                case "en-active-passive":
                    if(source.Language!="en"||arg.Relation!="obj"||root.Pos!="VERB"||!root.Features.TryGetValue("Tense",out var tense)||tense!="Past")return null;
                    if(root.Features.TryGetValue("Voice",out var voice)&&voice=="Pass")return null;
                    // Restrict conversion to a simple finite past clause with explicit actor.
                    if(s.Words.Any(w=>w.Head==root.Id&&(w.Relation.StartsWith("aux",StringComparison.Ordinal)||w.Relation=="advcl"||w.Relation=="ccomp"||w.Relation=="xcomp")))return null;
                    var subject=s.Words.Where(w=>w.Head==root.Id&&w.Relation=="nsubj").ToList();if(subject.Count!=1)return null;
                    var actor=s.Subtree(subject[0].Id).Where(w=>w.Pos!="PUNCT").ToList();
                    if(actor.Count==0||actor.Min(w=>w.Start)!=s.Start||actor.Max(w=>w.End)>root.Start||lo<root.End)return null;
                    if(s.Words.Any(w=>w.Pos!="PUNCT"&&!actor.Contains(w)&&!branch.Contains(w)&&w.Id!=root.Id))return null;
                    string? participle=PastParticiple(root);if(participle==null)return null;
                    string patient=FirstCase(source.Text.Substring(lo,hi-lo),true,branch[0].Pos,CultureInfo.InvariantCulture);
                    string agent=FirstCase(source.Text.Substring(actor.Min(w=>w.Start),actor.Max(w=>w.End)-actor.Min(w=>w.Start)),false,actor[0].Pos,CultureInfo.InvariantCulture);
                    string agreement=arg.Features.TryGetValue("Number",out var number)&&number=="Plur"?"were":"was";
                    string punctuation=source.Text.Substring(hi,s.End-hi).Trim();
                    if(!Regex.IsMatch(punctuation,@"^[.!?]?$"))return null;
                    return patient+" "+agreement+" "+participle+" by "+agent+punctuation;
                case "en-postpose-cause":
                    if(source.Language!="en"||arg.Relation!="advcl"||lo!=s.Start||hi>=root.Start||!branch.Any(w=>w.Head==arg.Id&&w.Relation=="mark"&&w.Form.Equals("Because",StringComparison.OrdinalIgnoreCase)))return null;
                    string cause=FirstCase(source.Text.Substring(lo,hi-lo),false,branch[0].Pos,CultureInfo.InvariantCulture);
                    string main=source.Text.Substring(hi,s.End-hi).TrimStart(' ',',');
                    string tail=Regex.Match(main,@"[.!?]+$").Value;
                    if(tail.Length>0)main=main.Substring(0,main.Length-tail.Length);
                    var mainFirst=s.Words.FirstOrDefault(w=>w.Start>=hi&&w.Pos!="PUNCT");if(mainFirst==null)return null;
                    main=FirstCase(main,true,mainFirst.Pos,CultureInfo.InvariantCulture);
                    return main+" because"+cause.Substring("because".Length)+tail;
                default:return null;
            }
        }
        private static string FirstCase(string text,bool upper,string pos,CultureInfo culture)
        {
            if(text.Length==0||pos=="PROPN"||!char.IsLetter(text[0]))return text;
            return (upper?char.ToUpper(text[0],culture):char.ToLower(text[0],culture))+text.Substring(1);
        }
        private static string? PastParticiple(DependencyWord verb)
        {
            if(verb.Form.EndsWith("ed",StringComparison.OrdinalIgnoreCase))return verb.Form.ToLowerInvariant();
            var irregular=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){["find"]="found",["show"]="shown",["write"]="written",["take"]="taken",["make"]="made",["give"]="given",["build"]="built",["see"]="seen",["know"]="known",["read"]="read",["send"]="sent",["hold"]="held",["choose"]="chosen"};
            return irregular.TryGetValue(verb.Lemma,out var value)?value:null;
        }
    }
}

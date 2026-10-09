using System;
using System.Collections.Generic;
using System.Linq;
using AcademicParaphraser.Core.Rewriting;
namespace AcademicParaphraser.Core.Backends
{
    public static class WordProposalCompiler
    {
        public static Candidate Compile(BackendContext context,ConstructionProposal proposal)
        {
            var edits=new List<TextEdit>();var source=context.Source;
            if(ConstructionRenderer.Replay(source,proposal.Operations)!=proposal.Text)throw new ArgumentException("Kuruluş kaydı Word önizlemesiyle eşleşmiyor.");
            foreach(var operation in proposal.Operations)
            {
                var sentence=source.Sentences[operation.SentenceIndex];string original=source.Text.Substring(sentence.Start,sentence.Length);
                var writable=context.Plan.Blocks.Where(b=>b.Editable&&b.Start<sentence.End&&b.Start+b.Text.Length>sentence.Start)
                    .Select(b=>new TextSpan{Start=Math.Max(b.Start,sentence.Start)-sentence.Start,Length=Math.Min(b.Start+b.Text.Length,sentence.End)-Math.Max(b.Start,sentence.Start)}).ToArray();
                var fixedSpans=context.Plan.Blocks.Where(b=>!b.Editable&&b.Text.Length>0&&b.Start<sentence.End&&b.Start+b.Text.Length>sentence.Start)
                    .Select(b=>new TextSpan{Start=Math.Max(b.Start,sentence.Start)-sentence.Start,Length=Math.Min(b.Start+b.Text.Length,sentence.End)-Math.Max(b.Start,sentence.Start)}).ToArray();
                var sentencePlan=RewritePlan.Create(original,writable,fixedSpans);
                string target=ConstructionRenderer.RenderSentence(source,operation)??throw new ArgumentException("Cümle kuruluşu yeniden üretilemedi.");
                var compiled=sentencePlan.BuildCandidate(sentencePlan.CompileNaturalText(target),"multi-stage:dependency-construction");
                foreach(var edit in compiled.Edits){edit.Start+=sentence.Start;edits.Add(edit);}
            }
            var result=new Candidate{Text=EditApplication.Apply(source.Text,edits),Edits=edits};
            if(result.Text!=proposal.Text||!CandidateIntegrity.IsApplicable(source.Text,result,e=>context.Plan.Blocks.Any(b=>b.Editable&&e.Start>=b.Start&&e.Start+e.Length<=b.Start+b.Text.Length)))
                throw new ArgumentException("Öneri korunan Word alanlarına kesin eşlenemedi.");
            return result;
        }
    }
}

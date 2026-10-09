using System;
using System.Collections.Generic;
using System.Linq;
namespace AcademicParaphraser.Core.Backends
{
    public static class EnglishNominalConstituents
    {
        public static IReadOnlyList<DependencyWord> Patient(DependencySentence sentence,DependencyWord head)
        {
            var core=sentence.Subtree(head.Id).Where(w=>w.Pos!="PUNCT").ToList();
            if(sentence.Root==null||head.Pos!="NOUN"||head.Head!=sentence.Root.Id)return core;
            // A parser sometimes attaches Y to the predicate in "between X and Y".
            // Only complete this explicitly paired nominal construction: one unmatched
            // between, no already-complete coordination, and one nominal Y with "and".
            var between=core.Where(w=>w.Relation=="case"&&w.Form.Equals("between",StringComparison.OrdinalIgnoreCase)).ToList();
            if(between.Count!=1||core.Any(w=>w.Relation=="cc"&&w.Form.Equals("and",StringComparison.OrdinalIgnoreCase)))return core;
            var extraHeads=sentence.Words.Where(w=>w.Head==sentence.Root.Id&&w.Relation=="conj"&&new[]{"NOUN","PROPN"}.Contains(w.Pos)&&w.Start>between[0].End).ToList();
            if(extraHeads.Count!=1)return core;
            var extra=sentence.Subtree(extraHeads[0].Id).Where(w=>w.Pos!="PUNCT").ToList();
            if(extra.Count(w=>w.Relation=="cc"&&w.Form.Equals("and",StringComparison.OrdinalIgnoreCase))!=1
                ||extra.Any(w=>w.Pos=="VERB"&&!w.Features.TryGetValue("VerbForm",out var form))
                ||extra.Any(w=>w.Pos=="VERB"&&w.Features.TryGetValue("VerbForm",out var form)&&form!="Part"))return core;
            var combined=core.Concat(extra).OrderBy(w=>w.Start).ToList();
            int lo=combined[0].Start,hi=combined.Last().End;
            if(extra.Min(w=>w.Start)<core.Max(w=>w.End)
                ||sentence.Words.Any(w=>w.Start>=lo&&w.End<=hi&&(w.Form==","||w.Pos!="PUNCT"&&!combined.Contains(w))))return core;
            return combined;
        }
    }
}

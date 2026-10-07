using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AcademicParaphraser.Core.Rewriting
{
    public static class LanguageProfile
    {
        public static bool IsEnglish(string text,MetinDili setting)
        {
            if(setting==MetinDili.İngilizce)return true;
            if(setting==MetinDili.Türkçe)return false;
            int en=Regex.Matches(text,@"\b(?:the|a|an|and|of|were|was|that|because|with|between|did|not|have|has|this|they|their|is|are|to|for)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant).Count;
            int tr=Regex.Matches(text,@"\b(?:ve|ile|bir|bu|olarak|için|ise|ancak|hakkında|göre|değil|olan|olduğu)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant).Count;
            return en>=2&&en>tr;
        }
        // Conservative literal names, supplemented by common English sentence openers.
        // This is not NER; unknown common capitalized words can still be overprotected.
        public static bool HasClearLanguageChange(string target,bool englishSource)
        {
            int en=Regex.Matches(target,@"\b(?:the|a|an|and|of|were|was|that|because|with|between|did|not|have|has|this|they|their|is|are|to|for)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant).Count;
            int tr=Regex.Matches(target,@"\b(?:ve|ile|bir|bu|olarak|için|ise|ancak|hakkında|göre|değil|olan|olduğu)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant).Count;
            return englishSource ? tr>=2&&tr>en : en>=2&&en>tr;
        }
        private static readonly HashSet<string> Openers=new HashSet<string>(new[]{"The","A","An","This","That","These","Those","It","We","They","Our","My","I","Your","His","Her","Because","However","Although","If","Unless","When","While","During","After","Before","In","On","At","From","As","To","For","By","Both","Only","Despite","Research","Researchers","Study","Studies","Survey","Results","Data","Evidence","Students","Teachers","Authors","Committee","Report","Reports","Participants","Measurements","Temperature","First","Second","Finally","Therefore"},StringComparer.OrdinalIgnoreCase);
        public static IEnumerable<TextSpan> EnglishNames(string text)=>Regex.Matches(text,@"\b[A-Z][a-z]+(?:['’]s)?\b",RegexOptions.CultureInvariant).Cast<Match>()
            .Where(m=>!Openers.Contains(m.Value)).Select(m=>new TextSpan{Start=m.Index,Length=m.Length,Reason="İngilizce özel isim adayı"});
    }
}

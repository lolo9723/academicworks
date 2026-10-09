using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AcademicParaphraser.Core.Backends;
namespace AcademicParaphraser.Infrastructure.Backends
{
    public static class ConlluReader
    {
        public static LinguisticAnalysis Parse(string source,string conllu,string language)
        {
            if(source.Length>80000||conllu.Length>3000000)throw new ArgumentException("Çözümleme yanıtı çok uzun.");
            var sentences=new List<DependencySentence>();var words=new List<DependencyWord>();int cursor=0;
            foreach(string line in conllu.Replace("\r\n","\n").Split('\n'))
            {
                if(line.Length==0){Finish(words,sentences);words=new List<DependencyWord>();continue;}
                if(line[0]=='#')continue;
                string[] fields=line.Split('\t');if(fields.Length!=10)throw new ArgumentException("CoNLL-U sütunları geçersiz.");
                if(fields[0].Contains("-")||fields[0].Contains("."))continue;
                if(!int.TryParse(fields[0],NumberStyles.None,CultureInfo.InvariantCulture,out int id)||!int.TryParse(fields[6],NumberStyles.None,CultureInfo.InvariantCulture,out int head))throw new ArgumentException("CoNLL-U kimliği geçersiz.");
                string form=fields[1];int start=source.IndexOf(form,cursor,StringComparison.Ordinal);
                // Never guess a Word position after tokenizer normalization or a contraction
                // split whose surface cannot be aligned exactly to the user's UTF-16 text.
                if(form.Length==0||start<0||source.Substring(cursor,start-cursor).Any(c=>!char.IsWhiteSpace(c)))throw new ArgumentException("Çözümleme tokenleri Word metnine kesin olarak eşlenemedi.");
                var word=new DependencyWord{Id=id,Head=head,Start=start,Length=form.Length,Form=form,Lemma=fields[2]=="_"?form:fields[2],Pos=fields[3],Relation=fields[7]};
                if(fields[5]!="_")foreach(string part in fields[5].Split('|')){var pair=part.Split('=');if(pair.Length!=2||word.Features.ContainsKey(pair[0]))throw new ArgumentException("Morfoloji öznitelikleri geçersiz.");word.Features[pair[0]]=pair[1];}
                words.Add(word);cursor=start+form.Length;
            }
            Finish(words,sentences);
            if(sentences.Count==0||source.Substring(cursor).Any(c=>!char.IsWhiteSpace(c)))throw new ArgumentException("Çözümlemede kaynak metnin bir bölümü kayboldu.");
            return new LinguisticAnalysis{Text=source,Language=language,Backend="UDPipe 1.3.0 + own UD 2.17 training",Sentences=sentences};
        }
        private static void Finish(List<DependencyWord> words,List<DependencySentence> result)
        {
            if(words.Count==0)return;
            if(words.Select(w=>w.Id).Distinct().Count()!=words.Count||words.Count(w=>w.Head==0)!=1)throw new ArgumentException("Çözümleme ağacının kökü/kimlikleri geçersiz.");
            var index=words.ToDictionary(w=>w.Id);
            foreach(var word in words)
            {
                var seen=new HashSet<int>();int id=word.Id;
                while(id!=0){if(!seen.Add(id)||!index.TryGetValue(id,out var node))throw new ArgumentException("Çözümleme ağacı döngülü veya kopuk.");id=node.Head;}
            }
            result.Add(new DependencySentence{Start=words[0].Start,Length=words.Last().End-words[0].Start,Words=words});
        }
    }
}

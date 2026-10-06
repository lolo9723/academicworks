using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace AcademicParaphraser.Infrastructure.InternetDictionaryProviders
{
    public static class WikiLexicalParser
    {
        public static DictionaryResult Parse(HtmlDocument document)
        {
            var result = new DictionaryResult();
            bool turkish = false, synonyms = false;
            int meanings = 0;
            foreach (var node in document.DocumentNode.Descendants())
            {
                if (node.Name == "h2") { turkish = node.InnerText.Contains("Türkçe") || node.InnerText.Contains("Turkish"); synonyms = false; }
                else if ((node.Name == "h3" || node.Name == "h4" || node.Name == "h5") && turkish)
                {
                    synonyms = node.InnerText.Contains("Eş anlamlı") || node.InnerText.Contains("Eşanlamlı") || node.InnerText.Contains("Synonyms");
                    string heading = HtmlEntity.DeEntitize(node.InnerText).Replace("[edit]", "").Replace("[değiştir]", "").Trim();
                    string pos = heading == "Noun" || heading == "Ad" || heading == "İsim" ? "Noun" : heading == "Verb" || heading == "Fiil" || heading == "Eylem" ? "Verb" : heading == "Adjective" || heading == "Sıfat" ? "Adjective" : heading == "Adverb" || heading == "Zarf" ? "Adverb" : "";
                    if (pos.Length > 0) result.PosTags.Add(pos);
                }
                else if (turkish && node.Name == "ol" && !synonyms)
                {
                    meanings += node.ChildNodes.Count(n => n.Name == "li");
                    foreach (var meaning in node.ChildNodes.Where(n => n.Name == "li"))
                    {
                        string definition = HtmlEntity.DeEntitize(meaning.InnerText).Trim();
                        result.Definitions.Add(definition.Substring(0, Math.Min(400, definition.Length)));
                    }
                }
                else if (turkish && node.Name == "span" && node.GetAttributeValue("class", "").Split(' ').Contains("synonym"))
                {
                    // Wiktionary also places explicit synonyms inside a definition's nyms span.
                    // Reading only that labelled span excludes gloss translations and antonyms.
                    foreach (var link in node.Descendants("a")) Add(result, link);
                }
                else if (turkish && synonyms && node.Name == "a")
                    Add(result, node);
            }
            result.Synonyms = result.Synonyms.Distinct(StringComparer.Ordinal).ToList();
            result.SingleSense = meanings == 1;
            result.PosTags = result.PosTags.Distinct(StringComparer.Ordinal).ToList();
            return result;
        }
        private static void Add(DictionaryResult result, HtmlNode link)
        {
            string text = HtmlEntity.DeEntitize(link.InnerText).Trim();
            string href = link.GetAttributeValue("href", "");
            if (href.StartsWith("/wiki/", StringComparison.Ordinal) && !href.Contains(":") && text.Length >= 3 && text.All(char.IsLetter)) result.Synonyms.Add(text);
        }
    }
}

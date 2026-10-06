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
                    synonyms = node.InnerText.Contains("Eş anlamlı") || node.InnerText.Contains("Eşanlamlı") || node.InnerText.Contains("Synonyms");
                else if (turkish && node.Name == "ol" && !synonyms)
                    meanings += node.ChildNodes.Count(n => n.Name == "li");
                else if (turkish && synonyms && node.Name == "a")
                {
                    string text = HtmlEntity.DeEntitize(node.InnerText).Trim();
                    string href = node.GetAttributeValue("href", "");
                    if (href.StartsWith("/wiki/", StringComparison.Ordinal) && !href.Contains(":") && text.Length >= 3 && text.All(char.IsLetter)) result.Synonyms.Add(text);
                }
            }
            result.Synonyms = result.Synonyms.Distinct(StringComparer.Ordinal).ToList();
            result.SingleSense = meanings == 1;
            return result;
        }
    }
}

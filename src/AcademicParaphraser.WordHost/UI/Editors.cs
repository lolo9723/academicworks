using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using AcademicParaphraser.Core;
namespace AcademicParaphraser.WordHost.UI
{
    public sealed class LexiconView
    {
        [Browsable(false)]
        public LexiconEntry Entry
        {
            get;
        }
        public LexiconView(LexiconEntry entry)
        {
            Entry = entry;
        }
        public string Kök
        {
            get => Entry.Lemma; set => Entry.Lemma = value;
        }
        public string SözcükTürü
        {
            get => Entry.Pos; set => Entry.Pos = value;
        }
        public string EşAnlamlılar
        {
            get => string.Join("; ", Entry.Synonyms); set => Entry.Synonyms = Split(value);
        }
        public string YakınAnlamlılar
        {
            get => string.Join("; ", Entry.NearSynonyms); set => Entry.NearSynonyms = Split(value);
        }
        public string YasakKarşılıklar
        {
            get => string.Join("; ", Entry.ForbiddenReplacements); set => Entry.ForbiddenReplacements = Split(value);
        }
        public string AkademikKarşılıklar
        {
            get => string.Join("; ", Entry.AcademicAlternatives); set => Entry.AcademicAlternatives = Split(value);
        }
        public string Alan
        {
            get => Entry.Domain; set => Entry.Domain = value;
        }
        public double Güven
        {
            get => Entry.Confidence; set => Entry.Confidence = value;
        }
        public bool TeknikTerim
        {
            get => Entry.Technical; set => Entry.Technical = value;
        }
        public string Örnekler
        {
            get => string.Join("; ", Entry.Examples); set => Entry.Examples = Split(value);
        }
        private static System.Collections.Generic.List<string> Split(string s) => s.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();
    }
    public sealed class RuleView
    {
        [Browsable(false)]
        public RuleDefinition Rule
        {
            get;
        }
        public RuleView(RuleDefinition rule)
        {
            Rule = rule;
        }
        public string Kimlik
        {
            get => Rule.Id; set => Rule.Id = value;
        }
        public string KaynakRegex
        {
            get => Rule.Pattern; set => Rule.Pattern = value;
        }
        public string HedefŞablon
        {
            get => Rule.Target; set => Rule.Target = value;
        }
        public string DönüşümAilesi
        {
            get => Rule.Family; set => Rule.Family = value;
        }
        public Strength Düzey
        {
            get => Rule.Strength; set => Rule.Strength = value;
        }
        public double Güven
        {
            get => Rule.Confidence; set => Rule.Confidence = value;
        }
        public string SözcükTürü
        {
            get => Rule.PosConstraint; set => Rule.PosConstraint = value;
        }
        public string BağlamRegex
        {
            get => Rule.ContextPattern; set => Rule.ContextPattern = value;
        }
        public string GerekliEkler
        {
            get => string.Join(";", Rule.RequiredMorphemes); set => Rule.RequiredMorphemes = value.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        }
        public string YasakEkler
        {
            get => string.Join(";", Rule.ForbiddenMorphemes); set => Rule.ForbiddenMorphemes = value.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        }
        public string Alan
        {
            get => Rule.Domain; set => Rule.Domain = value;
        }
        public long KullanımSayısı => Rule.UsageCount;
    }
    public sealed class DomainConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new StandardValuesCollection(new[] { "genel", "turizm", "rekreasyon", "sosyal bilimler", "eğitim", "psikoloji", "işletme", "yönetim" });
    }
}

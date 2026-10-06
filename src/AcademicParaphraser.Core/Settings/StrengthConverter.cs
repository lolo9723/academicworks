using System;
using System.ComponentModel;
using System.Globalization;
namespace AcademicParaphraser.Core
{
    public sealed class StrengthConverter : EnumConverter
    {
        public StrengthConverter() : base(typeof(Strength)) { }
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Strength strength)
                return strength == Strength.Light ? "Hafif" : strength == Strength.Moderate ? "Orta" : "Güçlü";
            return base.ConvertTo(context, culture, value, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string name)
            {
                if (name == "Hafif")
                    return Strength.Light;
                if (name == "Orta")
                    return Strength.Moderate;
                if (name == "Güçlü")
                    return Strength.Strong;
            }
            return base.ConvertFrom(context, culture, value)!;
        }
    }
}

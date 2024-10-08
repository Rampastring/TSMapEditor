using Rampastring.Tools;
using Rampastring.XNAUI;
using System.Globalization;

namespace TSMapEditor.UI.Controls
{
    public class EditorNumberTextBox : EditorTextBox
    {
        public EditorNumberTextBox(WindowManager windowManager) : base(windowManager)
        {
        }

        public int DefaultValue { get; set; } = 0;
        public double DoubleDefaultValue { get; set; } = 0.0;

        public bool AllowDecimals { get; set; }

        protected override bool AllowCharacterInput(char character)
        {
            return (character >= '0' && character <= '9') || (character == '-' && Text.Length == 0) || (AllowDecimals && character == '.');
        }

        public int Value
        {
            get
            {
                int firstNonDigitIndex = -1;
                int i = 0;
                while (i < Text.Length)
                {
                    if (!char.IsDigit(Text[i]) && (i > 0 || Text[i] != '-'))
                    {
                        firstNonDigitIndex = i;
                        break;
                    }

                    i++;
                }

                if (firstNonDigitIndex == -1)
                    return Conversions.IntFromString(Text, DefaultValue);

                return Conversions.IntFromString(Text.Substring(0, firstNonDigitIndex), DefaultValue);
            }
            set => Text = value.ToString(CultureInfo.InvariantCulture);
        }

        public double DoubleValue
        {
            get
            {
                int firstNonDigitIndex = -1;
                int i = 0;
                while (i < Text.Length)
                {
                    if (!char.IsDigit(Text[i]) && (i > 0 || Text[i] != '-') && Text[i] != '.')
                    {
                        firstNonDigitIndex = i;
                        break;
                    }

                    i++;
                }

                if (firstNonDigitIndex == -1)
                    return Conversions.DoubleFromString(Text, DoubleDefaultValue);

                return Conversions.DoubleFromString(Text.Substring(0, firstNonDigitIndex), DefaultValue);
            }
            set => Text = value.ToString("0.#######################", CultureInfo.InvariantCulture); // prevent scientific notation
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == nameof(AllowDecimals))
            {
                AllowDecimals = Conversions.BooleanFromString(value, AllowDecimals);
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }
    }
}

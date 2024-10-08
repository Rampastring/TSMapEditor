using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Controls
{
    public class EditorTextBox : XNATextBox
    {
        public EditorTextBox(WindowManager windowManager) : base(windowManager)
        {
            Height = Constants.UITextBoxHeight;
        }

        public bool AllowComma { get; set; } = true;
        public bool AllowSemicolon { get; set; } = false;

        protected override bool AllowCharacterInput(char character)
        {
            if (character == ',')
                return AllowComma;

            if (character == ';')
                return AllowSemicolon;

            return base.AllowCharacterInput(character);
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == nameof(AllowComma))
            {
                AllowComma = Conversions.BooleanFromString(value, AllowComma);
            }
            else if (key == nameof(AllowSemicolon))
            {
                AllowSemicolon = Conversions.BooleanFromString(value, AllowSemicolon);
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }
    }
}

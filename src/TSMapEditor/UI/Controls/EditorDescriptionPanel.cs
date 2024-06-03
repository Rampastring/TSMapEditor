using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace TSMapEditor.UI.Controls
{
    public class EditorDescriptionPanel : EditorPanel
    {
        public EditorDescriptionPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        private XNALabel lblDescription;

        public override string Text
        {
            get => lblDescription.Text;
            set => lblDescription.Text = string.Join(Environment.NewLine, Renderer.GetFixedTextLines(value, lblDescription.FontIndex, Width - (Constants.UIEmptySideSpace * 2), true, true));
        }

        public override void Initialize()
        {
            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = Name + "." + nameof(lblDescription);
            lblDescription.X = Constants.UIEmptySideSpace;
            lblDescription.Y = Constants.UIEmptyTopSpace;
            lblDescription.Text = string.Empty;
            AddChild(lblDescription);

            base.Initialize();
        }
    }
}

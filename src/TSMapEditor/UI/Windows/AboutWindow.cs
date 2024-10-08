using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Reflection;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class AboutWindow : INItializableWindow
    {
        public AboutWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            Name = nameof(AboutWindow);
            base.Initialize();

            var lblVersion = FindChild<XNALabel>("lblVersion");
            lblVersion.Text = "Version " + Constants.ReleaseVersion + ", Build " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public void Open() => Show();
    }
}

using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectParticleSystemTypeWindow : SelectObjectWindow<ParticleSystemType>
    {
        public SelectParticleSystemTypeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectParticleSystemTypeWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (ParticleSystemType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (ParticleSystemType particleSystemType in map.Rules.ParticleSystemTypes)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{particleSystemType.Index} {particleSystemType.ININame}", Tag = particleSystemType });
                if (particleSystemType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}

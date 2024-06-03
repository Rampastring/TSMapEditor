using Rampastring.XNAUI;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class GenerateStandardHousesWindow : INItializableWindow
    {
        public GenerateStandardHousesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(GenerateStandardHousesWindow);
            base.Initialize();

            FindChild<EditorButton>("btnSingleplayer").LeftClick += BtnSingleplayer_LeftClick;
            FindChild<EditorButton>("btnMultiplayer").LeftClick += BtnMultiplayer_LeftClick;
            FindChild<EditorButton>("btnCoOp").LeftClick += BtnCoOp_LeftClick;
            FindChild<EditorButton>("btnCancel").LeftClick += (s, e) => Hide();
        }

        private void BtnSingleplayer_LeftClick(object sender, System.EventArgs e)
        {
            AddHousesFromEditorRulesSectionAndHide("SPHouses");
        }

        private void BtnMultiplayer_LeftClick(object sender, EventArgs e)
        {
            AddHousesFromEditorRulesSectionAndHide("Houses");
        }

        private void BtnCoOp_LeftClick(object sender, EventArgs e)
        {
            AddHousesFromEditorRulesSectionAndHide("CoopHouses");
        }

        private void AddHousesFromEditorRulesSectionAndHide(string sectionName)
        {
            var houses = map.Rules.GetHousesFrom(map.EditorConfig.EditorRulesIni, sectionName);

            // Set some meaningful default values
            for (int i = 0; i < houses.Count; i++)
            {
                var house = houses[i];
                house.ID = i;
                house.Edge = "North";
                house.Allies = house.ININame;
                house.TechLevel = 7;

                if (!Constants.IsRA2YR)
                {
                    var houseType = new HouseType(house.ININame);
                    houseType.Index = i;
                    houseType.Color = house.Color;

                    // Find reasonable default for Side and ActsLike
                    Helpers.FindDefaultSideForNewHouseType(houseType, map.Rules);
                    house.ActsLike = houses.FindIndex(h => house.ININame.StartsWith(h.ININame));
                    if (house.ActsLike < 0)
                        house.ActsLike = 0;

                    map.AddHouseType(houseType);
                    house.HouseType = houseType;
                }
            }

            map.AddHouses(houses);

            ReassignObjectHouses();
            Hide();
        }

        private void ReassignObjectHouses()
        {
            map.DoForAllTechnos(t =>
            {
                string ownerName = t.Owner.ININame;
                var house = map.Houses.Find(h => h.ININame == ownerName);
                if (house != null)
                    t.Owner = house;
            });
        }

        public void Open()
        {
            Show();
        }
    }
}

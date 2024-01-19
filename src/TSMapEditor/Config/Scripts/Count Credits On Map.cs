// Script for counting value of all present Tiberium/ore overlay.

// Using clauses.
// Unless you know what's in the WAE code-base, you want to always include
// these "standard usings".
using System;
using TSMapEditor;
using TSMapEditor.Models;
using TSMapEditor.CCEngine;
using TSMapEditor.Rendering;
using TSMapEditor.GameMath;

namespace WAEScript
{
    public class CountCreditsOnMapScript
    {
        /// <summary>
        /// Returns the description of this script.
        /// All scripts must contain this function.
        /// </summary>
        public string GetDescription() => "This script will count credit value of all Tiberium and ore overlays. Continue?";

        /// <summary>
        /// Returns the message that is presented to the user if running this script succeeded.
        /// All scripts must contain this function.
        /// </summary>
        public string GetSuccessMessage()
        {
            return $"There is {count} credits worth of resources present.";
        }

        int count = 0;

        /// <summary>
        /// The function that actually does the magic.
        /// </summary>
        /// <param name="map">Map argument that allows us to access map data.</param>
        public void Perform(Map map)
        {
            count = 0;

            map.DoForAllValidTiles(cell =>
            {
                if (cell.Overlay == null || cell.Overlay.OverlayType == null || !cell.Overlay.OverlayType.Tiberium)
                    return;

                int tiberiumIndex = cell.Overlay.OverlayType.GetTiberiumIndex(Constants.UseCountries);
                if (tiberiumIndex > -1)
                {
                    int tiberiumTypeValue = map.Rules.TiberiumTypes[tiberiumIndex].Value;
                    count += tiberiumTypeValue * (cell.Overlay.FrameIndex + 1);
                }
            });
        }
    }
}

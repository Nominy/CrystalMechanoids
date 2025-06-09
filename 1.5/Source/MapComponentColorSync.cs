using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrystalMechanoids
{
    /// <summary>
    /// Map component to handle color sync initialization and updates
    /// </summary>
    public class MapComponent_ColorSync : MapComponent
    {
        private int nextUpdateTick = 0;
        private Color lastKnownColor = Color.cyan;
        
        public MapComponent_ColorSync(Map map) : base(map)
        {

        }
        
        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame >= nextUpdateTick)
            {
                CheckForColorChanges();
                nextUpdateTick = Find.TickManager.TicksGame + 250; // Check every ~4 seconds
            }
        }
        
        private void CheckForColorChanges()
        {
            if (!ColorSyncSystem.IsColorSyncEnabled()) return;
            
            var currentColor = ColorSyncSystem.GetMechanoidColor();
            
            // Check if color has changed
            if (Vector4.Distance(lastKnownColor, currentColor) > 0.01f)
            {
                lastKnownColor = currentColor;
                UpdateColorSyncBuildings(currentColor);
            }
        }
        
        private void UpdateColorSyncBuildings(Color newColor)
        {
            var buildings = map.listerBuildings.allBuildingsColonist
                .Where(b => b is IColorSyncable)
                .Cast<IColorSyncable>();
                
            foreach (var building in buildings)
            {
                building.UpdateSyncedColor(newColor);
            }
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastKnownColor, "lastKnownColor", Color.cyan);
        }
    }
} 
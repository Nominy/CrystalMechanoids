using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrystalMechanoids
{
    /// <summary>
    /// Simple crystal wall that syncs color with mechanoids
    /// </summary>
    public class Building_CrystalWall : Building, IColorSyncable
    {
        public override Color DrawColor
        {
            get
            {
                return ColorSyncSystem.GetBuildingColor();
            }
        }
        
        // IColorSyncable implementation
        public void UpdateSyncedColor(Color newColor)
        {
            // This is called by the system when colors change
            // The DrawColor property will automatically return the right color
        }
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            
            // Add debug toggle for color sync
            if (Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "Color Sync",
                    defaultDesc = "Toggle color synchronization with mechanoids (debug)",
                    icon = TexCommand.ToggleVent,
                    isActive = () => ColorSyncSystem.IsColorSyncEnabled(),
                    toggleAction = () => 
                    {
                        ColorSyncSystem.ToggleColorSync();
                    }
                };
            }
        }
        
        public override string GetInspectString()
        {
            var text = base.GetInspectString();
            
            if (ColorSyncSystem.IsColorSyncEnabled())
            {
                if (!text.NullOrEmpty())
                    text += "\n";
                text += "CM_ColorSyncEnabled".Translate(ColorUtility.ToHtmlStringRGB(DrawColor));
            }
            else
            {
                if (!text.NullOrEmpty())
                    text += "\n";
                text += "CM_ColorSyncReady".Translate();
            }
            
            return text;
        }
    }
} 
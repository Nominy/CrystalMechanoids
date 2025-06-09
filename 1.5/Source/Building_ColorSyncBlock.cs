using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrystalMechanoids
{
    /// <summary>
    /// Example building that demonstrates color syncing using simple DrawColor override
    /// </summary>
    public class Building_ColorSyncBlock : Building, IColorSyncable
    {
        // Simple override of DrawColor - this is the most reliable way to change building colors
        public override Color DrawColor
        {
            get
            {
                if (ColorSyncSystem.IsColorSyncEnabled())
                {
                    Color syncColor = ColorSyncSystem.GetBuildingColor();
                    Log.Message($"[CrystalMechanoids] ColorSyncBlock DrawColor returning: {syncColor} (R:{syncColor.r:F2} G:{syncColor.g:F2} B:{syncColor.b:F2})");
                    return syncColor;
                }
                else
                {
                    // Default light blue color when not syncing
                    Color defaultColor = new Color(0.7f, 0.9f, 1f, 1f);
                    return defaultColor;
                }
            }
        }
        
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            
            // Force graphic refresh on spawn
            if (Spawned)
            {
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Buildings);
            }
        }
        
        // IColorSyncable implementation
        public void UpdateSyncedColor(Color newColor)
        {
            // Mark the map mesh dirty so the building re-renders with new color
            if (Spawned)
            {
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Buildings);
                Log.Message($"[CrystalMechanoids] Updated ColorSyncBlock color to: {newColor}");
            }
        }
        
        public override void Notify_ColorChanged()
        {
            base.Notify_ColorChanged();
            
            // Force mesh refresh
            if (Spawned)
            {
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Buildings);
                Log.Message("[CrystalMechanoids] ColorSyncBlock Notify_ColorChanged called");
            }
        }
        
        // Add some gizmos for debugging
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            
            // Add a toggle for color sync (for testing)
            if (Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "Color Sync",
                    defaultDesc = "Toggle mechanoid color synchronization",
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
                Color syncColor = ColorSyncSystem.GetBuildingColor();
                text += "CM_ColorSyncEnabled".Translate(ColorUtility.ToHtmlStringRGB(syncColor));
                text += $" (Current: R:{DrawColor.r:F2} G:{DrawColor.g:F2} B:{DrawColor.b:F2})"; // Debug info
            }
            else
            {
                if (!text.NullOrEmpty())
                    text += "\n";
                text += "CM_ColorSyncReady".Translate();
                text += $" (Current: R:{DrawColor.r:F2} G:{DrawColor.g:F2} B:{DrawColor.b:F2})"; // Debug info
            }
            
            return text;
        }
    }
} 
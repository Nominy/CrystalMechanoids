using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrystalMechanoids
{
    /// <summary>
    /// Example building that demonstrates color syncing with partial color application
    /// Uses a ThingComp to handle overlay rendering - following RimWorld's modular design patterns
    /// </summary>
    public class Building_ColorSyncBlock : Building, IColorSyncable
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            
            // Let the comp handle graphics initialization
            GetComp<CompColorSyncOverlay>()?.Initialize();
            
            // Force graphic refresh on spawn
            if (Spawned)
            {
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Buildings);
            }
        }
        
        public override void Print(SectionLayer layer)
        {
            // First, let the base building print itself
            base.Print(layer);
            
            // Then let the comp print the overlay
            GetComp<CompColorSyncOverlay>()?.PrintOverlay(layer);
        }
        
        // IColorSyncable implementation
        public void UpdateSyncedColor(Color newColor)
        {
            // Let the comp handle the color update
            GetComp<CompColorSyncOverlay>()?.UpdateSyncedColor(newColor);
        }
        
        public override void Notify_ColorChanged()
        {
            base.Notify_ColorChanged();
            
            // Notify the comp about color changes
            GetComp<CompColorSyncOverlay>()?.OnColorChanged();
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
            
            var comp = GetComp<CompColorSyncOverlay>();
            if (comp != null)
            {
                var overlayInfo = comp.GetOverlayInfo();
                if (!text.NullOrEmpty())
                    text += "\n";
                text += overlayInfo;
            }
            
            return text;
        }
    }
    
    /// <summary>
    /// Component properties for the color sync overlay
    /// </summary>
    public class CompProperties_ColorSyncOverlay : CompProperties
    {
        public string overlayTexPath = "Things/Building/Furniture/CM_ColorSyncBlock_Overlay";
        public ShaderTypeDef overlayShader; // Will be resolved in Initialize()
        public float overlayOpacity = 0.8f;
        
        public CompProperties_ColorSyncOverlay()
        {
            compClass = typeof(CompColorSyncOverlay);
        }
    }
    
    /// <summary>
    /// Component that handles the color sync overlay rendering
    /// This follows RimWorld's pattern for additional graphics on buildings
    /// </summary>
    public class CompColorSyncOverlay : ThingComp
    {
        private Graphic overlayGraphic;
        
        public CompProperties_ColorSyncOverlay Props => (CompProperties_ColorSyncOverlay)props;
        
        public void Initialize()
        {
            // Initialize overlay graphic using proper RimWorld patterns
            var graphicData = new GraphicData();
            graphicData.texPath = Props.overlayTexPath;
            graphicData.graphicClass = typeof(Graphic_Single);
            // Use MetaOverlay shader if not specified in props, otherwise use the specified one
            graphicData.shaderType = Props.overlayShader ?? ShaderTypeDefOf.MetaOverlay;
            graphicData.drawSize = parent.def.graphicData.drawSize;
            overlayGraphic = graphicData.Graphic;
        }
        
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Initialize();
        }
        
        public void PrintOverlay(SectionLayer layer)
        {
            // Print our overlay with color
            if (overlayGraphic != null && parent.Spawned)
            {
                var overlayColor = GetOverlayColor();
                
                // Create a colored material
                var baseMat = overlayGraphic.MatAt(parent.Rotation, parent);
                var coloredMat = MaterialPool.MatFrom((Texture2D)baseMat.mainTexture, baseMat.shader, overlayColor);
                
                // Print the overlay on top with the colored material
                Printer_Plane.PrintPlane(
                    layer,
                    parent.DrawPos,
                    parent.def.graphicData.drawSize,
                    coloredMat,
                    flipUv: false,
                    uvs: null,
                    colors: null,
                    topVerticesAltitudeBias: 0.01f,
                    uvzPayload: 0
                );
            }
        }
        
        private Color GetOverlayColor()
        {
            if (ColorSyncSystem.IsColorSyncEnabled())
            {
                Color syncColor = ColorSyncSystem.GetBuildingColor();
                // Make sure we always return a valid color with proper opacity
                if (syncColor.a <= 0) syncColor.a = Props.overlayOpacity;
                Log.Message($"[CrystalMechanoids] ColorSyncBlock overlay color: {syncColor} (R:{syncColor.r:F2} G:{syncColor.g:F2} B:{syncColor.b:F2} A:{syncColor.a:F2})");
                return syncColor;
            }
            else
            {
                // Use a default crystal-like color when not syncing
                return new Color(0.7f, 0.9f, 1f, Props.overlayOpacity); // Light blue with transparency
            }
        }
        
        public void UpdateSyncedColor(Color newColor)
        {
            // Mark the map mesh dirty so the building re-renders with new overlay color
            if (parent.Spawned)
            {
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Buildings);
                Log.Message($"[CrystalMechanoids] Updated ColorSyncBlock overlay color to: {newColor}");
            }
        }
        
        public void OnColorChanged()
        {
            // Force mesh refresh when colors change
            if (parent.Spawned)
            {
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Buildings);
                Log.Message("[CrystalMechanoids] ColorSyncBlock comp Notify_ColorChanged called");
            }
        }
        
        public string GetOverlayInfo()
        {
            if (ColorSyncSystem.IsColorSyncEnabled())
            {
                Color syncColor = ColorSyncSystem.GetBuildingColor();
                return "CM_ColorSyncEnabled".Translate(ColorUtility.ToHtmlStringRGB(syncColor)) +
                       $" (Overlay: R:{GetOverlayColor().r:F2} G:{GetOverlayColor().g:F2} B:{GetOverlayColor().b:F2})";
            }
            else
            {
                return "CM_ColorSyncReady".Translate() + " (Base + colored crystal overlay)";
            }
        }
    }
} 
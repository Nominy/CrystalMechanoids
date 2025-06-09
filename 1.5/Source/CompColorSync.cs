using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrystalMechanoids
{
    /// <summary>
    /// Component that makes any building sync colors with mechanoids
    /// </summary>
    public class CompColorSync : ThingComp
    {
        public CompProperties_ColorSync Props => (CompProperties_ColorSync)props;
        
        public override void PostDraw()
        {
            base.PostDraw();
            
            // If color sync is enabled, overdraw with mech color
            if (ColorSyncSystem.IsColorSyncEnabled())
            {
                Color mechColor = ColorSyncSystem.GetCachedMechanoidColor();
                
                // Get the building's draw position and rotation
                Vector3 drawPos = parent.DrawPos;
                drawPos.y = parent.def.Altitude + 0.01f; // Slightly above the original
                
                // Draw a colored overlay
                Graphics.DrawMesh(
                    MeshPool.plane10,
                    Matrix4x4.TRS(drawPos, parent.Rotation.AsQuat, new Vector3(parent.def.Size.x, 1f, parent.def.Size.z)),
                    SolidColorMaterials.SimpleSolidColorMaterial(mechColor),
                    0
                );
            }
        }
        
        public override string CompInspectStringExtra()
        {
            if (ColorSyncSystem.IsColorSyncEnabled())
            {
                return "CM_ColorSyncEnabled".Translate(ColorUtility.ToHtmlStringRGB(ColorSyncSystem.GetCachedMechanoidColor()));
            }
            else
            {
                return "CM_ColorSyncReady".Translate();
            }
        }
    }
    
    /// <summary>
    /// Properties for the color sync component
    /// </summary>
    public class CompProperties_ColorSync : CompProperties
    {
        public CompProperties_ColorSync()
        {
            compClass = typeof(CompColorSync);
        }
    }
} 
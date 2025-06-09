using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrystalMechanoids
{
    /// <summary>
    /// Manages color synchronization between mechanoids and buildings
    /// </summary>
    public static class ColorSyncSystem
    {
        private static bool isColorSyncEnabled = false;
        private static Color cachedMechanoidColor = new Color(0.3f, 0.7f, 1f, 1f);
        
        /// <summary>
        /// Gets the current mechanoid color for the player faction
        /// </summary>
        public static Color GetCurrentMechanoidColor()
        {
            try
            {
                if (Find.FactionManager?.OfPlayer != null && ModLister.BiotechInstalled)
                {
                    // Use the actual MechColor property
                    return Find.FactionManager.OfPlayer.MechColor;
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[CrystalMechanoids] Error getting mechanoid color: {ex.Message}");
            }
            
            // Fallback to crystal blue
            return new Color(0.3f, 0.7f, 1f, 1f);
        }
        
        /// <summary>
        /// Gets the cached mechanoid color (for components that need it)
        /// </summary>
        public static Color GetCachedMechanoidColor()
        {
            return cachedMechanoidColor;
        }
        
        /// <summary>
        /// Checks if color sync is enabled
        /// </summary>
        public static bool IsColorSyncEnabled()
        {
            return isColorSyncEnabled;
        }
        
        /// <summary>
        /// Toggles color sync on/off
        /// </summary>
        public static void ToggleColorSync()
        {
            isColorSyncEnabled = !isColorSyncEnabled;
            
            if (isColorSyncEnabled)
            {
                // When enabling, cache current mech color
                cachedMechanoidColor = GetCurrentMechanoidColor();
            }
            
            // Update all buildings immediately
            UpdateAllSyncedBuildings();
            
            Log.Message($"[CrystalMechanoids] Color sync {(isColorSyncEnabled ? "enabled" : "disabled")} - using color: {cachedMechanoidColor}");
        }
        
        /// <summary>
        /// Called when mech color changes
        /// </summary>
        public static void OnMechColorChanged(Color newColor)
        {
            if (isColorSyncEnabled)
            {
                cachedMechanoidColor = newColor;
                UpdateAllSyncedBuildings();
                Log.Message($"[CrystalMechanoids] Mech color changed to: {newColor}, updating synced buildings");
            }
        }
        
        /// <summary>
        /// Gets the color that buildings should use
        /// </summary>
        public static Color GetBuildingColor()
        {
            if (isColorSyncEnabled)
            {
                return cachedMechanoidColor;
            }
            return new Color(0.3f, 0.7f, 1f, 1f); // Default crystal blue
        }
        
        /// <summary>
        /// Updates all synced buildings
        /// </summary>
        public static void UpdateAllSyncedBuildings()
        {
            int updatedCount = 0;
            foreach (Map map in Find.Maps)
            {
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building is IColorSyncable)
                    {
                        // Force the building to regenerate its graphics
                        building.Notify_ColorChanged();
                        // Also mark map mesh dirty for good measure
                        map.mapDrawer.MapMeshDirty(building.Position, MapMeshFlagDefOf.Buildings);
                        updatedCount++;
                    }
                }
            }
            Log.Message($"[CrystalMechanoids] Updated {updatedCount} synced buildings");
        }
        
        /// <summary>
        /// Initialize the color sync system
        /// </summary>
        public static void Initialize()
        {
            cachedMechanoidColor = new Color(0.3f, 0.7f, 1f, 1f);
            Log.Message($"[CrystalMechanoids] Color sync system initialized");
        }
    }
    
    /// <summary>
    /// Interface for buildings that can sync their color with mechanoids
    /// </summary>
    public interface IColorSyncable
    {
        void UpdateSyncedColor(Color newColor);
    }
    
    /// <summary>
    /// Mod settings for Crystal Mechanoids
    /// </summary>
    public class CrystalMechanoidSettings : ModSettings
    {
        public static bool enableColorSync = false;
        
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableColorSync, "enableColorSync", false);
            base.ExposeData();
        }
        
        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            
            listingStandard.CheckboxLabeled("CM_EnableColorSync".Translate(), ref enableColorSync, 
                "CM_EnableColorSyncDesc".Translate());
            
            listingStandard.End();
        }
    }
    
    /// <summary>
    /// Mod class for Crystal Mechanoids
    /// </summary>
    public class CrystalMechanoidsMod : Mod
    {
        CrystalMechanoidSettings settings;
        
        public CrystalMechanoidsMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<CrystalMechanoidSettings>();
        }
        
        public override void DoSettingsWindowContents(Rect inRect)
        {
            CrystalMechanoidSettings.DoSettingsWindowContents(inRect);
        }
        
        public override string SettingsCategory()
        {
            return "Crystal Mechanoids";
        }
    }
} 
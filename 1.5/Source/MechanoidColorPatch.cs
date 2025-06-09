using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace CrystalMechanoids
{
    /// <summary>
    /// Patches to integrate with RimWorld's mechanoid color customization system
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Mechs), "DoWindowContents")]
    public static class MechanoidColorPatch
    {
        // Patch the MainTabWindow_Mechs.DoWindowContents method to add our checkbox
        [HarmonyPostfix]
        static void Postfix(MainTabWindow_Mechs __instance, Rect rect)
        {
            if (!ModLister.BiotechInstalled)
            {
                return;
            }

            try
            {
                // Position our checkbox below the "Choose Mech Color" button
                Rect checkboxRect = new Rect(rect.x, rect.y + 40f, 300f, 24f);
                
                bool currentSync = ColorSyncSystem.IsColorSyncEnabled();
                bool newSync = currentSync;
                
                Widgets.CheckboxLabeled(checkboxRect, "CM_SyncBuildingColors".Translate(), ref newSync);
                
                if (newSync != currentSync)
                {
                    ColorSyncSystem.ToggleColorSync();
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[CrystalMechanoids] Error in DoWindowContents patch: {ex}");
            }
        }
    }

    // Patch Faction.MechColor setter to detect when mechanoid color changes
    [HarmonyPatch(typeof(Faction), nameof(Faction.MechColor), MethodType.Setter)]
    public static class FactionMechColorPatch
    {
        [HarmonyPostfix]
        static void Postfix(Faction __instance, Color value)
        {
            // Only care about player faction
            if (__instance == Faction.OfPlayer)
            {
                ColorSyncSystem.OnMechColorChanged(value);
            }
        }
    }

    // Remove the MechColor setter patch since that property doesn't exist
    // We'll use faction color spectrum changes instead
    
    // No need for universal patch - we control our own building types directly!

    // TODO: Re-enable mechanoid inspect patch later
    /*
    // Patch mechanoid inspection tabs to add our color sync checkbox
    [HarmonyPatch]
    public static class MechanoidInspectPatch
    {
        static MethodBase TargetMethod()
        {
            // Try to find any tab that handles mechanoid inspections
            // We'll target the general inspect panel gizmo generation
            return AccessTools.Method(typeof(Thing), nameof(Thing.GetGizmos));
        }

        static void Postfix(Thing __instance, ref IEnumerable<Gizmo> __result)
        {
            // Only apply to mechanoids owned by player
            if (__instance is Pawn mechanoid && 
                mechanoid.RaceProps.IsMechanoid && 
                mechanoid.Faction == Faction.OfPlayer)
            {
                var gizmos = __result.ToList();
                gizmos.Add(new Command_Toggle
                {
                    defaultLabel = "CM_SyncBuildingColors".Translate(),
                    defaultDesc = "CM_SyncBuildingColorsDesc".Translate(),
                    icon = TexCommand.ToggleVent,
                    isActive = () => ColorSyncSystem.IsColorSyncEnabled(),
                    toggleAction = () => {
                        ColorSyncSystem.ToggleColorSync();
                        if (ColorSyncSystem.IsColorSyncEnabled())
                        {
                            // Update all color-synced buildings immediately
                            ColorSyncSystem.UpdateAllSyncedBuildings();
                        }
                    }
                });
                __result = gizmos;
            }
        }
    }
    */

    // TODO: Re-enable faction color patch later
    /*
    // Patch the main mechanoid color source (faction colors for now)
    [HarmonyPatch(typeof(Faction), nameof(Faction.Color), MethodType.Getter)]
    public static class FactionColorPatch
    {
        static void Postfix(Faction __instance, ref Color __result)
        {

            // Store mechanoid faction color for our sync system
            if (__instance?.def?.defName == "Mechanoid")
            {
                ColorSyncSystem.CacheMechanoidColor(__result);
            }
        }
    }
    */
}
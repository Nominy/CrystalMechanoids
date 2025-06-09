using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using Verse.Noise;
using Verse.Grammar;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;

namespace CrystalMechanoids
{
    /// <summary>
    /// Patches to allow mechanoids to work independently without human oversight.
    /// </summary>
    public static class IndependentMechanoidPatches
    {
        private const string SUBCORE_ENCODER_DEF_NAME = "SubcoreEncoder";
        
        /// <summary>
        /// Allows mechanoids to repair other mechanoids by bypassing the ShouldSkip check.
        /// </summary>
        [HarmonyPatch(typeof(WorkGiver_RepairMech), nameof(WorkGiver_RepairMech.ShouldSkip))]
        public static class WorkGiver_RepairMech_ShouldSkip_Patch
        {
            /// <summary>
            /// Postfix to allow mechanoids to perform repair work on other mechs.
            /// </summary>
            /// <param name="__result">The original result from ShouldSkip</param>
            /// <param name="pawn">The pawn attempting to do the work</param>
            /// <param name="forced">Whether the work is forced</param>
            public static void Postfix(ref bool __result, Pawn pawn, bool forced = false)
            {
                if (pawn?.RaceProps?.IsMechanoid == true)
                {
                    __result = false; // Don't skip repair work for mechanoids
                }
            }
        }

        /// <summary>
        /// Allows mechanoids to use subcore encoders by modifying bill restrictions.
        /// </summary>
        [HarmonyPatch(typeof(Bill), nameof(Bill.PawnAllowedToStartAnew))]
        public static class Bill_PawnAllowedToStartAnew_Patch
        {
            /// <summary>
            /// Postfix to allow mechanoids to start work on subcore encoder recipes.
            /// </summary>
            /// <param name="__result">The original result from PawnAllowedToStartAnew</param>
            /// <param name="___recipe">The recipe associated with this bill</param>
            /// <param name="p">The pawn attempting to start the work</param>
            public static void Postfix(ref bool __result, ref RecipeDef ___recipe, Pawn p)
            {
                // Only proceed if the pawn was originally not allowed and is a mechanoid
                if (__result || !IsMechanoidPawn(p))
                    return;

                // Check if this recipe can be used with subcore encoders
                if (CanUseSubcoreEncoder(___recipe))
                {
                    __result = true;
                }
            }

            /// <summary>
            /// Checks if the given pawn is a mechanoid with valid race properties.
            /// </summary>
            private static bool IsMechanoidPawn(Pawn pawn)
            {
                return pawn?.RaceProps?.IsMechanoid == true;
            }

            /// <summary>
            /// Checks if the recipe can be used with subcore encoders.
            /// </summary>
            private static bool CanUseSubcoreEncoder(RecipeDef recipe)
            {
                return recipe?.recipeUsers?.Any(user => 
                    user?.defName == SUBCORE_ENCODER_DEF_NAME) == true;
            }
        }
    }
}

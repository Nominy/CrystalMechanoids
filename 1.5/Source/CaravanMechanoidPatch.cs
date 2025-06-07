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
using System.Reflection.Emit;

namespace CrystalMechanoids
{
    // Helper class with commonly used methods and cached reflection references
    public static class PatchHelpers
    {
        // Cached method references to avoid repeated reflection calls
        private static readonly MethodInfo _getRacePropsMethod = AccessTools.Method(typeof(Pawn), "get_RaceProps");
        private static readonly MethodInfo _getIsMechanoidMethod = AccessTools.Method(typeof(RaceProperties), "get_IsMechanoid");
        private static readonly MethodInfo _getMapMethod = AccessTools.Method(typeof(MapParent), "get_Map");
        private static readonly MethodInfo _getSpawnedColonyMechsMethod = AccessTools.Method(typeof(MapPawns), "get_SpawnedColonyMechs");
        private static readonly MethodInfo _getFreeColonistsMethod = AccessTools.Method(typeof(MapPawns), "get_FreeColonists");
        private static readonly MethodInfo _getCountMethod = AccessTools.Method(typeof(List<Pawn>), "get_Count");
        private static readonly MethodInfo _randomElementMethod = AccessTools.Method(typeof(GenCollection), "RandomElement", generics: new Type[] { typeof(Pawn) });
        private static readonly FieldInfo _mapPawnsField = AccessTools.Field(typeof(Map), "mapPawns");

        /// <summary>
        /// Generates IL instructions to check if a pawn (loaded from specified opcode) is a mechanoid
        /// Returns instructions that leave a boolean on the stack
        /// </summary>
        public static List<CodeInstruction> GenerateIsMechanoidCheck(OpCode loadPawnOpCode)
        {
            return new List<CodeInstruction>
            {
                new CodeInstruction(loadPawnOpCode),
                new CodeInstruction(OpCodes.Callvirt, _getRacePropsMethod),
                new CodeInstruction(OpCodes.Callvirt, _getIsMechanoidMethod)
            };
        }

        /// <summary>
        /// Generates IL instructions to get SpawnedColonyMechs.RandomElement() from a MapParent
        /// Assumes MapParent is loaded with the specified opcode
        /// </summary>
        public static List<CodeInstruction> GenerateGetRandomMech(OpCode loadMapParentOpCode)
        {
            return new List<CodeInstruction>
            {
                new CodeInstruction(loadMapParentOpCode),
                new CodeInstruction(OpCodes.Callvirt, _getMapMethod),
                new CodeInstruction(OpCodes.Ldfld, _mapPawnsField),
                new CodeInstruction(OpCodes.Callvirt, _getSpawnedColonyMechsMethod),
                new CodeInstruction(OpCodes.Call, _randomElementMethod)
            };
        }

        /// <summary>
        /// Safely tries to find insertion point and returns whether it was found
        /// </summary>
        public static bool TryFindInsertionPoint(List<CodeInstruction> codes, Func<CodeInstruction, int, bool> predicate, out int insertionPoint, string patchName)
        {
            insertionPoint = -1;
            
            for (int i = 0; i < codes.Count; i++)
            {
                if (predicate(codes[i], i))
                {
                    insertionPoint = i;
                    return true;
                }
            }
            
            Log.Error($"CrystalMechanoids could not find insertion point for {patchName}. This may cause the patch to not work correctly.");
            return false;
        }

        /// <summary>
        /// Safely checks if a caravan or list has mechanoids, with null safety
        /// </summary>
        public static bool HasMechanoids(IEnumerable<Pawn> pawns)
        {
            if (pawns == null) return false;
            
            foreach (Pawn p in pawns)
            {
                if (p?.RaceProps?.IsMechanoid == true)
                    return true;
            }
            return false;
        }
    }

    // Patch Dialog_FormCaravan CheckErrors so we can start a caravan with only mechanoids in the UI
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch]
    public static class Dialog_FormCaravan_CheckErrors_NoColonist_Patch {

        public static MethodBase TargetMethod() {
            return AccessTools.Method(AccessTools.Inner(typeof(Dialog_FormCaravan), "<>c"), "<CheckForErrors>b__95_0");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();
            codes[0].labels.Add(jumpLabel);

            var newCodes = PatchHelpers.GenerateIsMechanoidCheck(OpCodes.Ldarg_1);
            newCodes.Add(new CodeInstruction(OpCodes.Brfalse_S, jumpLabel));
            newCodes.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            newCodes.Add(new CodeInstruction(OpCodes.Ret));

            codes.InsertRange(0, newCodes);
            return codes.AsEnumerable();
        }
    }

    // Patch Dialog_FormCaravan CheckErrors so that mechanoids are included when the "can caravan pawns reach this item" check happens
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch]
    public static class Dialog_FormCaravan_CheckErrors_Unreachable_Patch {

        public static MethodBase TargetMethod() {
            return AccessTools.Method(AccessTools.Inner(typeof(Dialog_FormCaravan), "<>c__DisplayClass95_0"), "<CheckForErrors>b__1");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();
            codes[3].labels.Add(jumpLabel);

            var newCodes = PatchHelpers.GenerateIsMechanoidCheck(OpCodes.Ldarg_1);
            newCodes.Add(new CodeInstruction(OpCodes.Brtrue_S, jumpLabel));

            codes.InsertRange(0, newCodes);
            return codes.AsEnumerable();
        }
    }

    // Patch SplitCaravan CheckErrors so we can start a caravan with only mechanoids in the UI
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch]
    public static class Dialog_SplitCaravan_CheckErrors_Patch {

        public static MethodBase TargetMethod() {
            return AccessTools.Method(AccessTools.Inner(typeof(Dialog_SplitCaravan), "<>c"), "<CheckForErrors>b__85_0");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();
            codes[0].labels.Add(jumpLabel);

            var newCodes = PatchHelpers.GenerateIsMechanoidCheck(OpCodes.Ldarg_1);
            newCodes.Add(new CodeInstruction(OpCodes.Brfalse_S, jumpLabel));
            newCodes.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            newCodes.Add(new CodeInstruction(OpCodes.Ret));

            codes.InsertRange(0, newCodes);
            return codes.AsEnumerable();
        }
    }

    // Patch AllOwnersDowned to not count if there are Mechanoids in the caravan (otherwise Mech-only caravans can't move; 'all pawns downed in caravan')
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(Caravan), "get_AllOwnersDowned")]
    public static class Caravan_AllOwnersDowned_Patch {
        public static void Postfix(ref bool __result, ref Caravan __instance) {
            if (__instance?.pawns == null) return;
            
            foreach(Pawn p in __instance.pawns) {
                if(p?.RaceProps?.IsMechanoid == true && !p.Downed) {
                    __result = false;
                    break;
                }
            }
        }
    }

    // Patch AllOwnersHaveMentalBreak to not count if there are Mechanoids in the caravan (otherwise Mech-only caravans come up as 'all pawns have mental break')
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(Caravan), "get_AllOwnersHaveMentalBreak")]
    public static class Caravan_AllOwnersHaveMentalBreak_Patch {
        public static void Postfix(ref bool __result, ref Caravan __instance) {
            if (PatchHelpers.HasMechanoids(__instance?.pawns)) {
                __result = false;
            }
        }
    }

    // Patch CanFormOrReformCaravanNow method. Normally reforming is possible if there are no colonists; we allow reforming if there are mechanoids
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(FormCaravanComp), "get_CanFormOrReformCaravanNow")]
    public static class FormCaravanComp_CanFormOrReformCaravanNow_Patch {
        public static void Postfix(ref bool __result, ref FormCaravanComp __instance) {
            MapParent mapParent = __instance?.parent as MapParent;
            if(mapParent?.HasMap == true && mapParent.Map?.mapPawns?.SpawnedColonyMechs?.Count > 0) {
                __result = true;
            }
        }
    }

    // Patch CanReformNow method. Normally reforming is possible if there are no colonists; we allow reforming if there are mechanoids
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(FormCaravanComp), "CanReformNow")]
    public static class FormCaravanComp_CanReformNow_Patch {
        public static void Postfix(ref bool __result, ref FormCaravanComp __instance) {
            MapParent mapParent = __instance?.parent as MapParent;
            if(mapParent?.HasMap == true && mapParent.Map?.mapPawns?.SpawnedColonyMechs?.Count > 0) {
                __result = true;
            }
        }
    }

    // Patch FormCaravanComp GetGizmos method to allow the 'Reform Caravan' button to pop up if our caravan has no colonists but does have mechanoids
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch]
    public static class FormCaravanComp_GetGizmos_Patch {

        public static MethodBase TargetMethod() {
            return AccessTools.Method(AccessTools.Inner(typeof(FormCaravanComp), "<GetGizmos>d__18"), "MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();

            int insertionPoint = -1;
            if (!PatchHelpers.TryFindInsertionPoint(codes, 
                (code, i) => code.opcode == OpCodes.Castclass, 
                out insertionPoint, 
                nameof(FormCaravanComp_GetGizmos_Patch)))
            {
                return codes.AsEnumerable();
            }

            // Calculate offsets - these are based on the compiler-generated state machine structure
            const int OFFSET_TO_INSERTION = 13;
            const int OFFSET_TO_JUMP_TARGET = 20;
            
            insertionPoint += OFFSET_TO_INSERTION;
            codes[insertionPoint - OFFSET_TO_INSERTION + OFFSET_TO_JUMP_TARGET].labels.Add(jumpLabel);

            var newCodes = new List<CodeInstruction>();

            // Access the compiler-generated state machine fields to get mapParent
            newCodes.Add(new CodeInstruction(OpCodes.Ldarg_0)); 
            newCodes.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.Inner(typeof(FormCaravanComp), "<GetGizmos>d__18"), "<>8__1"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.Inner(typeof(FormCaravanComp), "<>c__DisplayClass18_0"), "mapParent"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MapParent), "get_Map"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "mapPawns"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MapPawns), "get_SpawnedColonyMechs"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "get_Count"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Brtrue, jumpLabel)); 

            codes.InsertRange(insertionPoint, newCodes);
            return codes.AsEnumerable();
        }
    }

    // Patch ShouldShowWarningForMechWithoutMechanitor method, so that if we make a caravan with mechs and no colonists (the whole point of the mod), we won't get a warning message for not having a mechanitor.
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(Dialog_FormCaravan), "ShouldShowWarningForMechWithoutMechanitor")]
    public static class Dialog_FormCaravan_ShouldShowWarningForMechWithoutMechanitor_Patch {
        public static void Postfix(ref bool __result, ref Dialog_FormCaravan __instance) {
            if (__instance?.transferables == null) return;

            foreach (TransferableOneWay transferable in __instance.transferables) {
                if (transferable?.HasAnyThing == true && transferable.AnyThing is Pawn) {
                    for (int i = 0; i < transferable.CountToTransfer; i++) {
                        if (i >= transferable.things.Count) break; // Safety check
                        
                        Pawn p = transferable.things[i] as Pawn;
                        if(p?.IsColonist == true) {
                            return;
                        }
                    }
                }
            }

            __result = false;
        }
    }

    // Patch Dialog_FormCaravan TrySend method to prevent 'Caravan has no one with social skills' warning when Caravan has no colonists anyways
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(Dialog_FormCaravan), "TrySend")]
    public static class Dialog_FormCaravan_TrySend_Patch {
    
        public static bool PawnListHasNoColonists(List<Pawn> pawns) {
            if (pawns == null) return true;
            
            foreach(Pawn p in pawns) {
                if(p?.IsColonist == true) {
                    return false;
                }
            }
            return true;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();
            
            int insertionPoint = -1;
            if (!PatchHelpers.TryFindInsertionPoint(codes, 
                (code, i) => code.opcode == OpCodes.Ldstr && code.operand.ToString() == "CaravanFoodWillRotSoonWarningDialog", 
                out insertionPoint, 
                nameof(Dialog_FormCaravan_TrySend_Patch)))
            {
                return codes.AsEnumerable();
            }

            // Calculate offsets based on the decompiled code structure
            const int OFFSET_TO_INSERTION = 5;
            const int OFFSET_TO_JUMP_TARGET = 22;
            
            insertionPoint += OFFSET_TO_INSERTION;
            codes[insertionPoint - OFFSET_TO_INSERTION + OFFSET_TO_JUMP_TARGET].labels.Add(jumpLabel);

            var newCodes = new List<CodeInstruction>();

            // Check if pawns list has no colonists before showing social skills warning
            newCodes.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AccessTools.Inner(typeof(Dialog_FormCaravan), "<>c__DisplayClass89_0"), "pawns"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Dialog_FormCaravan_TrySend_Patch), "PawnListHasNoColonists", new Type[] {typeof(List<Pawn>)}))); 
            newCodes.Add(new CodeInstruction(OpCodes.Brtrue, jumpLabel)); 
            newCodes.Add(new CodeInstruction(OpCodes.Ldloc_0)); 

            codes.InsertRange(insertionPoint, newCodes);
            return codes.AsEnumerable();
        }
    }

    // Patch CaravanUtility RandomOwner method; normally a Warning is logged if this method is called on a Caravan with no colonists, since it tries to pick randomly from 0 colonists. If the Caravan has no colonists, pick a random Mechanoid in the caravan instead.
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(CaravanUtility), "RandomOwner")]
    public static class CaravanUtility_RandomOwner_Patch {
        public static bool Prefix(ref Pawn __result, Caravan caravan) {            
            if (caravan?.PawnsListForReading == null) return true;
            
            foreach(Pawn p in caravan.PawnsListForReading) {
                if(p != null && CaravanUtility.IsOwner(p, caravan.Faction)) {
                    return true; // Has at least one owner, use original method
                }
            }
            
            // No owners found, try to find a mechanoid
            var mechanoids = caravan.PawnsListForReading.Where(p => p?.RaceProps?.IsMechanoid == true).ToList();
            if (mechanoids.Count > 0) {
                __result = mechanoids.RandomElement();
                return false;
            }

            return true; // Let original method handle it (will log warning)
        }
    }

    // Patch CaravansBattlefield CheckWonBattle method to select a Mechanoid for RecordTale if there are no colonists on the battlefield
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(CaravansBattlefield), "CheckWonBattle")]
    public static class CaravansBattlefield_CheckWonBattle_Patch {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();
            var finishJumpLabel = il.DefineLabel();
            var jumpCode = new CodeInstruction(OpCodes.Ldarg_0);
            jumpCode.labels.Add(jumpLabel);

            int insertionPoint = -1;
            if (!PatchHelpers.TryFindInsertionPoint(codes, 
                (code, i) => code.opcode == OpCodes.Stelem_Ref, 
                out insertionPoint, 
                nameof(CaravansBattlefield_CheckWonBattle_Patch)))
            {
                return codes.AsEnumerable();
            }

            insertionPoint -= 1; // Insert before Stelem_Ref
            codes[insertionPoint + 2].labels.Add(finishJumpLabel);

            var newCodes = new List<CodeInstruction>();
            
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "get_Count"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Brtrue, jumpLabel)); 

            // Use mechanoid instead
            newCodes.Add(new CodeInstruction(OpCodes.Ldarg_0));
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MapParent), "get_Map")));
            newCodes.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "mapPawns")));
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MapPawns), "get_SpawnedColonyMechs")));
            newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenCollection), "RandomElement", generics: new Type[] {typeof(Pawn)})));
            newCodes.Add(new CodeInstruction(OpCodes.Stelem_Ref)); 
            newCodes.Add(new CodeInstruction(OpCodes.Br, finishJumpLabel)); 

            // Original code path
            newCodes.Add(jumpCode);
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MapParent), "get_Map")));
            newCodes.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "mapPawns")));
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MapPawns), "get_FreeColonists"))); 
            
            codes.InsertRange(insertionPoint, newCodes);
            return codes.AsEnumerable();
        }
    }

    // Patch SettlementDefeatUtility CheckDefeated method to select a Mechanoid for RecordTale if there are no colonists on the battlefield
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(SettlementDefeatUtility), "CheckDefeated")]
    public static class SettlementDefeatUtility_CheckDefeated_Patch {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();
            var finishJumpLabel = il.DefineLabel();
            var jumpCode = new CodeInstruction(OpCodes.Ldloc_0);
            jumpCode.labels.Add(jumpLabel);

            int insertionPoint = -1;
            if (!PatchHelpers.TryFindInsertionPoint(codes, 
                (code, i) => code.opcode == OpCodes.Stelem_Ref, 
                out insertionPoint, 
                nameof(SettlementDefeatUtility_CheckDefeated_Patch)))
            {
                return codes.AsEnumerable();
            }

            insertionPoint -= 1; // Insert before Stelem_Ref
            codes[insertionPoint + 2].labels.Add(finishJumpLabel);

            var newCodes = new List<CodeInstruction>();
            
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<Pawn>), "get_Count"))); 
            newCodes.Add(new CodeInstruction(OpCodes.Brtrue, jumpLabel)); 

            // Use mechanoid instead - note: this uses Ldloc_0 for the Map instead of Ldarg_0 for MapParent
            newCodes.Add(new CodeInstruction(OpCodes.Ldloc_0));
            newCodes.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "mapPawns")));
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MapPawns), "get_SpawnedColonyMechs")));
            newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenCollection), "RandomElement", generics: new Type[] {typeof(Pawn)})));
            newCodes.Add(new CodeInstruction(OpCodes.Stelem_Ref)); 
            newCodes.Add(new CodeInstruction(OpCodes.Br, finishJumpLabel)); 

            // Original code path
            newCodes.Add(jumpCode);
            newCodes.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "mapPawns")));
            newCodes.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MapPawns), "get_FreeColonists"))); 
            
            codes.InsertRange(insertionPoint, newCodes);
            return codes.AsEnumerable();
        }
    }

    // Patch JobDriver_PrepareCaravan_GatherItems IsUsableCarrier method to allow Mechanoids to carry items when forming a caravan in the colony. This probably isn't necessary but I'm doing it anyways. I think it should allow mechs that can't Haul to be loaded like pack animals
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(JobDriver_PrepareCaravan_GatherItems), "IsUsableCarrier")]
    public static class JobDriver_PrepareCaravan_GatherItems_IsUsableCarrier_Patch {
        public static void Postfix(ref bool __result, Pawn p, Pawn forPawn, bool allowColonists) {            
            if (p != null && (p.RaceProps?.IsMechanoid == true || p.HostFaction == Faction.OfPlayer) && !p.IsBurning() && !p.Downed) {
                __result = !MassUtility.IsOverEncumbered(p);
            }
        }
    }

    // Patch LordToil_PrepareCaravan_GatherItems UpdateAllDuties so Mechanoids can gather items when forming a caravan
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(LordToil_PrepareCaravan_GatherItems), "UpdateAllDuties")]
    public static class LordToil_PrepareCaravan_GatherItems_UpdateAllDuties_Patch {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();

            int insertionPoint = -1;
            if (!PatchHelpers.TryFindInsertionPoint(codes, 
                (code, i) => code.opcode == OpCodes.Callvirt, 
                out insertionPoint, 
                nameof(LordToil_PrepareCaravan_GatherItems_UpdateAllDuties_Patch)))
            {
                return codes.AsEnumerable();
            }

            insertionPoint += 2; // Move to after the callvirt
            codes[insertionPoint + 3].labels.Add(jumpLabel);

            var newCodes = PatchHelpers.GenerateIsMechanoidCheck(OpCodes.Ldloc_1);
            newCodes.Add(new CodeInstruction(OpCodes.Brtrue_S, jumpLabel)); 

            codes.InsertRange(insertionPoint, newCodes);
            return codes.AsEnumerable();
        }
    }

    // Patch LordToil_PrepareCaravan_GatherItems LordToilTick so Mechanoids can gather items when forming a caravan
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(LordToil_PrepareCaravan_GatherItems), "LordToilTick")]
    public static class LordToil_PrepareCaravan_GatherItems_LordToilTick_Patch {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = new List<CodeInstruction>(instructions);
            
            var jumpLabel = il.DefineLabel();

            int insertionPoint = -1;
            if (!PatchHelpers.TryFindInsertionPoint(codes, 
                (code, i) => code.opcode == OpCodes.Stloc_2, 
                out insertionPoint, 
                nameof(LordToil_PrepareCaravan_GatherItems_LordToilTick_Patch)))
            {
                return codes.AsEnumerable();
            }

            insertionPoint += 1; // Move to after the stloc_2
            codes[insertionPoint + 3].labels.Add(jumpLabel);

            var newCodes = PatchHelpers.GenerateIsMechanoidCheck(OpCodes.Ldloc_2);
            newCodes.Add(new CodeInstruction(OpCodes.Brtrue_S, jumpLabel)); 

            codes.InsertRange(insertionPoint, newCodes);
            return codes.AsEnumerable();
        }
    }
    // Patch Caravan.Notify_PawnKilled so caravans with mechanoids aren't destroyed when the last human dies
    [HarmonyPatchCategory("BaseGame")]
    [HarmonyPatch(typeof(Caravan), nameof(Caravan.Notify_PawnKilled))]
    public static class Caravan_Notify_PawnKilled_Patch {
        public static bool Prefix(Caravan __instance, Pawn pawn) {
            if (__instance == null || pawn == null) return true;

            if (PatchHelpers.HasMechanoids(__instance.pawns)) {
                bool hasColonist = false;
                bool hasMech = false;
                foreach (Pawn p in __instance.pawns) {
                    if (p == pawn) continue;
                    if (p?.RaceProps?.IsMechanoid == true) hasMech = true;
                    if (p?.IsColonist == true) { hasColonist = true; break; }
                }
                if (!hasColonist && hasMech) {
                    __instance.RemovePawn(pawn);
                    return false;
                }
            }
            return true;
        }
    }
}

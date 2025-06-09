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
    /// <summary>
    /// Patch to allow mechanoids to operate scanners without triggering skill experience gain,
    /// which would cause null reference exceptions since mechanoids don't have skills.
    /// </summary>
    [HarmonyPatch]
    public static class JobDriver_OperateScanner_MakeNewToils_Patch
    {
        private const float SCANNER_EXPERIENCE_RATE = 0.035f;
        private const string PATCH_NAME = "CrystalMechanoids.ScanMechanoidsPatch";
        
        /// <summary>
        /// Dynamically finds the target method using reflection since it's a compiler-generated method.
        /// </summary>
        public static MethodBase TargetMethod()
        {
            var displayClass = AccessTools.Inner(typeof(JobDriver_OperateScanner), "<>c__DisplayClass1_0");
            return AccessTools.Method(displayClass, "<MakeNewToils>b__1");
        }

        /// <summary>
        /// Transpiler that inserts a null check for Pawn.skills before experience gain code.
        /// This prevents crashes when mechanoids (who have null skills) operate scanners.
        /// </summary>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);
            var insertIndex = FindInsertionPoint(codes);
            
            if (insertIndex == -1)
            {
                Log.Error($"[{PATCH_NAME}] Could not find insertion point for scanner experience rate!");
                return codes.AsEnumerable();
            }

            var returnLabel = CreateReturnLabel(codes, il);
            InsertSkillsNullCheck(codes, insertIndex, returnLabel);

            Log.Message($"[{PATCH_NAME}] Successfully patched scanner operation for mechanoids.");
            return codes.AsEnumerable();
        }

        /// <summary>
        /// Finds where to insert the null check by locating the experience rate constant.
        /// </summary>
        private static int FindInsertionPoint(List<CodeInstruction> codes)
        {
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && 
                    codes[i].operand is float value && 
                    Math.Abs(value - SCANNER_EXPERIENCE_RATE) < 0.001f)
                {
                    return i - 3; // Insert 3 instructions before the experience rate constant
                }
            }
            return -1;
        }

        /// <summary>
        /// Creates a label for the return statement to allow early exit.
        /// </summary>
        private static Label CreateReturnLabel(List<CodeInstruction> codes, ILGenerator il)
        {
            var returnLabel = il.DefineLabel();
            
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ret)
                {
                    codes[i].labels.Add(returnLabel);
                    break;
                }
            }
            
            return returnLabel;
        }

        /// <summary>
        /// Inserts IL instructions to check if the pawn's skills field is null,
        /// and if so, skip to the return statement (avoiding the experience gain code).
        /// </summary>
        private static void InsertSkillsNullCheck(List<CodeInstruction> codes, int insertIndex, Label returnLabel)
        {
            // Load the pawn (local variable 0) onto the stack
            codes.Insert(insertIndex, new CodeInstruction(OpCodes.Ldloc_0));
            
            // Load the pawn's skills field onto the stack
            codes.Insert(insertIndex + 1, new CodeInstruction(OpCodes.Ldfld, 
                AccessTools.Field(typeof(Pawn), nameof(Pawn.skills))));
            
            // If skills is null (mechanoids), branch to return statement
            codes.Insert(insertIndex + 2, new CodeInstruction(OpCodes.Brfalse_S, returnLabel));
        }
    }
}

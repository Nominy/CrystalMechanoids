using Verse;
using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace CrystalMechanoids
{
    [StaticConstructorOnStartup]
    public static class CrystalMechanoids
    {
        static CrystalMechanoids()
        {
            Log.Message("[CrystalMechanoids] Crystal Mechanoids mod loading...");
            
            // Initialize the color sync system first
            ColorSyncSystem.Initialize();
            
            // Initialize Harmony and apply all patches
            Harmony harmony = new Harmony("CrystalMechanoids");
            
            try
            {
                // Check if the target method exists before patching
                var mechsWindowType = typeof(RimWorld.MainTabWindow_Mechs);
                var doWindowContentsMethod = mechsWindowType.GetMethod("DoWindowContents");
                
                Log.Message($"[CrystalMechanoids] MainTabWindow_Mechs type found: {mechsWindowType != null}");
                Log.Message($"[CrystalMechanoids] DoWindowContents method found: {doWindowContentsMethod != null}");
                if (doWindowContentsMethod != null)
                {
                    Log.Message($"[CrystalMechanoids] DoWindowContents method info: {doWindowContentsMethod.DeclaringType}.{doWindowContentsMethod.Name}({string.Join(", ", doWindowContentsMethod.GetParameters().Select(p => p.ParameterType.Name))})");
                }
                
                harmony.PatchAll();
                Log.Message("[CrystalMechanoids] Main PatchAll() completed successfully");
                
                // Log what patches were applied
                var patches = harmony.GetPatchedMethods().ToList();
                Log.Message($"[CrystalMechanoids] Main applied patches to {patches.Count} methods");
                
                foreach (var method in patches)
                {
                    Log.Message($"[CrystalMechanoids] Main patched method: {method.DeclaringType?.Name}.{method.Name}");
                }
                
                // Specifically check if MainTabWindow_Mechs.DoWindowContents was patched
                if (doWindowContentsMethod != null)
                {
                    var patchInfo = Harmony.GetPatchInfo(doWindowContentsMethod);
                    if (patchInfo != null)
                    {
                        Log.Message($"[CrystalMechanoids] MainTabWindow_Mechs.DoWindowContents has {patchInfo.Postfixes.Count} postfixes");
                    }
                    else
                    {
                        Log.Warning("[CrystalMechanoids] MainTabWindow_Mechs.DoWindowContents was NOT patched!");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[CrystalMechanoids] Error in main patch application: {ex}");
            }
            
            Log.Message("[CrystalMechanoids] Crystal Mechanoids mod has been successfully loaded!");
            Log.Message("[CrystalMechanoids] Color synchronization system initialized!");
        }
    }
}
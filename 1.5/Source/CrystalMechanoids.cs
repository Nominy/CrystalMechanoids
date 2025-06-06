using Verse;
using HarmonyLib;

namespace CrystalMechanoids
{
    [StaticConstructorOnStartup]
    public static class CrystalMechanoids
    {
        static CrystalMechanoids()
        {
            Log.Message("[CrystalMechanoids] Crystal Mechanoids mod loading...");
            
            // Initialize Harmony and apply all patches
            Harmony harmony = new Harmony("CrystalMechanoids");
            harmony.PatchCategory("BaseGame");
            
            Log.Message("[CrystalMechanoids] Crystal Mechanoids mod has been successfully loaded!");
        }
    }
}
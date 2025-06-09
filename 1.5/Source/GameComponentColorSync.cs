using Verse;
using UnityEngine;

namespace CrystalMechanoids
{
    public class GameComponentColorSync : GameComponent
    {
        private int ticksSinceLastCheck = 0;
        private const int CHECK_INTERVAL = 60; // Check every 60 ticks (1 second)
        
        public GameComponentColorSync(Game game)
        {
        }
        
        public override void GameComponentTick()
        {
            ticksSinceLastCheck++;
            
            if (ticksSinceLastCheck >= CHECK_INTERVAL)
            {
                ticksSinceLastCheck = 0;
                ColorSyncSystem.CheckForColorChange();
            }
        }
        
        public override void ExposeData()
        {
            // Save/load color sync settings
            bool isEnabled = ColorSyncSystem.IsColorSyncEnabled();
            Color cachedColor = ColorSyncSystem.GetCachedMechanoidColor();
            
            Scribe_Values.Look(ref isEnabled, "colorSyncEnabled", false);
            Scribe_Values.Look(ref cachedColor, "cachedMechanoidColor", Color.white);
            
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Restore settings after loading
                if (isEnabled)
                {
                    ColorSyncSystem.ToggleColorSync(); // This will enable it
                }
                ColorSyncSystem.CacheMechanoidColor(cachedColor);
            }
        }
    }
} 
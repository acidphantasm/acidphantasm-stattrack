using acidphantasm_stattrack.Patches;
using BepInEx;
using BepInEx.Logging;

namespace acidphantasm_stattrack
{
    [BepInPlugin("com.acidphantasm.stattrack", "acidphantasm-StatTrack", "1.2.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        internal void Awake()
        {
            LogSource = Logger;

            LogSource.LogInfo("[StatTrack] loading...");

            new WeaponPatch().Enable();
            new WeaponOnShotPatch().Enable();
            new PlayerPatch().Enable();
            new GameWorldPatch().Enable();
            new MenuLoadPatch().Enable();
            new InsurancePatch().Enable();

            LogSource.LogInfo("[StatTrack] loaded!");
        }
    }
}

using acidphantasm_stattrack.Patches;
using BepInEx;
using BepInEx.Logging;

namespace acidphantasm_stattrack
{
    [BepInPlugin("phantasm.acid.stattrack", "acidphantasm-StatTrack", "1.2.0")]
    [BepInDependency("com.SPT.core", "3.10.0")]
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
            new InsurancePatch().Enable();

            LogSource.LogInfo("[StatTrack] loaded!");
        }
    }
}

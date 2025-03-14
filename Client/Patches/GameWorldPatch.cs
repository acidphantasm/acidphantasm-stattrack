using acidphantasm_stattrack.Utils;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace acidphantasm_stattrack.Patches
{
    internal class GameWorldPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.UnregisterPlayer));
        }

        [PatchPostfix]
        private static void PatchPostfix(IPlayer iPlayer)
        {
            if (iPlayer.IsYourPlayer) JsonFileUtils.EndRaidWriteData(iPlayer.ProfileId);
        }
    }
}

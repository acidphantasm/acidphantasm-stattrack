using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using StatAttributeClass = GClass3030;
using HarmonyLib;
using static acidphantasm_stattrack.Utils.Utility;
using acidphantasm_stattrack.Utils;

namespace acidphantasm_stattrack.Patches
{
    internal class WeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(typeof(Weapon), new Type[] { typeof(string), typeof(WeaponTemplate)});
        }

        [PatchPostfix]
        private static void PatchPostfix(Weapon __instance, string id, WeaponTemplate template)
        {
            StatAttributeClass statTrack = new StatAttributeClass((EItemAttributeId)EStatTrackAttributeId.Kills);
            statTrack.Name = EStatTrackAttributeId.Kills.GetName();
            statTrack.Base = () => 1f;
            statTrack.StringValue = () => JsonFileUtils.GetData(id);
            statTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
            SafelyAddAttributeToList(statTrack, __instance);

            StatAttributeClass hsTrack = new StatAttributeClass((EItemAttributeId)EStatTrackAttributeId.Headshots);
            hsTrack.Name = EStatTrackAttributeId.Headshots.GetName();
            hsTrack.Base = () => 1f;
            hsTrack.StringValue = () => JsonFileUtils.GetData(id, true);
            hsTrack.Tooltip = () => JsonFileUtils.GetData(id, true, true);
            hsTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
            SafelyAddAttributeToList(hsTrack, __instance);

        }
    }
}

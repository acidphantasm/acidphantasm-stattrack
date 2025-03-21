using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using StatAttributeClass = GClass3174;
using HarmonyLib;
using static acidphantasm_stattrack.Utils.Utility;
using acidphantasm_stattrack.Utils;
using EFT;

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
            statTrack.StringValue = () => JsonFileUtils.GetData(id, EStatTrackAttributeId.Kills);
            statTrack.Tooltip = () => JsonFileUtils.GetData(__instance.TemplateId, EStatTrackAttributeId.Kills, true);
            statTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
            SafelyAddAttributeToList(statTrack, __instance);

            StatAttributeClass hsTrack = new StatAttributeClass((EItemAttributeId)EStatTrackAttributeId.Headshots);
            hsTrack.Name = EStatTrackAttributeId.Headshots.GetName();
            hsTrack.Base = () => 1f;
            hsTrack.StringValue = () => JsonFileUtils.GetData(id, EStatTrackAttributeId.Headshots);
            hsTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
            SafelyAddAttributeToList(hsTrack, __instance);

            StatAttributeClass shotPerKillTrack = new StatAttributeClass((EItemAttributeId)EStatTrackAttributeId.ShotsPerKillAverage);
            shotPerKillTrack.Name = EStatTrackAttributeId.ShotsPerKillAverage.GetName();
            shotPerKillTrack.Base = () => 1f;
            shotPerKillTrack.StringValue = () => JsonFileUtils.GetData(id, EStatTrackAttributeId.ShotsPerKillAverage);
            shotPerKillTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
            SafelyAddAttributeToList(shotPerKillTrack, __instance);

            StatAttributeClass shotTrack = new StatAttributeClass((EItemAttributeId)EStatTrackAttributeId.Shots);
            shotTrack.Name = EStatTrackAttributeId.Shots.GetName();
            shotTrack.Base = () => 1f;
            shotTrack.StringValue = () => JsonFileUtils.GetData(id, EStatTrackAttributeId.Shots);
            shotTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
            SafelyAddAttributeToList(shotTrack, __instance);
        }
    }
    internal class WeaponOnShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Weapon), nameof(Weapon.OnShot));
        }

        [PatchPostfix]
        private static void PatchPostfix(Weapon __instance, float ammoBurnRatio, float ammoHeatFactor, float skillWeaponTreatmentFactor, BackendConfigSettingsClass.GClass1502 overheatSettings, float pastTime)
        {
            if (__instance.Owner.ID == Globals.GetPlayerProfile().ProfileId)
            {
                var weaponTpl = __instance.TemplateId;
                var weaponID = __instance.Id;

                JsonFileUtils.TemporaryAddData(weaponID, false, true);
                JsonFileUtils.TemporaryAddData(weaponTpl, false, true);
            }
        }
    }
}

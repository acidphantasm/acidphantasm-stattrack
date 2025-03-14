using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT;
using acidphantasm_stattrack.Utils;

namespace acidphantasm_stattrack.Patches
{
    internal class PlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnBeenKilledByAggressor));
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            if (!aggressor.IsYourPlayer || lethalDamageType != EDamageType.Bullet) return;

            var weapon = damageInfo.Weapon.Id;
            var weaponTpl = damageInfo.Weapon.TemplateId;

            if (bodyPart == EBodyPart.Head)
            {
                JsonFileUtils.TemporaryAddData(weapon, true, false);
                JsonFileUtils.TemporaryAddData(weaponTpl, true, false);
            }
            else
            {
                JsonFileUtils.TemporaryAddData(weapon, false, false);
                JsonFileUtils.TemporaryAddData(weaponTpl, false, false);
            }
        }
    }
}

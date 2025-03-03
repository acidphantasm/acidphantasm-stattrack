using System.Reflection;
using acidphantasm_stattrack.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace acidphantasm_stattrack.Patches
{
    public class InsurancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SocialNetworkClass), nameof(SocialNetworkClass.method_7));
        }

        [PatchPostfix]
        public static void Postfix(ChatMessageClass message)
        {
            if (message.HasRewards && message.Type == ChatShared.EMessageType.InsuranceReturn)
            {
                JsonFileUtils.LoadData();
            }
        }
    }
}

using EFT;
using SPT.Reflection.Utils;

namespace acidphantasm_stattrack
{
    internal class Globals
    {
        public static string mainProfileID;

        internal void Awake()
        {
            mainProfileID = GetPlayerProfile().ProfileId;
            Plugin.LogSource.LogInfo(mainProfileID);
        }
        public static Profile GetPlayerProfile()
        {
            return ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
        }
    }
}

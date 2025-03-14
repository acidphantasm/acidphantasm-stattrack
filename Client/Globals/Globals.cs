using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
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

        public static string GetItemLocalizedName(string itemID)
        {
            if (Singleton<ItemFactoryClass>.Instance != null)
            {
                if (Singleton<ItemFactoryClass>.Instance.GetPresetItem(itemID).LocalizedShortName() != null)
                {
                    return Singleton<ItemFactoryClass>.Instance.GetPresetItem(itemID).LocalizedShortName();
                }
            }
            return "WEAPON NAME NOT FOUND";
        }        
    }
}

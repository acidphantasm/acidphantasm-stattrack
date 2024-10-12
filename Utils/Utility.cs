using EFT.InventoryLogic;

namespace acidphantasm_stattrack.Utils
{
    public static class Utility
    {
        public enum EStatTrackAttributeId
        {
            Kills = 23,
            Headshots = 43,
        }

        public static string GetName(this EStatTrackAttributeId id)
        {
            switch (id)
            {
                case EStatTrackAttributeId.Kills:
                    return "KILLS";
                case EStatTrackAttributeId.Headshots:
                    return "HEADSHOT PERCENTAGE";
                default:
                    return id.ToString();
            }
        }

        public static void SafelyAddAttributeToList(ItemAttributeClass itemAttribute, Weapon __instance)
        {
            if (itemAttribute.Base() != 0f)
            {
                __instance.Attributes.Add(itemAttribute);
            }
        }
    }
}

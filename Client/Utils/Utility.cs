using EFT.InventoryLogic;

namespace acidphantasm_stattrack.Utils
{
    public static class Utility
    {
        public enum EStatTrackAttributeId
        {
            Kills = 23,
            Headshots = 43,
            ShotsPerKillAverage = 8,
            Shots = 48,
        }

        public static string GetName(this EStatTrackAttributeId id)
        {
            switch (id)
            {
                case EStatTrackAttributeId.Kills:
                    return "KILLS";
                case EStatTrackAttributeId.Headshots:
                    return "HEADSHOT KILL %";
                case EStatTrackAttributeId.ShotsPerKillAverage:
                    return "ROUNDS TO KILL AVG";
                case EStatTrackAttributeId.Shots:
                    return "ROUNDS FIRED";
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

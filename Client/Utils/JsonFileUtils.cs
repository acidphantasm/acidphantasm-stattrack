using Comfort.Common;
using EFT;
using EFT.UI;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using static acidphantasm_stattrack.Utils.Utility;

namespace acidphantasm_stattrack.Utils
{
    public static class JsonFileUtils
    {
        public static Dictionary<string, Dictionary<string, CustomizedObject>> WeaponInfoOutOfRaid { get; set; } = [];
        public static Dictionary<string, CustomizedObject> WeaponInfoForRaid { get; set; } = [];

        public static Dictionary<string, CustomizedObject> MergeDictionary(Dictionary<string, CustomizedObject> primaryDictionary, Dictionary<string, CustomizedObject> inRaidDictionary)
        {
            var mergedDictionary = new Dictionary<string, CustomizedObject>();
            foreach (var kvp in inRaidDictionary)
            {
                if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
                else
                {
                    mergedDictionary[kvp.Key].kills += inRaidDictionary[kvp.Key].kills;
                    mergedDictionary[kvp.Key].headshots += inRaidDictionary[kvp.Key].headshots;
                    mergedDictionary[kvp.Key].totalShots += inRaidDictionary[kvp.Key].totalShots;
                }
            }
            foreach (var kvp in primaryDictionary)
            {
                if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
                else
                {
                    mergedDictionary[kvp.Key].kills += primaryDictionary[kvp.Key].kills;
                    mergedDictionary[kvp.Key].headshots += primaryDictionary[kvp.Key].headshots;
                    mergedDictionary[kvp.Key].totalShots += primaryDictionary[kvp.Key].totalShots;
                }
            }
            return mergedDictionary;
        }
        public static void TemporaryAddData(string weaponID, bool headshot = false, bool shot = false)
        {
            var profileID = Globals.GetPlayerProfile().ProfileId;
            CustomizedObject values = new CustomizedObject();
            if (shot)
            {
                values.kills = 0;
                values.headshots = 0;
                values.totalShots = 1;
            }
            else if (headshot)
            {
                values.kills = 1;
                values.headshots = 1;
                values.totalShots = 0;
            }
            else if (!shot && !headshot)
            {
                values.kills = 1;
                values.headshots = 0;
                values.totalShots = 0;
            }
            else
            {
                values.kills = 0;
                values.headshots = 0;
                values.totalShots = 0;
                Plugin.LogSource.LogInfo("Something went wrong adding data..");
            }

            if (!WeaponInfoForRaid.ContainsKey(weaponID)) WeaponInfoForRaid.Add(weaponID, values);
            else
            {
                WeaponInfoForRaid[weaponID].kills += values.kills;
                WeaponInfoForRaid[weaponID].headshots += values.headshots;
                WeaponInfoForRaid[weaponID].totalShots += values.totalShots;
            }

        }

        public static string GetData(string weaponID, EStatTrackAttributeId attributeType, bool tooltip = false)
        {
            var profileID = Globals.GetPlayerProfile().ProfileId;
            if (WeaponInfoOutOfRaid == null)
            {
                LoadFromServer();
            }
            if (!WeaponInfoOutOfRaid.ContainsKey(profileID)) return "-";
            if (!WeaponInfoOutOfRaid[profileID].ContainsKey(weaponID)) return "-";

            string flavourText = "";
            switch (attributeType)
            {
                case EStatTrackAttributeId.Kills:
                    if (tooltip)
                    {
                        flavourText = " kills with all " + Globals.GetItemLocalizedName(weaponID);
                    }
                    string killCount = WeaponInfoOutOfRaid[profileID][weaponID].kills.ToString();
                    return killCount + flavourText;
                case EStatTrackAttributeId.Headshots:
                    if (tooltip)
                    {
                        flavourText = " headshot percent with all " + Globals.GetItemLocalizedName(weaponID);
                    }
                    string headshotPercent = (WeaponInfoOutOfRaid[profileID][weaponID].headshots / (double)WeaponInfoOutOfRaid[profileID][weaponID].kills).ToString("P1");
                    return headshotPercent + flavourText;
                case EStatTrackAttributeId.ShotsPerKillAverage:
                    if (tooltip)
                    {
                        flavourText = " rounds to kill average with all " + Globals.GetItemLocalizedName(weaponID);
                    }
                    string shotsPerKill = Math.Round(WeaponInfoOutOfRaid[profileID][weaponID].totalShots / (double)WeaponInfoOutOfRaid[profileID][weaponID].kills, 2).ToString();
                    return shotsPerKill + flavourText;
                case EStatTrackAttributeId.Shots:
                    if (tooltip)
                    {
                        flavourText = " rounds fired with all " + Globals.GetItemLocalizedName(weaponID);
                    }
                    string shotsCount = WeaponInfoOutOfRaid[profileID][weaponID].totalShots.ToString();
                    return shotsCount + flavourText;
                default:
                    return "-";
            }
        }

        public static void EndRaidMergeData()
        {
            var profileID = Globals.GetPlayerProfile().ProfileId;
            Dictionary<string, CustomizedObject> newDictionary = MergeDictionary(WeaponInfoOutOfRaid[profileID], WeaponInfoForRaid);
            WeaponInfoOutOfRaid[profileID] = newDictionary;
            SaveRaidEndInServer();
        }

        public static void SaveRaidEndInServer()
        {
            try
            {
                RequestHandler.PutJson("/stattrack/save", JsonConvert.SerializeObject(WeaponInfoOutOfRaid));
                WeaponInfoForRaid.Clear();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to save: " + ex.ToString());
                NotificationManagerClass.DisplayWarningNotification("Failed to save weapon customization - check the server");
            }
        }

        public static void LoadFromServer()
        {
            try
            {
                string payload = RequestHandler.GetJson("/stattrack/load");
                WeaponInfoOutOfRaid = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, CustomizedObject>>>(payload);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to load: " + ex.ToString());
                NotificationManagerClass.DisplayWarningNotification("Failed to load Weapon StatTrack File - check the server");
            }
        }

        public class CustomizedObject
        {
            public int kills;
            public int headshots;
            public int totalShots;
        }
    }
}

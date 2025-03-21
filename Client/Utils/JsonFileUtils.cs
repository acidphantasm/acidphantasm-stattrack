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
                    mergedDictionary[kvp.Key].timesLost += inRaidDictionary[kvp.Key].timesLost;
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
                    mergedDictionary[kvp.Key].timesLost += primaryDictionary[kvp.Key].timesLost;
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
                values.timesLost = 0;
            }
            else if (headshot)
            {
                values.kills = 1;
                values.headshots = 1;
                values.totalShots = 0;
                values.timesLost = 0;
            }
            else if (!shot && !headshot)
            {
                values.kills = 1;
                values.headshots = 0;
                values.totalShots = 0;
                values.timesLost = 0;
            }
            else
            {
                values.kills = 0;
                values.headshots = 0;
                values.totalShots = 0;
                values.timesLost = 0;
                Plugin.LogSource.LogInfo("Something went wrong adding data..");
            }

            if (!WeaponInfoForRaid.ContainsKey(weaponID)) WeaponInfoForRaid.Add(weaponID, values);
            else
            {
                WeaponInfoForRaid[weaponID].kills += values.kills;
                WeaponInfoForRaid[weaponID].headshots += values.headshots;
                WeaponInfoForRaid[weaponID].totalShots += values.totalShots;
                WeaponInfoForRaid[weaponID].timesLost += values.timesLost;
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

            int killCount = WeaponInfoOutOfRaid[profileID][weaponID].kills;
            int insuranceCount = WeaponInfoOutOfRaid[profileID][weaponID].timesLost;
            string killDeathRatio = insuranceCount > 0 ? Math.Round(killCount / (double)insuranceCount, 2).ToString() : "∞";
            string headshotPercent = Math.Round((WeaponInfoOutOfRaid[profileID][weaponID].headshots / (double)WeaponInfoOutOfRaid[profileID][weaponID].kills)*100, 1).ToString();
            string shotCount = WeaponInfoOutOfRaid[profileID][weaponID].totalShots.ToString();
            string shotsToKillAverage = Math.Round(WeaponInfoOutOfRaid[profileID][weaponID].totalShots / (double)WeaponInfoOutOfRaid[profileID][weaponID].kills, 2).ToString();

            if (tooltip)
            {
                return
                    $"All -{Globals.GetItemLocalizedName(weaponID)}- Stats" +
                    $"\n {killCount.ToString()} Kills" +
                    $"\n {killDeathRatio} Kill/Death Ratio" +
                    $"\n {headshotPercent} Headshot Kill %" +
                    $"\n {shotsToKillAverage} Rounds-To-Kill Average" +
                    $"\n {shotCount} Shots";
            }
            switch (attributeType)
            {
                case EStatTrackAttributeId.Kills:
                    var stringToReturn = insuranceCount > 1 ? $"{killCount.ToString()} K | {killDeathRatio} KD" : $"{killCount.ToString()} K | ∞ KD";
                    return stringToReturn;
                case EStatTrackAttributeId.Headshots:
                    return headshotPercent;
                case EStatTrackAttributeId.ShotsPerKillAverage:
                    return shotsToKillAverage;
                case EStatTrackAttributeId.Shots:
                    return shotCount;
                default:
                    return "-";
            }
        }

        public static void EndRaidMergeData()
        {
            var profileID = Globals.GetPlayerProfile().ProfileId;
            if (WeaponInfoOutOfRaid.ContainsKey(profileID))
            {
                Dictionary<string, CustomizedObject> newDictionary = MergeDictionary(WeaponInfoOutOfRaid[profileID], WeaponInfoForRaid);
                WeaponInfoOutOfRaid[profileID] = newDictionary;
            }
            else
            {
                WeaponInfoOutOfRaid[profileID] = WeaponInfoForRaid;
            }
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
                NotificationManagerClass.DisplayWarningNotification("Failed to save Weapon StatTrack File - check the server");
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
            public int timesLost;
        }
    }
}

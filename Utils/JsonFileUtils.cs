using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using static acidphantasm_stattrack.Utils.Utility;

namespace acidphantasm_stattrack.Utils
{
    public static class JsonFileUtils
    {
        public static Dictionary<string, Dictionary<string, int[]>> Stats { get; set; } = null;
        // Put into Stats variable, since mod keeps polling /stattrack/load
        public static Dictionary<string, int[]> WeaponInfoForRaid { get; set; } = [];

        private static readonly JsonSerializerSettings _options
            = new() { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore };

        public static void EndRaidWriteData(string profileID)
        {
            if (!Stats.ContainsKey(profileID))
            {
                Stats[profileID] = WeaponInfoForRaid;
                SaveData();
                WeaponInfoForRaid.Clear();
                return;
            }
            Dictionary<string, int[]> newDictionary = MergeDictionary(Stats[profileID], WeaponInfoForRaid);
            Stats[profileID] = newDictionary; // oldDictionary contains other profiles, update profileID key to new dictionary instead - need to load again since Stats updated

            SaveData();
            WeaponInfoForRaid.Clear();
        }

        public static Dictionary<string, int[]> MergeDictionary(Dictionary<string, int[]> ogDictionary, Dictionary<string, int[]> temporaryDictionary)
        {
            var mergedDictionary = new Dictionary<string, int[]>();
            foreach (var kvp in temporaryDictionary)
            {
                if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
                else mergedDictionary[kvp.Key] = [kvp.Value[0] + mergedDictionary[kvp.Key][0], kvp.Value[1] + mergedDictionary[kvp.Key][1], kvp.Value[2] + mergedDictionary[kvp.Key][2]];
            }
            foreach (var kvp in ogDictionary)
            {
                if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
                else
                {
                    if (ogDictionary[kvp.Key].Length != 3) mergedDictionary[kvp.Key] = [kvp.Value[0] + mergedDictionary[kvp.Key][0], kvp.Value[1] + mergedDictionary[kvp.Key][1], mergedDictionary[kvp.Key][2]];
                    else mergedDictionary[kvp.Key] = [kvp.Value[0] + mergedDictionary[kvp.Key][0], kvp.Value[1] + mergedDictionary[kvp.Key][1], kvp.Value[2] + mergedDictionary[kvp.Key][2]];
                }
            }
            return mergedDictionary;
        }
        public static void TemporaryAddData(string weaponID, bool headshot = false, bool shot = false)
        {
            int[] values;
            if (shot)
            {
                values = [0, 0, 1];
            }
            else if (headshot)
            {
                values = [1, 1, 0];
            }
            else if (!shot && !headshot)
            {
                values = [1, 0, 0];
            }
            else
            {
                values = [0, 0, 0];
                Plugin.LogSource.LogInfo("Something went wrong adding data.. adding [0, 0, 0]");
            }

            if (!WeaponInfoForRaid.ContainsKey(weaponID)) WeaponInfoForRaid.Add(weaponID, values);
            else WeaponInfoForRaid[weaponID] = [WeaponInfoForRaid[weaponID][0] + values[0], WeaponInfoForRaid[weaponID][1] + values[1], WeaponInfoForRaid[weaponID][2] + values[2]];
        }

        public static string GetData(string weaponID, EStatTrackAttributeId attributeType, bool tooltip = false)
        {
            var profileID = Globals.GetPlayerProfile().ProfileId;
            if (Stats == null) {
                LoadData();
            }
            if (!Stats.ContainsKey(profileID)) return "-";
            if (!Stats[profileID].ContainsKey(weaponID)) return "-";
            if (Stats[profileID][weaponID].Length == 3)
            {
                string flavourText = "";
                switch (attributeType)
                {
                    case EStatTrackAttributeId.Kills:
                        if (tooltip)
                        {
                            flavourText = " kills with all " + Globals.GetItemLocalizedName(weaponID);
                        }
                        string killCount = Stats[profileID][weaponID][0].ToString();
                        return killCount + flavourText;
                    case EStatTrackAttributeId.Headshots:
                        if (tooltip)
                        {
                            flavourText = " headshot percent with all " + Globals.GetItemLocalizedName(weaponID);
                        }
                        string headshotPercent = (Stats[profileID][weaponID][1] / (double)Stats[profileID][weaponID][0]).ToString("P1");
                        return headshotPercent + flavourText;
                    case EStatTrackAttributeId.ShotsPerKillAverage:
                        if (tooltip)
                        {
                            flavourText = " rounds to kill average with all " + Globals.GetItemLocalizedName(weaponID);
                        }
                        string shotsPerKill = Math.Round(Stats[profileID][weaponID][2] / (double)Stats[profileID][weaponID][0], 2).ToString();
                        return shotsPerKill + flavourText;
                    case EStatTrackAttributeId.Shots:
                        if (tooltip)
                        {
                            flavourText = " rounds fired with all " + Globals.GetItemLocalizedName(weaponID);
                        }
                        string shotsCount = Stats[profileID][weaponID][2].ToString();
                        return shotsCount + flavourText;
                    default:
                        return "-";
                }
            }
            return "-";
        }

        public static void LoadData()
        {
            try
            {
                string payload = RequestHandler.GetJson("/stattrack/load");
                Stats = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int[]>>>(payload);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to load: " + ex.ToString());
                NotificationManagerClass.DisplayWarningNotification("Failed to load Weapon StatTrack File - check the server");
            }
        }

        public static void SaveData()
        {
            try
            {
                RequestHandler.PutJson("/stattrack/save", JsonConvert.SerializeObject(Stats));
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to save: " + ex.ToString());
                NotificationManagerClass.DisplayWarningNotification("Failed to save weapon customization - check the server");
            }
        }
    }
}

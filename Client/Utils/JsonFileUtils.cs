using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static acidphantasm_stattrack.Utils.Utility;

namespace acidphantasm_stattrack.Utils
{
    public static class JsonFileUtils
    {
        public static Dictionary<string, int[]> WeaponInfoForRaid { get; set; } = [];

        private static string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


        private static readonly JsonSerializerSettings _options
            = new() { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore };

        private static void StreamWrite(object weaponData, string path)
        {
            using var streamWriter = File.CreateText(path);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            JsonSerializer.CreateDefault(_options).Serialize(jsonWriter, weaponData);
            jsonWriter.Close();
            streamWriter.Close();
        }
        public static void EndRaidWriteData(string profileID)
        {
            var path = Path.Combine(directory, profileID + ".json");

            if (!File.Exists(path))
            {
                StreamWrite(WeaponInfoForRaid, path);
                WeaponInfoForRaid.Clear();
                return;
            }

            var oldData = File.ReadAllText(path);
            var oldDictionary = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(oldData);

            Dictionary<string, int[]> newDictionary = MergeDictionary(oldDictionary, WeaponInfoForRaid);

            StreamWrite(newDictionary, path);
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
            var path = Path.Combine(directory, profileID + ".json");

            if (!File.Exists(path)) return "-";

            var jsonData = File.ReadAllText(path);
            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(jsonData);
            if (!jsonDictionary.ContainsKey(weaponID)) return "-";

            if (jsonDictionary[weaponID].Length == 3)
            {
                string flavourText = "";
                switch (attributeType)
                {
                    case EStatTrackAttributeId.Kills:
                        if (tooltip)
                        {
                            flavourText = " kills with all " + Globals.GetItemLocalizedName(weaponID);
                        }
                        string killCount = jsonDictionary[weaponID][0].ToString();
                        return killCount + flavourText;
                    case EStatTrackAttributeId.Headshots:
                        if (tooltip)
                        {
                            flavourText = " headshot percent with all " + Globals.GetItemLocalizedName(weaponID);
                        }
                        string headshotPercent = (jsonDictionary[weaponID][1] / (double)jsonDictionary[weaponID][0]).ToString("P1");
                        return headshotPercent + flavourText;
                    case EStatTrackAttributeId.ShotsPerKillAverage:
                        if (tooltip)
                        {
                            flavourText = " rounds to kill average with all " + Globals.GetItemLocalizedName(weaponID);
                        }
                        string shotsPerKill = Math.Round(jsonDictionary[weaponID][2] / (double)jsonDictionary[weaponID][0], 2).ToString();
                        return shotsPerKill + flavourText;
                    case EStatTrackAttributeId.Shots:
                        if (tooltip)
                        {
                            flavourText = " rounds fired with all " + Globals.GetItemLocalizedName(weaponID);
                        }
                        string shotsCount = jsonDictionary[weaponID][2].ToString();
                        return shotsCount + flavourText;
                    default:
                        return "-";
                }
            }

            return "-";
        }
    }
}

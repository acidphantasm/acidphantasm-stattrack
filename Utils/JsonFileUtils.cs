using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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

        public static Dictionary<string, int[]> MergeDictionary(params Dictionary<string, int[]>[] dictionaries)
        {
            var mergedDictionary = new Dictionary<string, int[]>();
            foreach (var dictionary in dictionaries)
            {
                foreach (var kvp in dictionary)
                {
                    if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
                    else mergedDictionary[kvp.Key] = [kvp.Value[0] + mergedDictionary[kvp.Key][0], kvp.Value[1] + mergedDictionary[kvp.Key][0]];
                }
            }
            return mergedDictionary;
        }
        public static void TemporaryAddData(string weaponID, bool headshot = false)
        {
            int[] values = headshot ? [1, 1] : [1, 0];
            if (!WeaponInfoForRaid.ContainsKey(weaponID)) WeaponInfoForRaid.Add(weaponID, values);
            else WeaponInfoForRaid[weaponID] = [WeaponInfoForRaid[weaponID][0] + values[0], WeaponInfoForRaid[weaponID][1] + values[1]];
        }

        public static string GetData(string weaponID, bool headshots = false, bool tooltip = false)
        {
            var profileID = Globals.GetPlayerProfile().ProfileId;
            var path = Path.Combine(directory, profileID + ".json");

            if (!File.Exists(path)) return "0";

            var jsonData = File.ReadAllText(path);
            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(jsonData);

            if (!jsonDictionary.ContainsKey(weaponID)) return "0";
            else if (headshots)
            {
                if (tooltip) return jsonDictionary[weaponID][1].ToString() + " headshots";

                string percent = (jsonDictionary[weaponID][1] / (double)jsonDictionary[weaponID][0]).ToString("P1");
                return percent;
            }
            return jsonDictionary[weaponID][0].ToString();
        }
    }
}

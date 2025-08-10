using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BloodShardTracker.Models;

namespace BloodShardTracker.Services
{
    public static class ShardStore
    {
        private static readonly string FilePath = Path.Combine(Directory.GetCurrentDirectory(), "bloodshards.json");

        public static void Save(IEnumerable<ShardDrop> drops)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(drops, options));
        }

        public static List<ShardDrop> Load()
        {
            if (!File.Exists(FilePath)) return new List<ShardDrop>();
            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<ShardDrop>>(json) ?? new List<ShardDrop>();
            }
            catch
            {
                return new List<ShardDrop>();
            }
        }

        public static void Clear()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
    }
}
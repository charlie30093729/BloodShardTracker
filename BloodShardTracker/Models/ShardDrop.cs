using System;
using System.Text.Json.Serialization;

namespace BloodShardTracker.Models
{
    public class ShardDrop
    {
        public DateTime When { get; set; } = DateTime.Now;
        public long PriceGp { get; set; }

        [JsonIgnore]
        public string PriceDisplay => string.Format("{0:N0} gp", PriceGp);
    }
}
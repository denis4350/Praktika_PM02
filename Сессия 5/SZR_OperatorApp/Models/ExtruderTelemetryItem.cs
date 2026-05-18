using Newtonsoft.Json;
using System;

namespace SZR_OperatorApp.Models
{
    public class ExtruderTelemetryItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }
        [JsonProperty("zoneNumber")]
        public int ZoneNumber { get; set; }
        [JsonProperty("zoneName")]
        public string ZoneName { get; set; }
        [JsonProperty("currentTemperature")]
        public decimal CurrentTemperature { get; set; }
        [JsonProperty("currentPressure")]
        public decimal CurrentPressure { get; set; }
        [JsonProperty("currentSpeed")]
        public int CurrentSpeed { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("temperatureMin")]
        public decimal? TemperatureMin { get; set; }
        [JsonProperty("temperatureMax")]
        public decimal? TemperatureMax { get; set; }
        [JsonProperty("pressureMin")]
        public decimal? PressureMin { get; set; }
        [JsonProperty("pressureMax")]
        public decimal? PressureMax { get; set; }

    }
}
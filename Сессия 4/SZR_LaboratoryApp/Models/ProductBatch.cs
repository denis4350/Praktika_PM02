// ProductBatch.cs
using System;
using Newtonsoft.Json;

namespace SZR_LaboratoryApp.Models
{
    public class ProductBatch
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("batchNumber")] public string batchNumber { get; set; }
        [JsonProperty("productName")] public string productName { get; set; }
        [JsonProperty("line")] public string line { get; set; }
        [JsonProperty("status")] public string status { get; set; }
        [JsonProperty("labStatus")] public string labStatus { get; set; }
        [JsonProperty("startedAt")] public DateTime? startedAt { get; set; }
        [JsonProperty("finishedAt")] public DateTime? finishedAt { get; set; }
        [JsonProperty("hasTest")] public bool hasTest { get; set; }
        [JsonProperty("hasOpenTest")] public bool hasOpenTest { get; set; }
        [JsonProperty("lastTestDate")] public DateTime? lastTestDate { get; set; }
    }
}
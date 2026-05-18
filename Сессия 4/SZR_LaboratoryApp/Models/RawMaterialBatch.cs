// RawMaterialBatch.cs
using System;
using Newtonsoft.Json;

namespace SZR_LaboratoryApp.Models
{
    public class RawMaterialBatch
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("batchNumber")] public string batchNumber { get; set; }
        [JsonProperty("supplierBatch")] public string supplierBatch { get; set; }
        [JsonProperty("materialName")] public string materialName { get; set; }
        [JsonProperty("category")] public string category { get; set; }
        [JsonProperty("supplier")] public string supplier { get; set; }
        [JsonProperty("arrivalDate")] public DateTime arrivalDate { get; set; }
        [JsonProperty("quantity")] public decimal quantity { get; set; }
        [JsonProperty("unit")] public string unit { get; set; }
        [JsonProperty("labStatus")] public string labStatus { get; set; }
        [JsonProperty("hasTest")] public bool hasTest { get; set; }
        [JsonProperty("hasOpenTest")] public bool hasOpenTest { get; set; }
        [JsonProperty("lastTestDate")] public DateTime? lastTestDate { get; set; }
        [JsonProperty("decisionAt")] public DateTime? decisionAt { get; set; }
        [JsonProperty("decisionBy")] public int? decisionBy { get; set; }
        [JsonProperty("comment")] public string comment { get; set; }
    }
}
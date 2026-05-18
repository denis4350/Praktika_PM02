using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SZR_OperatorApp.Models
{
    public class BatchInfo
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
        [JsonProperty("productName")] public string ProductName { get; set; }
        [JsonProperty("line")] public string Line { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("startedAt")] public DateTime StartedAt { get; set; }
        [JsonProperty("currentStepName")] public string CurrentStepName { get; set; }
        public List<BatchStep> Steps { get; set; }
    }
}
using System;
using Newtonsoft.Json;

namespace SZR_OperatorApp.Models
{
    public class ActiveBatch
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("productId")] public int ProductId { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
        [JsonProperty("productName")] public string ProductName { get; set; }
        [JsonProperty("line")] public string Line { get; set; }
        [JsonProperty("currentStep")] public int? CurrentStep { get; set; }
        [JsonProperty("currentStepName")] public string CurrentStepName { get; set; }
        [JsonProperty("currentStepStatus")] public string StepStatus { get; set; }
        [JsonProperty("status")] public string BatchStatus { get; set; }
        [JsonProperty("hasWarning")] public bool HasWarning { get; set; }
        [JsonProperty("hasCriticalDeviation")] public bool HasCriticalDeviation { get; set; }
        [JsonProperty("startedAt")] public DateTime? StartedAt { get; set; }
    }
}
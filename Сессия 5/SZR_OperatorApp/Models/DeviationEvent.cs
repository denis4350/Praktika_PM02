using System;
using Newtonsoft.Json;

namespace SZR_OperatorApp.Models
{
    public class DeviationEvent
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("batchId")] public int BatchId { get; set; }
        [JsonProperty("stepNumber")] public int? StepNumber { get; set; }
        [JsonProperty("eventType")] public string EventType { get; set; }
        [JsonProperty("parameterName")] public string ParameterName { get; set; }
        [JsonProperty("plannedValue")] public string PlannedValue { get; set; }
        [JsonProperty("actualValue")] public string ActualValue { get; set; }
        [JsonProperty("severity")] public string Severity { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("createdAt")] public DateTime CreatedAt { get; set; }
        [JsonProperty("createdBy")] public int CreatedBy { get; set; }
        [JsonProperty("resolvedAt")] public DateTime? ResolvedAt { get; set; }
        [JsonProperty("resolvedBy")] public int? ResolvedBy { get; set; }
    }
}
using System;
using Newtonsoft.Json;

namespace SZR_OperatorApp.Models
{
    public class BatchStep
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("stepNumber")] public int StepNumber { get; set; }
        [JsonProperty("stepType")] public string StepType { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("instruction")] public string Instruction { get; set; }
        [JsonProperty("isMandatory")] public bool IsMandatory { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("hasDeviation")] public bool HasDeviation { get; set; }
        [JsonProperty("plannedParams")] public object PlannedParams { get; set; }
        [JsonProperty("toleranceParams")] public object ToleranceParams { get; set; }
        [JsonProperty("actualParams")] public object ActualParams { get; set; }
        [JsonProperty("startedAt")] public DateTime? StartedAt { get; set; }
        [JsonProperty("finishedAt")] public DateTime? FinishedAt { get; set; }
    }
}
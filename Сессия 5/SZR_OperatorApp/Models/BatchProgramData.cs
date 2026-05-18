using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SZR_OperatorApp.Models
{
    public class BatchProgramData
    {
        [JsonProperty("batch")]
        public BatchInfoData batch { get; set; }
        [JsonProperty("steps")]
        public List<BatchStepData> steps { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
    }

    public class BatchInfoData
    {
        [JsonProperty("id")]
        public int id { get; set; }
        [JsonProperty("batchNumber")]
        public string BatchNumber { get; set; }
        [JsonProperty("productName")]
        public string productName { get; set; }
        [JsonProperty("line")]
        public string line { get; set; }
        [JsonProperty("status")]
        public string status { get; set; }
        [JsonProperty("startedAt")]
        public DateTime startedAt { get; set; }
    }

    public class BatchStepData
    {
        [JsonProperty("id")]
        public int id { get; set; }
        [JsonProperty("stepNumber")]
        public int stepNumber { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("instruction")]
        public string instruction { get; set; }
        [JsonProperty("stepType")]
        public string stepType { get; set; }
        [JsonProperty("isMandatory")]
        public bool isMandatory { get; set; }
        [JsonProperty("status")]
        public string status { get; set; }
        [JsonProperty("startedAt")]
        public DateTime? startedAt { get; set; }
        [JsonProperty("finishedAt")]
        public DateTime? finishedAt { get; set; }
        [JsonProperty("actualParams")]
        public object actualParams { get; set; }
        [JsonProperty("plannedParams")]
        public object plannedParams { get; set; }
        [JsonProperty("toleranceParams")]
        public object toleranceParams { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
    }
}
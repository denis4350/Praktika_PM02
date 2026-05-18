using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SZR_LaboratoryApp.Models
{
    public class LabTest
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("testNumber")] public string testNumber { get; set; }
        [JsonProperty("testType")] public string testType { get; set; }
        [JsonProperty("objectType")] public string objectType { get; set; }
        [JsonProperty("objectId")] public int objectId { get; set; }
        [JsonProperty("assignedAt")] public DateTime assignedAt { get; set; }
        [JsonProperty("assignedBy")] public int assignedBy { get; set; }
        [JsonProperty("executedBy")] public int? executedBy { get; set; }
        [JsonProperty("executedAt")] public DateTime? executedAt { get; set; }
        [JsonProperty("status")] public string status { get; set; }
        [JsonProperty("priority")] public string priority { get; set; }
        [JsonProperty("objectName")] public string objectName { get; set; }
        [JsonProperty("comment")] public string comment { get; set; }
        public List<LabTestParameter> parameters { get; set; }
        public string Result { get; set; }
    }

    public class LabTestParameter
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("parameterName")] public string ParameterName { get; set; }
        [JsonProperty("normMin")] public decimal? NormMin { get; set; }
        [JsonProperty("normMax")] public decimal? NormMax { get; set; }
        [JsonProperty("actualValue")] public decimal? ActualValue { get; set; }
        [JsonProperty("unit")] public string Unit { get; set; }
        [JsonProperty("isPassed")] public bool? IsPassed { get; set; }
    }
}
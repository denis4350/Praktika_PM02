using Newtonsoft.Json;
using System;

namespace SZR_OperatorApp.Models
{
    public class Notification
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
        [JsonProperty("isRead")] public bool IsRead { get; set; }
        [JsonProperty("createdAt")] public DateTime CreatedAt { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
    }
}
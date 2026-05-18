using Newtonsoft.Json;

namespace SZR_OperatorApp.Models
{
    public class Product
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("productType")] public string ProductType { get; set; }
        [JsonProperty("form")] public string Form { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
    }
}
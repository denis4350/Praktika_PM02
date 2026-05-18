using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZR_OperatorApp.Models
{
    public class UserInfoDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("login")]
        public string Login { get; set; }
        [JsonProperty("fullName")]
        public string FullName { get; set; }
        [JsonProperty("role")]
        public string Role { get; set; }
        [JsonProperty("department")]
        public string Department { get; set; }
        [JsonProperty("shift")]
        public string Shift { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
    }
}
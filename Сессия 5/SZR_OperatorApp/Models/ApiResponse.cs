using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZR_OperatorApp.Models
{
    public class ApiResponse<T>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("pagination")]
        public PaginationInfo Pagination { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
    }

    public class PaginationInfo
    {
        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
    }
    public class TokenResponseDto
    {
        [JsonProperty("accessToken")]
        public string accessToken { get; set; }
        [JsonProperty("refreshToken")]
        public string refreshToken { get; set; }
        [JsonProperty("expiresAt")]
        public DateTime expiresAt { get; set; }
        [JsonProperty("user")]
        public UserData user { get; set; }
        [JsonProperty("batchNumber")] public string BatchNumber { get; set; }

        public class UserData
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("login")]
            public string login { get; set; }
            [JsonProperty("fullName")]
            public string fullName { get; set; }
            [JsonProperty("role")]
            public string role { get; set; }
            [JsonProperty("department")]
            public string department { get; set; }
            [JsonProperty("batchNumber")] public string BatchNumber { get; set; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SZR_Production_API.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string ProductType { get; set; }
        public string Form { get; set; }
        public string Status { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public Product()
        {
            Code = string.Empty;
            Name = string.Empty;
            ProductType = string.Empty;
            Form = string.Empty;
            Status = "Активен";
            CreatedAt = DateTime.Now;
        }
    }
}
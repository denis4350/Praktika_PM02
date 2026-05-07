using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SZR_Production_API.Models
{
    public class RawMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        [Required, MaxLength(20)]
        public string Unit { get; set; }

        public bool IsActive { get; set; } = true;

        public RawMaterial()
        {
            Code = "";
            Name = "";
            Category = "";
            Unit = "";
        }
    }
}
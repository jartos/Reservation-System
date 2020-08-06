using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace First.Models
{
    public class Resort
    {
        public int ResortId { get; set; }
        [Required]
        public string ResortName { get; set; }
        [JsonIgnore]
        public List<Cabin> Cabins { get; set; }
        [JsonIgnore]
        public List<Activity> Activities { get; set; }
    }
}

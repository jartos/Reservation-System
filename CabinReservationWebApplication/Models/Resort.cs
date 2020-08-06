using CabinReservationWebApplication.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CabinReservationWebApplication.Models
{
    public class Resort
    {
        public int ResortId { get; set; }
        [Required]
        [Display(Name = "Toimipiste")]
        public string ResortName { get; set; }
        public List<Cabin> Cabins { get; set; }
        public List<Activity> Activities { get; set; }
        
        [NotMapped]
        public decimal CabinsReservationsPercentange { get; set; }
        [NotMapped]
        public int ActivitiesReservationsCount { get; set; }
    }
}

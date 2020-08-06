using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace First.Models
{
    public class ActivityReservation
    {
        public int ActivityReservationId { get; set; }
        public int CabinReservationId { get; set; }
        public int ActivityId { get; set; }
        public DateTime ActivityReservationTime { get; set; }
        [JsonIgnore]
        public CabinReservation CabinReservation { get; set; }
        public Activity Activity { get; set; }
    }
}

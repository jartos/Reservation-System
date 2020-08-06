using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace First.Models
{
    public class Activity
    {
        public int ActivityId { get; set; }
        public int ResortId { get; set; }
        public string PostalCode { get; set; }
        public string ActivityProvider { get; set; }
        public string ActivityName { get; set; }
        public string Address { get; set; }
        public decimal ActivityPrice { get; set; }
        public string Description { get; set; }
        public Resort Resort { get; set; }
        public Post Post { get; set; }
        [JsonIgnore]
        public List<ActivityReservation> ActivityReservations { get; set; }

    }
}

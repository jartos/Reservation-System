using First.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CabinReservationAPI.Models
{
    public class CabinImage
    {
        public int CabinImageId { get; set; }
        public int CabinId { get; set; }
        public string ImageUrl { get; set; }
        [JsonIgnore]
        public Cabin Cabin { get; set; }
    }
}

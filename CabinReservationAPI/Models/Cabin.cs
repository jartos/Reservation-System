using CabinReservationAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace First.Models
{
    public class Cabin
    {
        public int CabinId { get; set; }
        public int ResortId { get; set; }
        public int PersonId { get; set; }
        [Required]
        public string PostalCode { get; set; }
        [Required]
        public string CabinName { get; set; }
        [Required]
        public string Address { get; set; }
        public decimal CabinPricePerDay { get; set; }
        public string Description { get; set; }
        public decimal Area { get; set; }
        public int Rooms { get; set; }
        public Resort Resort { get; set; }
        public Person Person { get; set; }
        public Post Post { get; set; }
        [JsonIgnore]
        public List<CabinReservation> CabinReservations { get; set; }
        public List<CabinImage> CabinImages { get; set; }
    }
}

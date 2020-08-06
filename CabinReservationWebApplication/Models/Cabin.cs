using CabinReservationWebApplication.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CabinReservationWebApplication.Models
{
    public class Cabin
    {
        [DisplayName("Mökki Id")]
        public int CabinId { get; set; }

        [DisplayName("Toimipiste Id")]
        public int ResortId { get; set; }
        [DisplayName("Henkilö Id")]
        public int PersonId { get; set; }
        [Required]
        [DisplayName("Postinumero")]
        public string PostalCode { get; set; }
        [Required]
        [DisplayName("Majoituspaikan nimi")]
        public string CabinName { get; set; }
        [Required]
        [DisplayName("Osoite")]
        public string Address { get; set; }
        [DisplayName("Hinta € / vrk")]
        public decimal CabinPricePerDay { get; set; }
        [StringLength(9999)]
        [DisplayName("Kuvaus")]
        public string Description { get; set; }
        [DisplayName("Neliöt")]
        public decimal Area { get; set; }
        [DisplayName("Makuuhuoneet")]
        public int Rooms { get; set; }
        public Resort Resort { get; set; }
        public Person Person { get; set; }
        public Post Post { get; set; }
        public List<CabinReservation> CabinReservations { get; set; }
        
        [NotMapped]
        public decimal ReservationPercentange { get; set; }
        public List<CabinImage> CabinImages { get; set; }
    }
}

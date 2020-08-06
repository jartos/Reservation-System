using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CabinReservationWebApplication.Models
{
    public class Person
    {
        public int PersonId { get; set; }
        public string PostalCode { get; set; }
        [Display(Name = "Henkilötunnus")]
        public string SocialSecurityNumber { get; set; }
        [Display(Name = "Etunimi")]
        public string FirstName { get; set; }
        [Display(Name = "Sukunimi")]
        public string LastName { get; set; }
        [Display(Name = "Puhelinnumero")]
        public string PhoneNumber { get; set; }
        [Display(Name = "Osoite")]
        public string Address { get; set; }
        [Display(Name = "Sähköposti")]
        public string Email { get; set; }
        [NotMapped]
        [Display(Name = "Rooli")]
        public string Role { get; set; }
        public Post Post { get; set; }
        public List<CabinReservation> CabinReservations { get; set; }
        public List<Cabin> Cabins { get; set; }
    }
}

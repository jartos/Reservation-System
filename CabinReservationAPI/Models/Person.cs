using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace First.Models
{
    public class Person
    {
        public int PersonId { get; set; }
        [Required]
        public string PostalCode { get; set; }
        [Required]
        public string SocialSecurityNumber { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string PhoneNumber{ get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string Email { get; set; }
        public Post Post { get; set; }
        [JsonIgnore]
        public List<CabinReservation> CabinReservations { get; set; }
        [JsonIgnore]
        public List<Cabin> Cabins { get; set; }
    }
}

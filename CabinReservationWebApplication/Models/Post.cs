using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CabinReservationWebApplication.Models
{
    public class Post
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "Postinumero")]
        public string PostalCode { get; set; }
        [Display(Name = "Postitoimipaikka")]
        public string City { get; set; }
        public List<Cabin> Cabins { get; set; }
        public List<Person> Persons { get; set; }
        public List<Activity> Activities { get; set; }

    }
}

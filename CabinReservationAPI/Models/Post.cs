using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace First.Models
{
    public class Post
    {
        [Display(Name = "Postinumero")]
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string PostalCode { get; set; }
        [Required]
        [Display(Name = "Postitoimipaikka")]
        public string City { get; set; }
        [JsonIgnore]
        public List<Cabin> Cabins { get; set; }
        [JsonIgnore]
        public List<Person> Persons { get; set; }
        [JsonIgnore]
        public List<Activity> Activities { get; set; }

    }
}

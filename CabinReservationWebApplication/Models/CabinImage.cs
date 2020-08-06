using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CabinReservationWebApplication.Models
{
    public class CabinImage
    {
        [Display(Name = "Kuvan id")]
        public int CabinImageId { get; set; }
        [Display(Name = "Mökin id")]
        public int CabinId { get; set; }
        [Display(Name = "Url-osoite")]
        public string ImageUrl { get; set; }
        public Cabin Cabin { get; set; }
        [NotMapped]
        public IFormFile Files { get; set; }
    }
}

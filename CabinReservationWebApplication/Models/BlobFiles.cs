using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CabinReservationWebApplication.Models
{
    public class BlobFiles
    {
        [Display(Name = "Tiedoston nimi")]
        public string FileName { get; set; }
        [Display(Name = "Koko")]
        public string FileSize { get; set; }
        [Display(Name = "Muokattu viimeksi")]
        public string ModifiedOn { get; set; }
    }
}

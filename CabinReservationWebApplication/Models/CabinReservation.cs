using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CabinReservationWebApplication.Models
{
    public class CabinReservation
    {
        public int CabinReservationId { get; set; }
        public int CabinId { get; set; }
        public int PersonId { get; set; }
        [DisplayName("Varausaika")]
        public DateTime ReservationBookingTime { get; set; }
        [DisplayName("Aloitus pvm")]
        public DateTime ReservationStartDate { get; set; }
        [DisplayName("Päättymis pvm")]
        public DateTime ReservationEndDate { get; set; }
        public Cabin Cabin { get; set; }
        public Person Person { get; set; }
        public List<ActivityReservation> ActivityReservations { get; set; }
        public List<Invoice> Invoices { get; set; }
    }
}

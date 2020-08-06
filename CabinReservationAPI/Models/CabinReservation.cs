using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace First.Models
{
    public class CabinReservation
    {
        public int CabinReservationId { get; set; }
        public int CabinId { get; set; }
        public int PersonId { get; set; }
        public DateTime ReservationBookingTime { get; set; }
        public DateTime ReservationStartDate { get; set; }
        public DateTime ReservationEndDate { get; set; }
        public Cabin Cabin { get; set; }
        public Person Person { get; set; }
        
        public List<ActivityReservation> ActivityReservations { get; set; }
        public List<Invoice> Invoices { get; set; }
    }
}

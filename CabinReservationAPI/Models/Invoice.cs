using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace First.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int CabinReservationId { get; set; }
        public DateTime DateOfExpiry { get; set; }
        public bool PaidYesNo { get; set; }
        public decimal Vat { get; set; }
        public decimal Discount { get; set; }
        public decimal InvoiceTotalAmount { get; set; }
        [JsonIgnore]
        public CabinReservation CabinReservation { get; set; }
        // This is for that user can get Invoices Paid-status Both=0/No=1/Yes=2
        [NotMapped]
        public int Status { get; set; }
    }
}

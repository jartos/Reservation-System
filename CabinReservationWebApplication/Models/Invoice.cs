using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CabinReservationWebApplication.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int CabinReservationId { get; set; }
        [Display(Name = "Eräpäivä")]
        public DateTime DateOfExpiry { get; set; }
        [Display(Name = "Maksettu")]
        public bool PaidYesNo { get; set; }
        [Display(Name = "Arvonlisävero")]
        public decimal Vat { get; set; }
        [Display(Name = "Alennusprosentti")]
        public decimal Discount { get; set; }
        [Display(Name = "Kokonaissumma")]
        public decimal InvoiceTotalAmount { get; set; }
        public CabinReservation CabinReservation { get; set; }
        // This is for that user can get Invoices Paid-status Both=0/No=1/Yes=2
        [NotMapped]
        [Display(Name = "Tila")]
        public int Status { get; set; }
    }
}

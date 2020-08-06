using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CabinReservationWebApplication.Areas.Identity.Data;
using CabinReservationWebApplication.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;

namespace CabinReservationWebApplication.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly ServiceRepository _service;
        private readonly IConverter _converter;
        private readonly IConfiguration _configuration;
        private readonly UserManager<CabinReservationWebApplicationUser> _userManager;

        public InvoicesController(ServiceRepository service, IConverter converter, IConfiguration configuration, UserManager<CabinReservationWebApplicationUser> userManager)
        {
            _service = service;
            _converter = converter;
            _configuration = configuration;
            _userManager = userManager;
        }

        // GET: Invoices/Index
        // Returns view where Administrator can search Invoices or CabinOwner can search his own Cabin Invoices
        [Authorize(Roles = "Administrator, CabinOwner")]
        public ActionResult Index(int success)
        {
            // If request came from Create/Edit/Delete
            switch (success)
            {
                case 1:
                    ViewBag.Message = "Lasku lisätty onnistuneesti!";
                    break;
                case 2:
                    ViewBag.Message = "Laskua muokattu onnistuneesti!";
                    break;
                case 3:
                    ViewBag.Message = "Lasku poistettu onnistuneesti!";
                    break;
                default:
                    ViewBag.Message = null;
                    break;
            }

            ViewBag.FirstEntry = true;

            List<SelectListItem> Statuses = new List<SelectListItem>();
            Statuses.Add(new SelectListItem() { Value = "0", Text = "Kaikki" });
            Statuses.Add(new SelectListItem() { Value = "1", Text = "Ei Maksettu" });
            Statuses.Add(new SelectListItem() { Value = "2", Text = "Maksettu" });
            ViewBag.Statuses = Statuses;

            return View();
        }

        // POST: Invoices/Index
        // Gets List of Invoices by search conditions
        [HttpPost]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<IActionResult> Index(Invoice invoice)
        {
            var invoices = await _service.GetInvoices(User, invoice);

            // Getting CabinReservations in Invoices because JsonIgnore-attribute
            if (invoices != null)
            {
                foreach (var item in invoices)
                {
                    item.CabinReservation = await _service.GetCabinReservation(User, item.CabinReservationId);
                }
            }

            ViewBag.Invoices = invoices;
            ViewBag.FirstEntry = false;

            if (invoice.CabinReservation.ReservationStartDate != DateTime.MinValue) ViewBag.Starting = invoice.CabinReservation.ReservationStartDate.ToString("dd'.'MM'.'yyyy");
            if (invoice.CabinReservation.ReservationEndDate != DateTime.MinValue) ViewBag.Ending = invoice.CabinReservation.ReservationEndDate.ToString("dd'.'MM'.'yyyy");

            List<SelectListItem> Statuses = new List<SelectListItem>();
            Statuses.Add(new SelectListItem() { Value = "0", Text = "Kaikki" });
            Statuses.Add(new SelectListItem() { Value = "1", Text = "Ei maksettu" });
            Statuses.Add(new SelectListItem() { Value = "2", Text = "Maksettu" });
            ViewBag.Statuses = Statuses;

            return View();
        }

        // GET: Invoices/Details/5
        // Returns view with Invoice Details
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var invoice = await _service.GetInvoice(User, id);

                invoice.CabinReservation = await _service.GetCabinReservation(User, invoice.CabinReservationId);

                return View(invoice);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Invoices/Create/5
        // Returns view where Administrator/CabinOwner can create Invoice in CabinReservation
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Create(int id)
        {
            try
            {
                // Getting CabinReservation in Invoice
                var cabinReservation = await _service.GetCabinReservation(User, id);

                // Counting calculated total amount
                decimal activitiesTotalPrice = 0;
                if (cabinReservation.ActivityReservations != null)
                {
                    foreach (var item in cabinReservation.ActivityReservations)
                    {
                        activitiesTotalPrice += item.Activity.ActivityPrice;
                    }
                }
                decimal duration = (cabinReservation.ReservationEndDate - cabinReservation.ReservationStartDate).Days;

                // Setting Calculated total amount and date of expiry
                ViewBag.CalculatedTotalAmount = (duration * cabinReservation.Cabin.CabinPricePerDay) + activitiesTotalPrice;
                ViewBag.CalculatedDateOfExpiry = cabinReservation.ReservationEndDate.AddDays(30);

                ViewBag.CabinReservationId = cabinReservation.CabinReservationId;

                return View();
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: Invoices/Create/5
        // Creates new Invoice
        [Authorize(Roles = "Administrator, CabinOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Invoice invoice)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool postInvoice = await _service.PostInvoice(User, invoice);

                    if (postInvoice) return RedirectToAction("Index", new { success = 1 });
                }

                ViewBag.CalculatedTotalAmount = invoice.InvoiceTotalAmount;
                ViewBag.CalculatedDateOfExpiry = invoice.DateOfExpiry;

                ViewBag.CabinReservationId = invoice.CabinReservationId;

                return View(invoice);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Invoices/Edit/5
        // Returns view where Administrator/CabinOwner can edit Invoice
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var invoice = await _service.GetInvoice(User, id);

                ViewBag.DateOfExpiry = invoice.DateOfExpiry;

                invoice.CabinReservation = await _service.GetCabinReservation(User, invoice.CabinReservationId);

                return View(invoice);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: Invoices/Edit/5
        // Edits Invoice
        [Authorize(Roles = "Administrator, CabinOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, Invoice invoice)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool editInvoice = await _service.PutInvoice(User, id, invoice);

                    if (editInvoice) return RedirectToAction("Index", new { success = 2 });
                }

                return View(invoice);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Invoices/Delete/5
        // Returns view where Administrator can delete Invoice
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var invoice = await _service.GetInvoice(User, id);

                invoice.CabinReservation = await _service.GetCabinReservation(User, invoice.CabinReservationId);

                return View(invoice);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: Invoices/Delete/5
        // Deletes Invoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> Delete(int id, Invoice invoice)
        {
            try
            {
                bool deleteInvoice = await _service.DeleteInvoice(User, id);

                if (deleteInvoice) return RedirectToAction("Index", new { success = 3 });

                return View("ErrorPage");
            }
            catch
            {
                return View();
            }
        }


        //-------------------------------------------------------------------------------------------------------------------- PDF

        // GET: Invoices/Details/5
        public async Task<ActionResult> Pdf(int id)
        {
            try
            {
                return File(await CreateInvoicePdfStream(id), "application/pdf");
            }
            catch
            {
                return BadRequest();
            }
        }

        private async Task<byte[]> CreateInvoicePdfStream(int id)
        {


            var invoiceDetails = await _service.GetInvoice(User, id);

            if (invoiceDetails != null)
            {
                invoiceDetails.CabinReservation = await _service.GetCabinReservation(User, invoiceDetails.CabinReservationId);
            }

            var globalSettings = new GlobalSettings
            {
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 30, Bottom = 30, Left = 20, Right = 20 },
                DocumentTitle = "Lasku " + id
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = InvoicePdfGenerator.GetHTMLString(invoiceDetails),
                WebSettings = { DefaultEncoding = "utf-8", UserStyleSheet = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "pdf.css") }
            };

            var pdf = new HtmlToPdfDocument
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);


        }

        //---------------------------------------------------------------------------------------------------- Send email

        public async Task<IActionResult> SendMail(int id)
        {

            var invoiceDetails = await _service.GetInvoice(User, id);

            if (invoiceDetails != null)
            {
                invoiceDetails.CabinReservation = await _service.GetCabinReservation(User, invoiceDetails.CabinReservationId);
            }

            var globalSettings = new GlobalSettings
            {
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 30, Bottom = 30, Left = 20, Right = 20 },
                DocumentTitle = "Lasku " + id
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = InvoicePdfGenerator.GetHTMLString(invoiceDetails),
                WebSettings = { DefaultEncoding = "utf-8", UserStyleSheet = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "pdf.css") }
            };

            var pdf = new HtmlToPdfDocument
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            byte[] file = _converter.Convert(pdf);

            string filename = "lasku.pdf";
            Attachment attachment = new Attachment(new MemoryStream(file), filename);

            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(_configuration.GetValue<string>("Smtp:Server"));
                mail.From = new MailAddress(_configuration.GetValue<string>("Smtp:FromAddress"));
                
                mail.To.Add(invoiceDetails.CabinReservation.Person.Email);

                mail.Subject = "Lasku";
                mail.Body = "Hei, tässä on laskusi.";
                mail.Attachments.Add(attachment);

                SmtpServer.Port = _configuration.GetValue<int>("Smtp:Port");
                SmtpServer.Credentials = new NetworkCredential(_configuration.GetValue<string>("Smtp:FromAddress"), _configuration.GetValue<string>("Smtp:Password"));
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);

                return View();
            }
            catch
            {
                return BadRequest("Sähköpostin lähettäminen epäonnistui");
            }

        }
    }
}

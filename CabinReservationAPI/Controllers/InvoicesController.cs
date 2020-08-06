using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CabinReservationSystemAPI.Models;
using First.Models;
using Microsoft.AspNetCore.Authorization;

namespace CabinReservationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public InvoicesController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/Invoices/Id=5
        // Returns Invoice by InvoiceId
        // User must be role Administrator or CabinOwner can get his own Cabin Invoice and own CabinReservation Invoice or Customer can get his own CabinReservation Invoice
        [HttpGet("Id={id}")]
        public async Task<ActionResult<Invoice>> GetInvoiceByInvoiceId(int id)
        {
            try
            {
                var invoice = await _context.Invoice
                    .Where(invoice => invoice.InvoiceId == id)
                    .FirstOrDefaultAsync();

                invoice.CabinReservation = await _context.CabinReservation
                    .Where(cabinReservation => cabinReservation.CabinReservationId == invoice.CabinReservationId)
                    .Include(cabinReservation => cabinReservation.Cabin).ThenInclude(cabin => cabin.Person)
                    .Include(cabinReservation => cabinReservation.Cabin.Resort)
                    .Include(cabinReservation => cabinReservation.Person)
                    .FirstOrDefaultAsync();

                if (User.IsInRole("Administrator") || invoice.CabinReservation.Cabin.Person.Email == User.Identity.Name || invoice.CabinReservation.Person.Email == User.Identity.Name)
                {
                    if (invoice == null) return NotFound();
                    return invoice;
                }

                return Unauthorized();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // POST: api/Invoices
        // Creates new Invoice by Invoice
        // User must be in Administrator or CabinOwner can create new Invoice only in his own Cabin
        [HttpPost]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<IActionResult> PostInvoice(Invoice invoice)
        {
            try
            {
                // Getting and checking that CabinOwner owns the Cabin what is in Invoice
                if (User.IsInRole("CabinOwner"))
                {
                    var cabinReservation = await _context.CabinReservation.Where(cabinReservation => cabinReservation.CabinReservationId == invoice.CabinReservationId)
                        .Include(cabinReservation => cabinReservation.Cabin).ThenInclude(cabin => cabin.Person).FirstOrDefaultAsync();

                    if (cabinReservation.Cabin.Person.Email != User.Identity.Name) return Unauthorized();
                }

                _context.Invoice.Add(invoice);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // PUT: api/Invoices/5
        // Change Invoice information by InvoiceId and Invoice
        // User must be in Administrator or CabinOwner can edit his own Cabin Invoice
        [HttpPut("Id={id}")]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<IActionResult> PutInvoice(int id, Invoice invoice)
        {
            try
            {
                // Getting and checking that CabinOwner owns the Cabin what is in Invoice
                if (User.IsInRole("CabinOwner"))
                {
                    var cabinReservation = await _context.CabinReservation.Where(cabinReservation => cabinReservation.Cabin.Person.Email == User.Identity.Name).FirstOrDefaultAsync();
                    if (cabinReservation == null) return Unauthorized();
                }

                if (id != invoice.InvoiceId) return BadRequest();

                _context.Entry(invoice).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch 
            {
                return StatusCode(500);
            }
        }

        // DELETE: api/Invoices/5
        // Delete Invoice by InvoiceId
        // User must be role Administrator
        [HttpDelete("Id={id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<CabinReservation>> DeleteInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoice.FindAsync(id);

                if (invoice == null) return NotFound();

                _context.Invoice.Remove(invoice);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/Invoices/ResortName=ResortName/CabinName=CabinName/FirstName=FirstName/LastName=LastName/Starting=01-01-2000/Ending=01-01-2020/Status=0
        // Returns Invoices by ResortName, CabinName, FirstName, LastName, Starting, Ending, Status
        // User must be role Administrator or CabinOwner can get his own Cabins Invoices
        [Authorize(Roles = "Administrator, CabinOwner")]
        [HttpGet("ResortName={ResortName}/CabinName={CabinName}/FirstName={FirstName}/LastName={LastName}/Starting={Starting}/Ending={Ending}/Status={Status}")]
        public async Task<ActionResult<IEnumerable<CabinReservation>>> GetCabinReservations(string ResortName, string CabinName, string FirstName, string LastName, string Starting, string Ending, string Status)
        {
            try
            {

                // If parameter is - set parameter to empty string
                if (ResortName == "-") ResortName = "";
                if (CabinName == "-") CabinName = "";
                if (FirstName == "-") FirstName = "";
                if (LastName == "-") LastName = "";
                if (Starting == "-") Starting = DateTime.MinValue.ToString();
                if (Ending == "-") Ending = DateTime.MaxValue.ToString();

                IEnumerable<Invoice> invoices = new List<Invoice>();

                // Getting Paid-status = Both
                if (User.IsInRole("Administrator"))
                {
                    invoices = await _context.Invoice
                        .Where(invoice => invoice.CabinReservation.Cabin.Resort.ResortName.ToUpper().Contains(ResortName.ToUpper()))
                        .Where(invoice => invoice.CabinReservation.Cabin.CabinName.ToUpper().Contains(CabinName.ToUpper()))
                        .Where(invoice => invoice.CabinReservation.Person.FirstName.ToUpper().Contains(FirstName.ToUpper()))
                        .Where(invoice => invoice.CabinReservation.Person.LastName.ToUpper().Contains(LastName.ToUpper()))
                        .Where(invoice => invoice.DateOfExpiry >= DateTime.Parse(Starting))
                        .Where(invoice => invoice.DateOfExpiry <= DateTime.Parse(Ending))
                        .OrderBy(invoice => invoice.DateOfExpiry)
                        .ToListAsync();
                }

                // Getting only CabinOwners own Cabins & Paid-status = Both
                if (User.IsInRole("CabinOwner"))
                {
                    invoices = await _context.Invoice
                        .Where(invoice => invoice.CabinReservation.Cabin.Resort.ResortName.ToUpper().Contains(ResortName.ToUpper()))
                        .Where(invoice => invoice.CabinReservation.Cabin.CabinName.ToUpper().Contains(CabinName.ToUpper()))
                        .Where(invoice => invoice.CabinReservation.Person.LastName.ToUpper().Contains(LastName.ToUpper()))
                        .Where(invoice => invoice.DateOfExpiry >= DateTime.Parse(Starting))
                        .Where(invoice => invoice.DateOfExpiry <= DateTime.Parse(Ending))
                        .Where(invoice => invoice.CabinReservation.Cabin.Person.Email == User.Identity.Name)
                        .OrderBy(invoice => invoice.DateOfExpiry)
                        .ToListAsync();
                }

                // If Paid-status = Not Paid
                if (Status == "1") invoices = invoices.Where(invoice => invoice.PaidYesNo == false).ToList();

                // If Paid-status = Paid
                else if (Status == "2") invoices = invoices.Where(invoice => invoice.PaidYesNo == true).ToList();

                if (invoices.Count() == 0) return NotFound();
                return Ok(invoices);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        private bool InvoiceExists(int id)
        {
            return _context.Invoice.Any(e => e.InvoiceId == id);
        }
    }
}
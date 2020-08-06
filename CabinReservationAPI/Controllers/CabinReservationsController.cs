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
    public class CabinReservationsController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public CabinReservationsController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/CabinReservations
        // Returns User all CabinReservations
        [HttpGet]
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult<IEnumerable<CabinReservation>>> GetCabinReservations()
        {
            try
            {
                var cabinReservations = await _context.CabinReservation
                    .Where(cabinReservation => cabinReservation.Person.Email == User.Identity.Name)
                    .Include(cabinReservation => cabinReservation.Cabin.Resort)
                    .Include(cabinReservation => cabinReservation.Person)
                    .Include(cabinReservation => cabinReservation.ActivityReservations)
                    .OrderBy(cabinReservation => cabinReservation.ReservationStartDate)
                    .ToListAsync();

                if (cabinReservations.Count() == 0) return NotFound();
                return Ok(cabinReservations);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/CabinReservations/5
        // Returns Cabinreservation by CabinReservationId
        // User must be role Administrator or Customer can get only own CabinReservations or CabinOwner can get his own CabinReservation and own Cabin CabinReservation
        [HttpGet("{id}")]
        public async Task<ActionResult<CabinReservation>> CabinReservationsByReservationId(int id)
        {
            try
            {
                var cabinReservation = await _context.CabinReservation
                        .Where(cabinReservation => cabinReservation.CabinReservationId == id)
                        .Include(cabinReservation => cabinReservation.ActivityReservations).ThenInclude(activityReservation => activityReservation.Activity)
                        .Include(cabinReservation => cabinReservation.Cabin).ThenInclude(cabin => cabin.Resort)
                        .Include(cabinReservation => cabinReservation.Cabin).ThenInclude(cabin => cabin.Post)
                        .Include(cabinReservation => cabinReservation.Cabin).ThenInclude(cabin => cabin.Person)
                        .Include(cabinReservation => cabinReservation.Person).ThenInclude(person => person.Post)
                        .Include(cabinReservation => cabinReservation.Invoices).FirstOrDefaultAsync();

                if (User.IsInRole("Administrator") || cabinReservation.Person.Email == User.Identity.Name || cabinReservation.Cabin.Person.Email == User.Identity.Name)
                {
                    if (cabinReservation == null) return NotFound();
                    return cabinReservation;
                }

                return Unauthorized();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/CabinReservations/Cabins/5
        // Returns Cabin all CabinReservations by CabinId
        // If Administrator or CabinOwner getting his own CabinCabinReservations, include Cabin, Person
        [HttpGet("Cabin/{id}")]
        public async Task<ActionResult<IEnumerable<CabinReservation>>> GetCabinReservationsByCabinId(int id)
        {
            try
            {
                List<CabinReservation> cabinReservations = await _context.CabinReservation
                    .Where(reservation => reservation.CabinId == id)
                    .Include(reservation => reservation.Cabin).ThenInclude(cabin => cabin.Resort)
                    .Include(reservation => reservation.Cabin).ThenInclude(cabin => cabin.Person)
                    .Include(reservation => reservation.Person)
                    .ToListAsync();

                if (cabinReservations == null) return NotFound();

                if (User.IsInRole("Administrator") || cabinReservations[0].Cabin.Person.Email == User.Identity.Name)
                {
                    return cabinReservations;
                }

                // If user is Customer or CabinOwner doesnt own Cabin, remove Cabin and Person to response
                foreach (var item in cabinReservations)
                {
                    item.Cabin = null;
                    item.Person = null;
                }

                return cabinReservations;
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // POST: api/CabinReservations
        // Creates a new CabinReservation with an unique id.
        // Customer/CabinOwner can create CabinReservation by own PersonId or Administrator can create CabinReservation all PersonId:s
        [HttpPost]
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult<CabinReservation>> PostCabinReservation(CabinReservation cabinReservation)
        {
            try
            {
                // Last moment "kikkailua", had to do this because locally it works great, but when publishing it sets Start- and EndDates 1 day greater than selected
                // This may not work all cases/timezones
                //cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.ToUniversalTime();
                //cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.ToUniversalTime();
                cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.AddHours(3);
                cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.AddHours(3);
                cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.Date;
                cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.Date;

                if (cabinReservation.ReservationStartDate >= cabinReservation.ReservationEndDate) return BadRequest();

                // Getting list of days between ReservationStartDate - ReservationEndDate
                var suggestedDays = Enumerable.Range(0, 1 + cabinReservation.ReservationEndDate.Subtract(cabinReservation.ReservationStartDate).Days)
                   .Select(offset => cabinReservation.ReservationStartDate.AddDays(offset))
                   .ToArray();
                // Checking that Cabin is not reserved by given time
                foreach (var item in suggestedDays)
                {
                    var checkAvailability = await _context.CabinReservation
                        .Where(cabinRes => cabinRes.CabinId == cabinReservation.CabinId)
                        .Where(cabinRes => cabinRes.ReservationStartDate.AddDays(1) == item || cabinRes.ReservationEndDate.AddDays(-1) == item)
                        .FirstOrDefaultAsync();

                    if (checkAvailability != null) return BadRequest();
                }


                _context.Add(cabinReservation);

                if (User.IsInRole("CabinOwner") || User.IsInRole("Customer"))
                {
                    cabinReservation.Person = await _context.Person.Where(person => person.Email == User.Identity.Name).FirstOrDefaultAsync();
                    cabinReservation.PersonId = cabinReservation.Person.PersonId;
                }
                else if (User.IsInRole("Administrator"))
                {
                    cabinReservation.Person = await _context.Person.Where(person => person.PersonId == cabinReservation.PersonId).FirstOrDefaultAsync();
                }

                else return Unauthorized();

                cabinReservation.ReservationBookingTime = DateTime.Now;

                // Counting Activities TotalPrice
                decimal activitiesTotalPrice = 0;
                if (cabinReservation.ActivityReservations != null)
                {
                    foreach (var item in cabinReservation.ActivityReservations)
                    {
                        var activity = await _context.Activity.Where(activity => activity.ActivityId == item.ActivityId).FirstOrDefaultAsync();
                        activitiesTotalPrice += activity.ActivityPrice;
                    }
                }

                // Creating and adding new Invoice to CabinReservation
                Invoice invoice = new Invoice();
                var cabin = await _context.Cabin.Where(cabin => cabin.CabinId == cabinReservation.CabinId).FirstOrDefaultAsync();
                decimal duration = (cabinReservation.ReservationEndDate - cabinReservation.ReservationStartDate).Days;
                invoice.DateOfExpiry = cabinReservation.ReservationEndDate.AddDays(30);
                invoice.InvoiceTotalAmount = (duration * cabin.CabinPricePerDay) + activitiesTotalPrice;

                cabinReservation.Invoices = new List<Invoice>();
                cabinReservation.Invoices.Add(invoice);

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // PUT: api/CabinReservations/5
        // Change CabinReservation information by CabinReservation
        // Administrator can change all CabinReservations, CabinOwner can change own CabinReservations and own Cabins CabinReservations, Customer can change own CabinReservations
        [HttpPut]
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<IActionResult> PutCabinReservation(CabinReservation cabinReservation)
        {
            try
            {
                //// Last moment "kikkailua", had to do this because locally it works great, but when publishing it sets Start- and EndDates 1 day greater than selected
                //// This may not work all cases/timezones
                //cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.ToUniversalTime();
                //cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.ToUniversalTime();
                cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.AddHours(3);
                cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.AddHours(3);
                cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.Date;
                cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.Date;

                if (cabinReservation.ReservationStartDate >= cabinReservation.ReservationEndDate) return BadRequest();

                // Getting list of days between ReservationStartDate - ReservationEndDate
                var suggestedDays = Enumerable.Range(0, 1 + cabinReservation.ReservationEndDate.Subtract(cabinReservation.ReservationStartDate).Days)
                   .Select(offset => cabinReservation.ReservationStartDate.AddDays(offset))
                   .ToArray();
                // Checking that Cabin is not reserved by given time
                foreach (var item in suggestedDays)
                {
                    var checkAvailability = await _context.CabinReservation
                        .Where(cabinRes => cabinRes.CabinId == cabinReservation.CabinId)
                        .Where(cabinRes => cabinRes.ReservationStartDate.AddDays(1) == item || cabinRes.ReservationEndDate.AddDays(-1) == item)
                        .Where(cabinRes => cabinRes.CabinReservationId != cabinReservation.CabinReservationId)
                        .FirstOrDefaultAsync();

                    if (checkAvailability != null) return BadRequest();
                }

                // Getting old CabinReservation
                var oldCabinReservation = await _context.CabinReservation
                .Where(oldReservation => oldReservation.CabinReservationId == cabinReservation.CabinReservationId)
                .Include(oldReservation => oldReservation.Cabin).ThenInclude(cabin => cabin.Person)
                .Include(oldReservation => oldReservation.Person)
                .Include(oldReservation => oldReservation.ActivityReservations)
                .Include(oldReservation => oldReservation.Invoices)
                .FirstOrDefaultAsync();
                if (oldCabinReservation == null) return NotFound();

                // Dont allow edit if old CabinReservation ReservationStartDate is earlier than tomorrow
                if (oldCabinReservation.ReservationStartDate < DateTime.Now.AddDays(1)) return BadRequest();

                // Checking authorizing
                if (User.IsInRole("Customer") && oldCabinReservation.Person.Email != User.Identity.Name) return Unauthorized();

                if (User.IsInRole("CabinOwner") &&
                oldCabinReservation.Cabin.Person.Email != User.Identity.Name &&
                oldCabinReservation.Person.Email != User.Identity.Name) return Unauthorized();

                _context.Attach(oldCabinReservation);

                oldCabinReservation.ReservationBookingTime = DateTime.Now;
                oldCabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.Date;
                oldCabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.Date;

                // Removing all old ActivityReservations
                oldCabinReservation.ActivityReservations.RemoveAll(a => a.ActivityReservationId >= 0);

                // Adding new ActivityReservations
                decimal activitiesTotalPrice = 0;
                var activityReservations = cabinReservation.ActivityReservations;
                if (activityReservations != null)
                {
                    for (int i = 0; i < activityReservations.Count(); i++)
                    {
                        oldCabinReservation.ActivityReservations.Add(activityReservations[i]);

                        var activity = await _context.Activity.Where(activity => activity.ActivityId == activityReservations[i].ActivityId).FirstOrDefaultAsync();
                        activitiesTotalPrice += activity.ActivityPrice;
                    }
                }

                // Removing old Invoices
                oldCabinReservation.Invoices.RemoveAll(i => i.CabinReservationId == cabinReservation.CabinReservationId);

                // Creating and adding new Invoice to CabinReservation
                Invoice invoice = new Invoice();
                var cabin = await _context.Cabin.Where(cabin => cabin.CabinId == cabinReservation.CabinId).FirstOrDefaultAsync();

                decimal duration = (cabinReservation.ReservationEndDate - cabinReservation.ReservationStartDate).Days;
                invoice.DateOfExpiry = cabinReservation.ReservationEndDate.AddDays(30);
                invoice.InvoiceTotalAmount = (duration * cabin.CabinPricePerDay) + activitiesTotalPrice;

                oldCabinReservation.Invoices = new List<Invoice>();
                oldCabinReservation.Invoices.Add(invoice);

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // DELETE: api/CabinReservations/5
        // Delete CabinReservation by CabinReservationId
        // User must be role Administrator or Customer can delete only own CabinReservations or CabinOwner can delete his own CabinReservations and own Cabin CabinReservations
        // TODO: Check this later, now it Deletes Invoices also, what if User has paid Invoice already
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult<CabinReservation>> DeleteCabinReservation(int id)
        {
            try
            {
                var cabinReservation = await _context.CabinReservation
                    .Where(cabinReservation => cabinReservation.CabinReservationId == id)
                    .Include(cabinReservation => cabinReservation.Cabin).ThenInclude(cabinReservation => cabinReservation.Person)
                    .Include(cabinReservation => cabinReservation.Person)
                    .FirstOrDefaultAsync();

                // Administrator can delete also old CabinReservations
                if (User.IsInRole("Administrator"))
                {
                    if (cabinReservation == null) return NotFound();

                    _context.CabinReservation.Remove(cabinReservation);

                    await _context.SaveChangesAsync();

                    return NoContent();
                }

                if (User.Identity.Name == cabinReservation.Cabin.Person.Email || User.Identity.Name == cabinReservation.Person.Email)
                {
                    // User or CabinOwner cant delete CabinReservation if ReservationStartDate is earlier than tomorrow
                    if (cabinReservation.ReservationStartDate < DateTime.Now.AddDays(1)) return BadRequest();

                    if (cabinReservation == null) return NotFound();

                    _context.CabinReservation.Remove(cabinReservation);

                    await _context.SaveChangesAsync();

                    return NoContent();
                }

                return Unauthorized();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/CabinReservations/ResortName/CabinName/PersonLastName/StartDate/EndDate
        // Returns Cabinreservations by Cabin.Resort.ResortName, Cabin.CabinName, Person.LastName, ReservationStartDate, ReservationEndDate
        // User must be in Administrator or CabinOwner can get only his own Cabin CabinReservations
        [HttpGet("{ResortName}/{CabinName}/{PersonLastName}/{StartDate}/{EndDate}")]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult<IEnumerable<CabinReservation>>> GetCabinReservation(string ResortName, string CabinName, string PersonLastName, string StartDate, string EndDate)
        {
            try
            {
                // If parameter is - set parameter to empty string
                if (ResortName == "-") ResortName = "";
                if (CabinName == "-") CabinName = "";
                if (PersonLastName == "-") PersonLastName = "";
                if (StartDate == "-") StartDate = DateTime.MinValue.ToString();
                if (EndDate == "-") EndDate = DateTime.MaxValue.ToString();

                var cabinReservations = new List<CabinReservation>();

                if (User.IsInRole("Administrator"))
                {
                    cabinReservations = await _context.CabinReservation
                        .Where(cabinReservation => cabinReservation.Cabin.Resort.ResortName.ToUpper().Contains(ResortName.ToUpper()))
                        .Where(cabinReservation => cabinReservation.Cabin.CabinName.ToUpper().Contains(CabinName.ToUpper()))
                        .Where(cabinReservation => cabinReservation.Person.LastName.ToUpper().Contains(PersonLastName.ToUpper()))

                        .Where(cabinReservation => cabinReservation.ReservationStartDate >= DateTime.Parse(StartDate) || cabinReservation.ReservationEndDate >= DateTime.Parse(StartDate))
                        .Where(cabinReservation => cabinReservation.ReservationEndDate <= DateTime.Parse(EndDate) || cabinReservation.ReservationStartDate <= DateTime.Parse(EndDate))

                        .Include(cabinReservation => cabinReservation.Cabin.Resort)
                        .Include(cabinReservation => cabinReservation.Person)
                        .OrderBy(cabinReservation => cabinReservation.ReservationStartDate)
                        .ToListAsync();
                }

                // CabinOwner getting only his own Cabin CabinReservations
                else
                {
                    var checkPerson = await _context.Person.Where(person => person.Email == User.Identity.Name).FirstOrDefaultAsync();

                    cabinReservations = await _context.CabinReservation
                        .Where(cabinReservation => cabinReservation.Cabin.Resort.ResortName.ToUpper().Contains(ResortName.ToUpper()))
                        .Where(cabinReservation => cabinReservation.Cabin.CabinName.ToUpper().Contains(CabinName.ToUpper()))
                        .Where(cabinReservations => cabinReservations.Cabin.PersonId == checkPerson.PersonId)
                        .Where(cabinReservation => cabinReservation.Person.LastName.ToUpper().Contains(PersonLastName.ToUpper()))

                        .Where(cabinReservation => cabinReservation.ReservationStartDate >= DateTime.Parse(StartDate) || cabinReservation.ReservationEndDate >= DateTime.Parse(StartDate))
                        .Where(cabinReservation => cabinReservation.ReservationEndDate <= DateTime.Parse(EndDate) || cabinReservation.ReservationStartDate <= DateTime.Parse(EndDate))

                        .Include(cabinReservation => cabinReservation.Cabin.Resort)
                        .Include(cabinReservation => cabinReservation.Person)
                        .OrderBy(cabinReservation => cabinReservation.ReservationStartDate)
                        .ToListAsync();
                }

                if (cabinReservations.Count() == 0) return NotFound();
                return Ok(cabinReservations);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/CabinReservations/Starting=01-01-2000/01-01-2020/1,2,3
        // Returns Cabinreservations by StartingDate, EndingDate, ResortIds
        // User must be role Administrator
        [HttpGet("Starting={Starting}/Ending={Ending}/ResortIds={ResortIds}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<CabinReservation>>> GetCabinReservations(string Starting, string Ending, string ResortIds)
        {
            try
            {
                List<int> resortIds = ResortIds.Split(',').Select(Int32.Parse).ToList(); ;

                var cabinReservations = await _context.CabinReservation
                    .Where(cabinReservation => resortIds.Contains(cabinReservation.Cabin.ResortId))

                    .Where(cabinReservation => cabinReservation.ReservationStartDate >= DateTime.Parse(Starting) || cabinReservation.ReservationEndDate >= DateTime.Parse(Starting))
                    .Where(cabinReservation => cabinReservation.ReservationEndDate <= DateTime.Parse(Ending) || cabinReservation.ReservationStartDate <= DateTime.Parse(Ending))

                    .Include(cabinReservation => cabinReservation.Cabin.Resort)
                    .Include(cabinReservation => cabinReservation.Person)
                    .Include(cabinReservation => cabinReservation.ActivityReservations)
                    .ToListAsync();

                if (cabinReservations.Count() == 0) return NotFound();
                return Ok(cabinReservations);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
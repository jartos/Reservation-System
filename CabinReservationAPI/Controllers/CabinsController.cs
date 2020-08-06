using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CabinReservationSystemAPI.Models;
using First.Models;
using Microsoft.AspNetCore.Authorization;

namespace CabinReservationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CabinsController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public CabinsController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/Cabins/5
        // Returns Cabin by CabinId
        // If user in in role Administrator or CabinOwner getting his own cabin, response includes Person
        [HttpGet("{id}")]
        public async Task<ActionResult<Cabin>> GetCabin(int id)
        {
            try
            {
                var cabin = await _context.Cabin.Where(cabin => cabin.CabinId == id)
                    .Include(cabin => cabin.Resort)
                    .Include(cabin => cabin.Person)
                    .Include(cabin => cabin.CabinImages)
                    .FirstOrDefaultAsync();

                if (User.IsInRole("Administrator") || cabin.Person.Email == User.Identity.Name)
                {
                    if (cabin == null) return NotFound();
                    return cabin;
                }

                // If user is Customer or CabinOwner dont owns Cabin, remove Person to response
                if (cabin == null) return NotFound();
                cabin.Person = null;
                return cabin;

            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/Cabins
        // Returns CabinOwner all Cabins
        [HttpGet]
        [Authorize(Roles = "CabinOwner")]
        public async Task<ActionResult<IEnumerable<Cabin>>> GetCabinOwnerCabins()
        {
            try
            {
                var cabins = await _context.Cabin
                    .Where(cabin => cabin.Person.Email == User.Identity.Name)
                    .Include(cabin => cabin.Person)
                    .Include(cabin => cabin.Resort)
                    .Include(cabin => cabin.Post)
                    .ToListAsync();
                if (cabins == null) return NotFound();
                return cabins;
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // PUT: api/Cabins/5
        // Change Cabin information by CabinId and Cabin
        // User must be in Administrator or CabinOwner can edit his own Cabin
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<IActionResult> PutCabin(int id, Cabin cabin)
        {
            try
            {
                if (id != cabin.CabinId) return BadRequest();

                // If CabinOwner, check that he can edit only his own Cabin
                if (User.IsInRole("CabinOwner"))
                {
                    var checkPerson = await _context.Person.Where(person => person.Email == User.Identity.Name).FirstOrDefaultAsync();
                    if (cabin.PersonId != checkPerson.PersonId) return Unauthorized();
                }

                _context.Entry(cabin).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();

            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CabinExists(id)) return NotFound();
                else throw;
            }
        }

        // |------------------------------OK------------------------------|
        // POST: api/Cabins
        // Creates a new Cabin with an unique id
        // User must be in Administrator or CabinOwner can create Cabin by own PersonId
        [HttpPost]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult<Cabin>> PostCabin([FromBody] Cabin cabin)
        {
            try
            {
                // If CabinOwner, check that he can create Cabin only by own PersonId
                if (User.IsInRole("CabinOwner"))
                {
                    var checkPerson = await _context.Person.Where(person => person.Email == User.Identity.Name).FirstOrDefaultAsync();
                    if (cabin.PersonId != checkPerson.PersonId) return Unauthorized();
                }

                _context.Cabin.Add(cabin);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // DELETE: api/Cabins/5
        // Delete cabin by CabinId
        // User must be in Administrator or CabinOwner can delete his own Cabin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult<Cabin>> DeleteCabin(int id)
        {
            try
            {
                var cabin = await _context.Cabin.Where(cabin => cabin.CabinId == id).Include(cabin => cabin.Person).FirstOrDefaultAsync();

                // If CabinOwner, check that he can delete only his own Cabin
                if (User.IsInRole("CabinOwner"))
                {
                    var checkPerson = await _context.Person.Where(person => person.Email == User.Identity.Name).FirstOrDefaultAsync();
                    if (cabin.PersonId != checkPerson.PersonId) return Unauthorized();
                }

                if (cabin == null) return NotFound();

                _context.Cabin.Remove(cabin);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/Cabins/ResortName/CabinName/PersonLastName
        // Returns Cabins by ResortName, CabinName, PersonLastName
        // User must be role Administrator
        [HttpGet("{ResortName}/{CabinName}/{PersonLastName}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<Cabin>>> GetCabins(string ResortName, string CabinName, string PersonLastName)
        {
            try
            {
                // If parameter is - set parameter to empty string
                if (ResortName == "-") ResortName = "";
                if (CabinName == "-") CabinName = "";
                if (PersonLastName == "-") PersonLastName = "";

                var cabins = await _context.Cabin
                    .Where(cabin => cabin.Resort.ResortName.ToUpper().Contains(ResortName.ToUpper()))
                    .Where(cabin => cabin.CabinName.ToUpper().Contains(CabinName.ToUpper()))
                    .Where(cabin => cabin.Person.LastName.ToUpper().Contains(PersonLastName.ToUpper()))
                    .Include(cabin => cabin.Resort)
                    .Include(cabin => cabin.Person)
                    .ToListAsync();

                if (cabins.Count() == 0) return NotFound();
                return Ok(cabins);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/Cabins/SearchWord//Arrival=01-10-2020/Departure=01-01-2021/Rooms=5
        // Returns Cabins by SearchWord, Arrival, Departure, Rooms
        [HttpGet("{SearchWord}/Arrival={Arrival}/Departure={Departure}/Rooms={Rooms}")]
        public async Task<ActionResult<IEnumerable<Cabin>>> GetCabins(string SearchWord, string Arrival, string Departure, string Rooms)
        {
            try
            {
                // If parameter is - set parameter to empty string
                if (SearchWord == "-") SearchWord = "";
                if (Arrival == "-") Arrival = DateTime.Now.ToString();
                if (Departure == "-") Departure = DateTime.MaxValue.ToString();

                // Getting Cabins by SearchWord and Rooms
                var cabins = await _context.Cabin
                    .Where(cabin => cabin.Resort.ResortName.ToUpper().Contains(SearchWord.ToUpper()) || cabin.CabinName.ToUpper().Contains(SearchWord.ToUpper()))
                    .Where(cabin => cabin.Rooms >= Convert.ToInt32(Rooms))
                    .Include(cabin => cabin.Resort)
                    .Include(cabin => cabin.CabinImages)
                    .OrderBy(cabin => cabin.Rooms)
                    .ToListAsync();

                DateTime arrival = DateTime.Parse(Arrival);
                DateTime departure = DateTime.Parse(Departure);

                double interval = (departure - arrival).TotalDays;

                // Removing Cabin in Cabins if that is full reserved between Arrival and Departure 
                List<int> cabinsToRemove = new List<int>();
                foreach (var cabin in cabins)
                {
                    var cabinIsFree = await _context.CabinReservation
                        .Where(cabinReservation => cabinReservation.CabinId == cabin.CabinId)

                        .Where(cabinReservation => cabinReservation.ReservationStartDate >= arrival || cabinReservation.ReservationEndDate >= arrival)
                        .Where(cabinReservation => cabinReservation.ReservationEndDate <= departure || cabinReservation.ReservationStartDate <= departure)

                        .ToListAsync();

                    // TODO: Must confirm that this calculates right
                    double reservedDaysCount = 0;
                    foreach (var cabinReservation in cabinIsFree)
                    {
                        reservedDaysCount += (cabinReservation.ReservationEndDate - cabinReservation.ReservationStartDate).TotalDays;

                        if (cabinReservation.ReservationStartDate < arrival) reservedDaysCount -= (arrival - cabinReservation.ReservationStartDate).TotalDays;
                        if (cabinReservation.ReservationEndDate > departure) reservedDaysCount -= (cabinReservation.ReservationEndDate - departure).TotalDays;
                    }

                    if (reservedDaysCount >= interval)
                    {
                        cabinsToRemove.Add(cabin.CabinId);
                    }
                }

                cabins.RemoveAll(cabin => cabinsToRemove.Exists(cabinId => cabinId == cabin.CabinId));

                if (cabins.Count() == 0) return NotFound();
                return Ok(cabins);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        private bool CabinExists(int id)
        {
            return _context.Cabin.Any(e => e.CabinId == id);
        }

    }
}

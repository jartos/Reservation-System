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
    public class ActivityReservationsController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public ActivityReservationsController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/ActivityReservations/All
        // Returns all Activityreservations
        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<ActivityReservation>>> GetActivityReservations()
        {
            try
            {
                List<ActivityReservation> activityReservations = await _context.ActivityReservation.ToListAsync();
                if (activityReservations.Count() == 0) return NotFound();
                return Ok(activityReservations);
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        // GET: api/ActivityReservations/5
        // Returns ActivityReservation by ActivityReservationId
        // User must be role Administrator
        // TODO: Can User get own ActivityReservation? Maybe not, because he can get own CabinReservations and ActivityReservations are linked in that
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ActivityReservation>> GetActivityReservation(int id)
        {
            try
            {
                var activityReservation = await _context.ActivityReservation
                    .Where(activityReservation => activityReservation.ActivityReservationId == id)
                    .Include(activityReservation => activityReservation.Activity)
                    .FirstOrDefaultAsync();

                if (activityReservation == null) return NotFound();
                return activityReservation;
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // |------------------------------OK------------------------------|
        // GET: api/ActivityReservations/CabinReservations/5
        // Returns CabinReservation all ActivityReservations by CabinReservationId
        [HttpGet("CabinReservations/{id}")]
        public async Task<ActionResult<IEnumerable<ActivityReservation>>> GetActivityReservationsByCabinReservationId(int id)
        {
            try
            {
                List<ActivityReservation> reservations = await _context.ActivityReservation.Where(reservation => reservation.CabinReservationId == id).ToListAsync();
                if (reservations == null) return NotFound();
                return reservations;
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // |------------------------------OK------------------------------|
        // GET: api/ActivityReservations/Activities/5
        // Returns Activity all ActivityReservations by ActivityId
        [HttpGet("Activities/{id}")]
        public async Task<ActionResult<IEnumerable<ActivityReservation>>> GetActivityReservationsByActivityId(int id)
        {
            try
            {
                List<ActivityReservation> reservations = await _context.ActivityReservation.Where(reservation => reservation.ActivityId == id).ToListAsync();
                if (reservations == null) return NotFound();
                return reservations;
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // |------------------------------OK------------------------------|
        // PUT: api/ActivityReservations/5
        // Change ActivityReservation information by ActivityReservationId
        [HttpPut("{id}")]
        public async Task<IActionResult> PutActivityReservation(int id, ActivityReservation activityReservation)
        {
            if (id != activityReservation.ActivityReservationId) return BadRequest();

            _context.Entry(activityReservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActivityReservationExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // |------------------------------OK------------------------------|
        // POST: api/ActivityReservations
        // Creates a new ActivityReservation with an unique id.
        [HttpPost]
        public async Task<ActionResult<ActivityReservation>> PostActivityReservation(ActivityReservation activityReservation)
        {
            try
            {
                _context.ActivityReservation.Add(activityReservation);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetActivityReservation", new { id = activityReservation.ActivityReservationId }, activityReservation);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // |------------------------------OK------------------------------|
        // DELETE: api/ActivityReservations/5
        // Delete ActivityReservation by ActivityReservationId
        [HttpDelete("{id}")]
        public async Task<ActionResult<ActivityReservation>> DeleteActivityReservation(int id)
        {
            try
            {
                var activityReservation = await _context.ActivityReservation.FindAsync(id);
                if (activityReservation == null) return NotFound();

                _context.ActivityReservation.Remove(activityReservation);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // |------------------------------OK------------------------------|
        // GET: api/ActivityReservations
        // Returns ActivityReservations by CabinReservation.Cabin.Resort.ResortName, Activity.ActivityName, Activity.ActivityProvider, CabinReservation.Person.LastName, starting for and ending for
        // User must be role Administrator
        [HttpGet("{ResortName}/{ActivityName}/{ActivityProvider}/{PersonLastName}/{Starting}/{Ending}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<ActivityReservation>>> GetCabinReservation(string ResortName, string ActivityName, string ActivityProvider, string PersonLastName, string Starting, string Ending)
        {
            try
            {
                // If parameter is - set parameter to empty string
                if (ResortName == "-") ResortName = "";
                if (ActivityName == "-") ActivityName = "";
                if (ActivityProvider == "-") ActivityProvider = "";
                if (PersonLastName == "-") PersonLastName = "";
                if (Starting == "-") Starting = DateTime.MinValue.ToString();
                if (Ending == "-") Ending = DateTime.MaxValue.ToString();

                var activityReservations = await _context.ActivityReservation
                    .Where(activityReservation => activityReservation.CabinReservation.Cabin.Resort.ResortName.ToUpper().Contains(ResortName.ToUpper()))
                    .Where(activityReservation => activityReservation.Activity.ActivityName.ToUpper().Contains(ActivityName.ToUpper()))
                    .Where(activityReservation => activityReservation.Activity.ActivityProvider.ToUpper().Contains(ActivityProvider.ToUpper()))

                    .Where(activityReservation => activityReservation.ActivityReservationTime >= DateTime.Parse(Starting))
                    .Where(activityReservation => activityReservation.ActivityReservationTime <= DateTime.Parse(Ending))

                    .Where(activityReservation => activityReservation.CabinReservation.Person.LastName.ToUpper().Contains(PersonLastName.ToUpper()))
                    .Include(activityReservation => activityReservation.Activity)
                    .OrderBy(activityReservation => activityReservation.ActivityReservationTime)
                    .ToListAsync();

                if (activityReservations.Count() == 0) return NotFound();
                return Ok(activityReservations);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        private bool ActivityReservationExists(int id)
        {
            return _context.ActivityReservation.Any(e => e.ActivityReservationId == id);
        }
    }
}

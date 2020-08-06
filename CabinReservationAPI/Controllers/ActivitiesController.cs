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
    public class ActivitiesController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public ActivitiesController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/Activities/5
        // Returns activity by ActivityId
        [HttpGet("{id}")]
        public async Task<ActionResult<Activity>> GetActivity(int id)
        {
            try
            {
                var activity = await _context.Activity.Where(activity => activity.ActivityId == id)
                  .Include(activity => activity.Resort)
                  .FirstOrDefaultAsync();
                if (activity == null) return NotFound();
                return activity;
            }
            catch
            {
                return StatusCode(500);
            }

        }

        // GET: api/Activities/Resort/5
        // Returns Activities by ResortId
        [HttpGet("Resorts/{id}")]
        public async Task<ActionResult<IEnumerable<Activity>>> GetResortActivities(int id)
        {
            try
            {
                List<Activity> activities = await _context.Activity
                         .Include(a => a.Resort)
                         .Where(a => a.ResortId == id)
                         .ToListAsync();

                if (activities.Count == 0) return NotFound();
                return Ok(activities);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // PUT: api/Activities/5
        // Updates activity information by ActivityId and Activity
        // User must be role Administrator
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PutActivity(int id, Activity activity)
        {
            if (id != activity.ActivityId)
            {
                return BadRequest();
            }

            _context.Entry(activity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActivityExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Activities
        // Creates a new activity
        // User must be role Administrator
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Activity>> PostActivity([FromBody] Activity activity)
        {
            try
            {
                _context.Activity.Add(activity);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // DELETE: api/Activities/5
        // Deletes activity by ActivityId
        // User must be role Administrator
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Activity>> DeleteActivity(int id)
        {
            var activity = await _context.Activity.FindAsync(id);
            if (activity == null) return NotFound();

            _context.Activity.Remove(activity);
            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/Activities/ResortName/ActivityName/ActivityProvider
        // Returns Activities by ResortName, ActivityName, ActivityProvider
        // User must be role Administrator
        [HttpGet("{ResortName}/{ActivityName}/{ActivityProvider}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<Activity>>> GetActivities(string ResortName, string ActivityName, string ActivityProvider)
        {
            try
            {
                // If parameter is - set parameter to empty string
                if (ResortName == "-") ResortName = "";
                if (ActivityName == "-") ActivityName = "";
                if (ActivityProvider == "-") ActivityProvider = "";

                var activities = await _context.Activity
                    .Where(activity => activity.Resort.ResortName.ToUpper().Contains(ResortName.ToUpper()))
                    .Where(activity => activity.ActivityName.ToUpper().Contains(ActivityName.ToUpper()))
                    .Where(activity => activity.ActivityProvider.ToUpper().Contains(ActivityProvider.ToUpper()))
                    .Include(activity => activity.Resort)
                    .ToListAsync();

                if (activities.Count() == 0) return NotFound();
                return Ok(activities);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        private bool ActivityExists(int id)
        {
            return _context.Activity.Any(e => e.ActivityId == id);
        }
    }
}

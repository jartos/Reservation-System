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
    public class ResortsController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public ResortsController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/Resorts/All
        // Returns all Resorts
        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<Resort>>> GetAll()
        {
            try
            {
                List<Resort> resorts = await _context.Resort.ToListAsync();
                if (resorts.Count() == 0) return NotFound();
                return Ok(resorts);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/Resorts/5
        // Returns Resort by ResortId
        // User must be role Administrator
        [HttpGet("{id}")]
        public async Task<ActionResult<Resort>> GetResort(int id)
        {
            // Checks User-role in token
            if (false == User.IsInRole("Administrator")) return Unauthorized();

            try
            {
                var resort = await _context.Resort.Where(resort => resort.ResortId == id).FirstOrDefaultAsync();
                if (resort == null) return NotFound();
                return resort;
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/Resorts/Cabins/5
        // Returns Resort Cabins by ResortId
        // TODO: Maybe this should be in CabinsController?
        [HttpGet("Cabins/{id}")]
        public async Task<ActionResult<IEnumerable<Cabin>>> GetResortCabins(int id)
        {
            try
            {
                List<Cabin> cabins = await _context.Cabin
                         .Where(cabin => cabin.ResortId == id)
                         .Include(cabin => cabin.Post)
                         .Include(cabin => cabin.Resort)
                         .OrderBy(cabin => cabin.CabinPricePerDay)
                         .ToListAsync();

                if (cabins.Count == 0) return NotFound();
                return Ok(cabins);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // POST: api/Resorts
        // Creates a new Resort with an unique id
        // User must be role Administrator
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Resort>> PostResort([FromBody] Resort resort)
        {
            try
            {
                _context.Resort.Add(resort);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch
            {
                return StatusCode(400);
            }
        }

        // PUT: api/Resorts/5
        // Changes Resort information (ResortName)
        // User must be role Administrator
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PutResort(int id, [FromBody] Resort resort)
        {
            if (id != resort.ResortId)
            {
                return BadRequest();
            }

            _context.Entry(resort).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResortExists(id))
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

        // DELETE: api/Resorts/5
        // Deletes Resort by id
        // User must be role Administrator
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Resort>> DeleteResort(int id)
        {
            var resort = await _context.Resort.FindAsync(id);
            if (resort == null)
            {
                return NotFound();
            }

            _context.Resort.Remove(resort);
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

        private bool ResortExists(int id)
        {
            return _context.Resort.Any(e => e.ResortId == id);
        }
    }
}

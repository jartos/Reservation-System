using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CabinReservationAPI.Models;
using CabinReservationSystemAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace CabinReservationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CabinImagesController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public CabinImagesController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/CabinImages/All
        // Returns all CabinImages
        // User must be role Administrator
        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<CabinImage>>> GetCabinImage()
        {
            try
            {
                // Checks User-role in token
                if (User.IsInRole("Administrator") == false) return Unauthorized();

                List<CabinImage> cabinImages = await _context.CabinImage
                    .Include(cabin => cabin.Cabin)
                    .ToListAsync();
                if (cabinImages.Count() == 0) return NotFound();
                return Ok(cabinImages);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/CabinImages/Cabins/CabinId=5
        // Returns all CabinImages by CabinId
        [HttpGet("CabinId={id}")]
        public async Task<ActionResult<IEnumerable<CabinImage>>> GetCabinImagesByCabinId(int id)
        {
            try
            {
                var cabinImages = await _context.CabinImage
                    .Where(i => i.CabinId == id)
                    .ToListAsync();

                if (cabinImages == null) return NotFound();
                return cabinImages;
            }
            catch
            {
                return StatusCode(500);
            }
        }

        //// GET: api/CabinImages/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<CabinImage>> GetCabinImage(int id)
        //{
        //    var cabinImage = await _context.CabinImage.FindAsync(id);

        //    if (cabinImage == null)
        //    {
        //        return NotFound();
        //    }

        //    return cabinImage;
        //}

        //// PUT: api/CabinImages/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to, for
        //// more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutCabinImage(int id, CabinImage cabinImage)
        //{
        //    if (id != cabinImage.CabinImageId)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(cabinImage).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!CabinImageExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        // POST: api/CabinImages
        // Creates a new CabinImage with an unique id
        // User must be in Administrator or CabinOwner role
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult<CabinImage>> PostCabinImage(CabinImage cabinImage)
        {
            try
            {
                // If CabinOwner, check that he can create Cabin only by own PersonId
                if (User.IsInRole("CabinOwner"))
                {
                    var checkPerson = await _context.Person.Where(person => person.Email == User.Identity.Name).FirstOrDefaultAsync();

                    var cabin = await _context.Cabin.Where(cabin => cabin.CabinId == cabinImage.CabinId).FirstOrDefaultAsync();

                    if (cabin.PersonId != checkPerson.PersonId) return Unauthorized();
                }

                // Return BadRequest if cabin has already 4 images
                var cabinImages = await _context.CabinImage.Where(cImage => cImage.CabinId == cabinImage.CabinId).ToListAsync();
                if (cabinImages.Count() >= 4) return BadRequest();

                _context.CabinImage.Add(cabinImage);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // DELETE: api/CabinImages/CabinId=5
        // Delete CabinImage by CabinImageId
        // User must be in Administrator or CabinOwner can delete his own Cabin
        [HttpDelete("CabinId={id}")]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult<CabinImage>> DeleteCabinImage(int id)
        {
            try
            {
                var cabinImage = await _context.CabinImage.Where(cabinImage => cabinImage.CabinImageId == id).Include(cabinImage => cabinImage.Cabin).FirstOrDefaultAsync();

                // If CabinOwner, check that he can delete only his own Cabin
                if (User.IsInRole("CabinOwner"))
                {
                    var checkPerson = await _context.Person.Where(person => person.Email == User.Identity.Name).FirstOrDefaultAsync();
                    if (cabinImage.Cabin.PersonId != checkPerson.PersonId) return Unauthorized();
                }

                if (cabinImage == null) return NotFound();

                _context.CabinImage.Remove(cabinImage);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        private bool CabinImageExists(int id)
        {
            return _context.CabinImage.Any(e => e.CabinImageId == id);
        }
    }
}

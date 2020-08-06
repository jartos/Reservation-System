using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CabinReservationSystemAPI.Models;

namespace CabinReservationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public PostsController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/Posts/Postalcodes
        // Return string-List of PostalCodes
        [HttpGet("postalcodes")]
        public async Task<ActionResult<List<string>>> GetPostalCodes()
        {
            try
            {
                var posts = await _context.Post.Select(post => post.PostalCode).ToListAsync();

                if (null == posts) return NotFound();

                return posts;
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}



//// GET: api/Posts
//[HttpGet]
//public async Task<ActionResult<IEnumerable<Post>>> GetPost()
//{
//    return await _context.Post.ToListAsync();
//}

//// GET: api/Posts/5
//[HttpGet("{id}")]
//public async Task<ActionResult<Post>> GetPost(string id)
//{
//    var post = await _context.Post.FindAsync(id);

//    if (post == null)
//    {
//        return NotFound();
//    }

//    return post;
//}

//// PUT: api/Posts/5
//// To protect from overposting attacks, please enable the specific properties you want to bind to, for
//// more details see https://aka.ms/RazorPagesCRUD.
//[HttpPut("{id}")]
//public async Task<IActionResult> PutPost(string id, Post post)
//{
//    if (id != post.PostalCode)
//    {
//        return BadRequest();
//    }

//    _context.Entry(post).State = EntityState.Modified;

//    try
//    {
//        await _context.SaveChangesAsync();
//    }
//    catch (DbUpdateConcurrencyException)
//    {
//        if (!PostExists(id))
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

//// POST: api/Posts
//// To protect from overposting attacks, please enable the specific properties you want to bind to, for
//// more details see https://aka.ms/RazorPagesCRUD.
//[HttpPost]
//public async Task<ActionResult<Post>> PostPost(Post post)
//{
//    _context.Post.Add(post);
//    try
//    {
//        await _context.SaveChangesAsync();
//    }
//    catch (DbUpdateException)
//    {
//        if (PostExists(post.PostalCode))
//        {
//            return Conflict();
//        }
//        else
//        {
//            throw;
//        }
//    }

//    return CreatedAtAction("GetPost", new { id = post.PostalCode }, post);
//}

//// DELETE: api/Posts/5
//[HttpDelete("{id}")]
//public async Task<ActionResult<Post>> DeletePost(string id)
//{
//    var post = await _context.Post.FindAsync(id);
//    if (post == null)
//    {
//        return NotFound();
//    }

//    _context.Post.Remove(post);
//    await _context.SaveChangesAsync();

//    return post;
//}

//private bool PostExists(string id)
//{
//    return _context.Post.Any(e => e.PostalCode == id);
//}
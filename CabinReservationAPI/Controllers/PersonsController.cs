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
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PersonsController : ControllerBase
    {
        private readonly CabinReservationSystemContext _context;

        public PersonsController(CabinReservationSystemContext context)
        {
            _context = context;
        }

        // GET: api/Persons/5
        // Returns Person by PersonId
        // User must be role Administrator
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Person>> GetPerson(int id)
        {
            var person = await _context.Person.Where(person => person.PersonId == id).Include(person => person.Post).FirstOrDefaultAsync();

            if (person == null)
            {
                return NotFound();
            }

            return person;
        }

        // GET: api/Persons
        // User getting his own Person Information
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult<Person>> GetPerson()
        {
            try
            {
                var person = await _context.Person.Where(person => person.Email == User.Identity.Name)
                    .Include(person => person.Post)
                    .FirstOrDefaultAsync();
                if (person == null) return NotFound();
                return person;
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // PUT: api/Persons/5
        // Change Person information by PersonId and Person
        // User must be in Administrator or User can edit only his own information
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<IActionResult> PutPerson(int id, Person person)
        {
            try
            {
                // Checks that User-role is Administrator or User in IdentityDB matches Person in CabinReservationsDB
                if (false == User.IsInRole("Administrator") && person.Email != User.Identity.Name) return Unauthorized();

                if (id != person.PersonId) return BadRequest();

                _context.Entry(person).State = EntityState.Modified;
                
                await _context.SaveChangesAsync();

                return NoContent();
            }

            catch (DbUpdateConcurrencyException)
            {
                if (!PersonExists(id)) return NotFound();
                else throw;
            }
        }

        // POST: api/Persons
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<Person>> PostPerson(Person person)
        {
            try
            {
                _context.Person.Add(person);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetPerson", new { id = person.PersonId }, person);
            }
            catch
            {
                return StatusCode(400);
            }
        }

        // DELETE: api/Persons/5
        // Delete person by PersonId 
        // User must be role Administrator
        // TODO: must allow that Person can delete his own information?
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Person>> DeletePerson(int id)
        {
            try
            {
                var person = await _context.Person.FindAsync(id);
                if (person == null) return NotFound();

                _context.Person.Remove(person);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // |------------------------------OK------------------------------|
        // GET: api/Persons/FirstName/LastName
        // Returns Persons by FirstName, LastName
        // User must be role Administrator
        [HttpGet("{FirstName}/{LastName}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<Cabin>>> GetPersons(string FirstName, string LastName)
        {
            try
            {
                // If parameter is - set parameter to empty string
                if (FirstName == "-") FirstName = "";
                if (LastName == "-") LastName = "";

                var persons = await _context.Person
                    .Where(person => person.FirstName.ToUpper().Contains(FirstName.ToUpper()))
                    .Where(person => person.LastName.ToUpper().Contains(LastName.ToUpper()))
                    .Include(person => person.Post)
                    .ToListAsync();

                if (persons.Count() == 0) return NotFound();
                return Ok(persons);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: api/Persons/email=test@test.org
        // Returns Person by Email
        // User must be role Administrator
        [HttpGet("Email={Email}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Person>> GetPerson(string Email)
        {
            var person = await _context.Person.Where(person => person.Email == Email).Include(person => person.Post).FirstOrDefaultAsync();

            if (person == null)
            {
                return NotFound();
            }

            return person;
        }

        private bool PersonExists(int id)
        {
            return _context.Person.Any(e => e.PersonId == id);
        }
    }
}

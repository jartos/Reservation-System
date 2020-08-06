using CabinReservationWebApplication.Areas.Identity.Data;
using CabinReservationWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CabinReservationWebApplication.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class PersonsController : Controller
    {
        private readonly ServiceRepository _service;
        private readonly UserManager<CabinReservationWebApplicationUser> _userManager;

        public PersonsController(ServiceRepository service, UserManager<CabinReservationWebApplicationUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        // GET: Persons/Index
        // Returns view where Administrator can search Persons
        public ActionResult Index(int success)
        {
            // If request came from Create/Edit/Delete
            switch (success)
            {
                //case 1:
                //    ViewBag.Message = "Henkilö lisätty onnistuneesti!";
                //    break;
                case 2:
                    ViewBag.Message = "Henkilöä muokattu onnistuneesti!";
                    break;
                case 3:
                    ViewBag.Message = "Henkilö poistettu onnistuneesti!";
                    break;
                default:
                    ViewBag.Message = null;
                    break;
            }

            ViewBag.FirstEntry = true;
            return View();
        }

        //GET: Persons/Index
        //Returns view with List of Persons by search conditions
        [HttpPost]
        public async Task<IActionResult> Index(Person person)
        {
            try
            {
                ViewBag.Persons = await _service.GetPersons(User, person.FirstName, person.LastName);
                ViewBag.FirstEntry = false;

                return View();
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Persons/Details/5
        // Returns view with Person Details
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var person = await _service.GetPerson(User, id);
                return View(person);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Persons/Edit/5
        // Returns view where user can edit Person
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var person = await _service.GetPerson(User, id);

                // Getting PostalCodes to Dropdownlist
                ViewBag.PostalCodes = await _service.GetPostalCodesSelectListItems();
                if (null == ViewBag.PostalCodes)
                {
                    ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään postinumeroa";
                    return View("ErrorPage");
                }

                // Getting and Setting Role to Person 
                var user = await _userManager.FindByNameAsync(person.Email);
                var rolename = await _userManager.GetRolesAsync(user);
                // TODO: Maybe this if/else can be deleted when we create new Database?
                if (rolename.Count() == 0)
                {
                    person.Role = "Customer";
                }
                else person.Role = rolename[0];

                // Getting Roles to DropdownList
                List<SelectListItem> Roles = new List<SelectListItem>(){
                    (new SelectListItem { Value = "Customer", Text = "Customer" }),
                    (new SelectListItem { Value = "CabinOwner", Text = "CabinOwner" }),
                    (new SelectListItem { Value = "Administrator", Text = "Administrator" }) };
                ViewBag.Roles = Roles;

                return View(person);
            }
            catch
            {
                return View("ErrorPage");
            }

        }

        // POST: Persons/Edit/5
        // Edits Person, sets Role if Admin changes it
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, Person person)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool editPerson = await _service.PutPerson(User, id, person);

                    // Setting Userrole to Person
                    // TODO: Maybe must signout that User if Admin changes Role?
                    var user = await _userManager.FindByNameAsync(person.Email);
                    var oldRolename = await _userManager.GetRolesAsync(user);

                    var removeRole = await _userManager.RemoveFromRoleAsync(user, oldRolename[0]);
                    var setRole = await _userManager.AddToRoleAsync(user, person.Role);
                    if (editPerson && removeRole.Succeeded && setRole.Succeeded) return RedirectToAction("Index", new { success = 2 });

                }

                // Getting PostalCodes to Dropdownlist
                ViewBag.PostalCodes = await _service.GetPostalCodesSelectListItems();
                if (null == ViewBag.PostalCodes)
                {
                    ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään postinumeroa";
                    return View("ErrorPage");
                }

                // Getting'n'Setting Role to Person 
                var userAgain = await _userManager.FindByNameAsync(person.Email);
                var rolename = await _userManager.GetRolesAsync(userAgain);

                // TODO: Maybe this if/else can be deleted when we create new Database?
                if (rolename.Count() == 0) person.Role = "Customer";
                else person.Role = rolename[0];

                // Getting Roles to DropdownList
                List<SelectListItem> Roles = new List<SelectListItem>(){
                    (new SelectListItem { Value = "Customer", Text = "Customer" }),
                    (new SelectListItem { Value = "CabinOwner", Text = "CabinOwner" }),
                    (new SelectListItem { Value = "Administrator", Text = "Administrator" }) };
                ViewBag.Roles = Roles;

                return View(person);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Persons/Delete/5
        // Returns view where Administrator can delete Person
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var person = await _service.GetPerson(User, id);
                return View(person);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: Persons/Delete/5
        // Deletes Person
        // TODO: must add ErrorHandling if Person deleted success in CabinReservationDB but failed in IdentityDB, maybe post same Person back in CabinReservationDB??
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, Person person)
        {
            try
            {
                bool deletePerson = await _service.DeletePerson(User, id);

                if (deletePerson)
                {
                    // Deleting Person also IdentityDB
                    var user = await _userManager.FindByEmailAsync(person.Email);
                    var result = await _userManager.DeleteAsync(user);
                    if (result.Succeeded) return RedirectToAction("Index", new { success = 3 });
                }

                ViewBag.ErrorMessage = "Jos yritit poistaa henkilön jolla on majoitusvarauksia tai majoituspaikkoja, poista ensin ne";
                return View("ErrorPage");
            }
            catch
            {
                return View();
            }
        }
    }
}


//// GET: Administrator/CreatePerson
//// Returns view where user can create new Person
//public async Task<ActionResult> CreatePerson()
//{
//    // Getting PostalCodes to Dropdownlist
//    ViewBag.PostalCodes = await PostalCodesToDropdownMenu();
//    if (null == ViewBag.PostalCodes)
//    {
//        ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään postinumeroa";
//        return View("ErrorPage");
//    }

//    // Getting Roles to DropdownList
//    List<SelectListItem> Roles = new List<SelectListItem>(){
//            (new SelectListItem { Value = "Customer", Text = "Customer" }),
//            (new SelectListItem { Value = "CabinOwner", Text = "CabinOwner" }),
//            (new SelectListItem { Value = "Administrator", Text = "Administrator" }) };
//    ViewBag.Roles = Roles;

//    return View();
//}

//// POST: Administrator/CreatePerson
//// Creates new Person
//[HttpPost]
//[ValidateAntiForgeryToken]
//public async Task<ActionResult> CreatePerson(Person person)
//{
//    try
//    {
//        if (ModelState.IsValid)
//        {
//            // TODO: Post Person, set role, handle password somehow

//            //bool postCabin = await _service.PostCabin(User, cabin);
//            //if (postCabin) return RedirectToAction(nameof(Cabins));
//            //return View("ErrorPage");


//        }

//        // Getting PostalCodes to Dropdownlist
//        ViewBag.PostalCodes = await PostalCodesToDropdownMenu();
//        if (null == ViewBag.PostalCodes)
//        {
//            ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään postinumeroa";
//            return View("ErrorPage");
//        }

//        // Getting Roles to DropdownList
//        List<SelectListItem> Roles = new List<SelectListItem>(){
//            (new SelectListItem { Value = "Customer", Text = "Customer" }),
//            (new SelectListItem { Value = "CabinOwner", Text = "CabinOwner" }),
//            (new SelectListItem { Value = "Administrator", Text = "Administrator" }) };
//        ViewBag.Roles = Roles;

//        return View(person);
//    }
//    catch
//    {
//        return View("ErrorPage");
//    }
//}
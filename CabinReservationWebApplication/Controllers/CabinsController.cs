using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CabinReservationWebApplication.Areas.Identity.Data;
using CabinReservationWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CabinReservationWebApplication.Controllers
{
    public class CabinsController : Controller
    {
        private readonly ServiceRepository _service;
        private readonly UserManager<CabinReservationWebApplicationUser> _userManager;

        public CabinsController(ServiceRepository service, UserManager<CabinReservationWebApplicationUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        // GET: Cabins/Index
        // Returns view where Administrator can search Cabins or CabinOwner can see his own cabins
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Index(int success)
        {
            // If request came from Create/Edit/Delete
            switch (success)
            {
                case 1:
                    ViewBag.Message = "Majoituspaikka lisätty onnistuneesti!";
                    break;
                case 2:
                    ViewBag.Message = "Majoituspaikkaa muokattu onnistuneesti!";
                    break;
                case 3:
                    ViewBag.Message = "Majoituspaikka poistettu onnistuneesti!";
                    break;
                default:
                    ViewBag.Message = null;
                    break;
            }

            if (User.IsInRole("Administrator"))
            {
                ViewBag.FirstEntry = true;
                return View();
            }

            else
            {
                ViewBag.Cabins = await _service.GetCabins(User);

                return View();
            }
        }

        // POST: Cabins/Index
        // Returns view with List of Cabins by given search conditions
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Index(Cabin cabin)
        {
            // If request came from Resorts/Index, set these
            if (cabin.Person == null) cabin.Person = new Person();
            ViewBag.ResortName = cabin.Resort.ResortName;

            ViewBag.Cabins = await _service.GetCabins(User, cabin.Resort.ResortName, cabin.CabinName, cabin.Person.LastName);
            ViewBag.FirstEntry = false;

            return View();
        }


        // GET: Cabins/Details/5
        // Returns view with Cabin Details
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var cabin = await _service.GetCabin(User, id);
                return View(cabin);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Cabins/Edit/5
        // Returns view where Administrator/CabinOwner can edit Cabin
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                // Getting PostalCodes to Dropdownlist
                ViewBag.PostalCodes = await _service.GetPostalCodesSelectListItems();
                if (null == ViewBag.PostalCodes)
                {
                    ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään postinumeroa";
                    return View("ErrorPage");
                }

                var cabin = await _service.GetCabin(User, id);

                if (cabin == null) return View("ErrorPage");

                return View(cabin);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: Cabins/Edit/5
        // Edits Cabin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Edit(int id, Cabin cabin)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool editCabin = await _service.PutCabin(User, id, cabin);

                    if (editCabin) return RedirectToAction("Index", new { success = 2 });

                    return View("ErrorPage");
                }

                // Getting PostalCodes to Dropdownlist
                ViewBag.PostalCodes = await _service.GetPostalCodesSelectListItems();
                if (null == ViewBag.PostalCodes)
                {
                    ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään postinumeroa";
                    return View("ErrorPage");
                }
                return View(cabin);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Cabins/Delete/5
        // Returns view where Administrator/CabinOwner can delete Cabin
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var cabin = await _service.GetCabin(User, id);
                return View(cabin);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: Cabins/Delete/5
        // Deletes Cabin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Delete(int id, Cabin cabin)
        {
            try
            {
                bool deleteCabin = await _service.DeleteCabin(User, id);

                if (deleteCabin) return RedirectToAction("Index", new { success = 3 });

                ViewBag.ErrorMessage = "Jos yritit poistaa majoituspaikkaa jolla on majoitusvarauksia, poistaminen ei ole sallittua";
                return View("ErrorPage");
            }
            catch
            {
                return View();
            }
        }

        // GET: Cabins/Create
        // Returns view where Administrator/CabinOwner can create new Cabin
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Create()
        {
            // Getting PostalCodes to Dropdownlist
            ViewBag.PostalCodes = await _service.GetPostalCodesSelectListItems();
            if (null == ViewBag.PostalCodes)
            {
                ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään postinumeroa";
                return View("ErrorPage");
            }

            // Getting Resorts to Dropdownlist
            ViewBag.Resorts = await _service.GetResortsSelectListItems();
            if (null == ViewBag.Resorts)
            {
                ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään toimipistettä";
                return View("ErrorPage");
            }

            // If Administrator get CabinOwners
            if (User.IsInRole("Administrator"))
            {
                // Getting users in IDentityDB which are in role CabinOwner
                var cabinOwners = await _userManager.GetUsersInRoleAsync("CabinOwner");
                if (cabinOwners.Count() == 0)
                {
                    ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään henkilöä roolissa CabinOwner";
                    return View("ErrorPage");
                }
                // Getting Persons in CabinReservationDB by cabinOwners
                List<Person> persons = new List<Person>();
                foreach (var item in cabinOwners)
                {
                    var person = await _service.GetPerson(User, item.UserName);
                    if (null != person) persons.Add(person);
                }
                List<SelectListItem> Persons = new List<SelectListItem>();
                foreach (var item in persons)
                {
                    Persons.Add(new SelectListItem { Value = item.PersonId.ToString(), Text = item.FirstName + " " + item.LastName });
                }
                ViewBag.Persons = Persons;
            }

            // If CabinOwner get own Person information
            else
            {
                var person = await _service.GetPerson(User);
                ViewBag.PersonId = person.PersonId;
            }

            return View();
        }

        // POST: Cabins/Create
        // Creates new Cabin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Create(Cabin cabin)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool postCabin = await _service.PostCabin(User, cabin);

                    if (postCabin) return RedirectToAction("Index", new { success = 1 });

                    return View("ErrorPage");
                }

                // If Administrator get CabinOwners
                if (User.IsInRole("Administrator"))
                {
                    // Getting users in IDentityDB which are in role CabinOwner
                    var cabinOwners = await _userManager.GetUsersInRoleAsync("CabinOwner");
                    if (cabinOwners.Count() == 0)
                    {
                        ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään henkilöä roolissa CabinOwner";
                        return View("ErrorPage");
                    }
                    // Getting Persons in CabinReservationDB by cabinOwners
                    List<Person> persons = new List<Person>();
                    foreach (var item in cabinOwners)
                    {
                        var person = await _service.GetPerson(User, item.UserName);
                        if (null != person) persons.Add(person);
                    }
                    List<SelectListItem> Persons = new List<SelectListItem>();
                    foreach (var item in persons)
                    {
                        Persons.Add(new SelectListItem { Value = item.PersonId.ToString(), Text = item.FirstName + " " + item.LastName });
                    }
                    ViewBag.Persons = Persons;
                }

                // If CabinOwner get own Person information
                else
                {
                    var person = await _service.GetPerson(User);
                    ViewBag.PersonId = person.PersonId;
                }

                return View(cabin);
            }
            catch
            {
                return View("ErrorPage");
            }
        }
    }
}
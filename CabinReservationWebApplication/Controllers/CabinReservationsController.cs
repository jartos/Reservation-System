using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CabinReservationWebApplication.Areas.Identity.Data;
using CabinReservationWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CabinReservationWebApplication.Controllers
{
    public class CabinReservationsController : Controller
    {
        private readonly ServiceRepository _service;
        private readonly UserManager<CabinReservationWebApplicationUser> _userManager;

        public CabinReservationsController(ServiceRepository service, UserManager<CabinReservationWebApplicationUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        // GET: CabinReservations/Index
        // Returns view where Administrator can search CabinReservations or CabinOwner can search own Cabin CabinReservations
        [Authorize(Roles = "Administrator, CabinOwner")]
        public ActionResult Index(int success)
        {
            // If request came from Create/Edit/Delete
            switch (success)
            {
                //case 1:
                //    ViewBag.Message = "Lisäpalvelu lisätty onnistuneesti!";
                //    break;
                //case 2:
                //    ViewBag.Message = "Lisäpalvelua muokattu onnistuneesti!";
                //    break;
                case 3:
                    ViewBag.Message = "Majoitusvaraus ja sen lisäpalveluvaraukset poistettu onnistuneesti!";
                    break;
                default:
                    ViewBag.Message = null;
                    break;
            }

            ViewBag.FirstEntry = true;
            return View();
        }

        // POST: CabinReservations/Index
        // Returns List of CabinReservations by search conditions if CabinId = 0 
        // If CabinId is not 0 returns Cabin all CabinReservations with no search conditions
        [HttpPost]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<IActionResult> Index(CabinReservation cabinReservation)
        {
            if (cabinReservation.CabinId == 0)
            {
                ViewBag.CabinReservations = await _service.GetCabinReservations(User, cabinReservation);
            }
            else
            {
                ViewBag.CabinReservations = await _service.GetCabinReservations(User, cabinReservation.CabinId);
            }

            ViewBag.FirstEntry = false;

            if (cabinReservation.ReservationStartDate != DateTime.MinValue) ViewBag.Starting = cabinReservation.ReservationStartDate.ToString("dd'.'MM'.'yyyy");
            if (cabinReservation.ReservationEndDate != DateTime.MinValue) ViewBag.Ending = cabinReservation.ReservationEndDate.ToString("dd'.'MM'.'yyyy");

            return View();
        }

        // GET: CabinReservations/Create/5
        // Returns view where Customer/CabinOwner can create CabinReservation by own PersonId or Administrator can create CabinReservation all PersonId:s
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult> Create(int id)
        {
            ViewBag.Cabin = await _service.GetCabin(id);

            if (User.IsInRole("Administrator"))
            {
                // Getting persons in dropdownmenu
                var persons = await _service.GetPersons(User, "-", "-");
                List<SelectListItem> Persons = new List<SelectListItem>();
                foreach (var item in persons)
                {
                    Persons.Add(new SelectListItem { Value = item.PersonId.ToString(), Text = item.FirstName + " " + item.LastName });
                }
                ViewBag.Persons = Persons;
            }

            // Creating empty Model to view
            CabinReservation cabinReservation = new CabinReservation();

            // Adding 9 empty ActivityReservations in CabinReservation
            List<ActivityReservation> activityReservations = new List<ActivityReservation>();
            ActivityReservation activityReservation = new ActivityReservation();
            for (int i = 0; i < 9; i++)
            {
                activityReservations.Add(activityReservation);
            }
            cabinReservation.ActivityReservations = activityReservations;

            return View(cabinReservation);
        }

        // POST: CabinReservation/Create
        // Redirects SelectDates if User has selected Activities
        // Redirects Confirm if User has not selected Activities
        [HttpPost]
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult> Create(CabinReservation cabinReservation)
        {
            //cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.AddHours(3);
            //cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.AddHours(3);

            // Removing empty ActivityReservations in CabinReservation 
            foreach (var item in cabinReservation.ActivityReservations.ToList())
            {
                if (item.ActivityId == 0)
                    cabinReservation.ActivityReservations.Remove(item);

                //else {
                //    item.ActivityReservationTime = item.ActivityReservationTime.AddHours(3);
                //} 
            }

            // If user has not select ActivityReservations show Confirm View
            if (cabinReservation.ActivityReservations.Count() == 0)
            {
                return await Confirm(cabinReservation);
            }
            return await SelectDates(cabinReservation);
        }

        // GET: CabinReservations/SelectDates
        // Returns view where User must select ActivityReservations dates
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult> SelectDates(CabinReservation cabinReservation)
        {
            cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.AddHours(3);
            cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.AddHours(3);

            if (User.IsInRole("Administrator")) cabinReservation.Person = await _service.GetPerson(User, cabinReservation.PersonId);

            else cabinReservation.Person = await _service.GetPerson(User);

            // Getting Activities in ActivityReservations
            foreach (var item in cabinReservation.ActivityReservations)
            {
                item.Activity = await _service.GetActivity(item.ActivityId);
            }

            ViewBag.CabinReservation = cabinReservation;
            return View("SelectDates");
        }

        // GET: CabinReservations/Confirm
        // Returns view which shows CabinReservation summary and User can Post CabinReservation
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult> Confirm(CabinReservation cabinReservation)
        {
            // This because timezones when publishing app, not right way/may not work in all timezones
            cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.AddHours(3);
            cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.AddHours(3);

            if (User.IsInRole("Administrator")) cabinReservation.Person = await _service.GetPerson(User, cabinReservation.PersonId);

            else cabinReservation.Person = await _service.GetPerson(User);

            ViewBag.CabinReservation = cabinReservation;

            return View("Confirm");
        }

        // GET: CabinReservations/Success
        // Posts/Puts CabinReservation
        // TODO: this must be moved somewhere else, where all ActionResults can use this same Success View
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult> Success(CabinReservation cabinReservation)
        {
            //cabinReservation.ReservationStartDate = cabinReservation.ReservationStartDate.AddHours(3);
            //cabinReservation.ReservationEndDate = cabinReservation.ReservationEndDate.AddHours(3);

            // If CabinReservationId = 0, post new CabinReservation
            if (cabinReservation.CabinReservationId == 0)
            {
                bool postReservation = await _service.PostCabinReservation(User, cabinReservation);
                if (postReservation) return View("Success");
            }

            // Else edit old CabinReservation
            else
            {
                bool putReservation = await _service.PutCabinReservation(User, cabinReservation);
                if (putReservation) return View("Success");
            }

            return View("ErrorPage");
        }

        // GET: CabinReservations/Details/5
        // Returns view with CabinReservation Details
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var cabinReservation = await _service.GetCabinReservation(User, id);
                return View(cabinReservation);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: CabinReservations/Edit/5
        // Returns view where Administrator can edit CabinReservation or CabinOwner can edit his own CabinReservation and own Cabin CabinReservation or User can edit his own CabinReservation
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var cabinReservation = await _service.GetCabinReservation(User, id);

                // Dont allow edit if ReservationStartDate is earlier than tomorrow
                if (cabinReservation.ReservationStartDate < DateTime.Now.AddDays(1))
                {
                    ViewBag.ErrorMessage = "Muokkaus ei sallittua, varauksen alkamispäivämäärä on huomenna tai aiemmin";
                    return View("ErrorPage");
                }

                // Adding empty ActivityReservations in CabinReservation until there is 9 total
                ActivityReservation activityReservation = new ActivityReservation();
                int activityReservationsToAdd = 9 - cabinReservation.ActivityReservations.Count();
                for (int i = 0; i < activityReservationsToAdd; i++)
                {
                    cabinReservation.ActivityReservations.Add(activityReservation);
                }

                ViewBag.Cabin = await _service.GetCabin(cabinReservation.CabinId);

                if (cabinReservation == null) return View("ErrorPage");

                return View(cabinReservation);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: CabinReservations/Edit/5
        // Redirects SelectDates or Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, CabinOwner, Customer")]
        public async Task<ActionResult> Edit(int id, CabinReservation cabinReservation)
        {
            try
            {
                // Removing empty ActivityReservations in CabinReservation 
                foreach (var item in cabinReservation.ActivityReservations.ToList())
                {
                    if (item.ActivityId == 0)
                        cabinReservation.ActivityReservations.Remove(item);
                }

                // If user has not select ActivityReservations show Confirm View
                if (cabinReservation.ActivityReservations.Count() == 0)
                {
                    return await Confirm(cabinReservation);
                }
                return await SelectDates(cabinReservation);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: CabinReservations/Delete/5
        // Returns view where Administrator can delete CabinReservation or CabinOwner can delete his own Cabin CabinReservation, but CabinOwner cannot delete CabinReservation if ReservationStartDate is earlier than tomorrow
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var cabinReservation = await _service.GetCabinReservation(User, id);
                return View(cabinReservation);
            }
            catch
            {
                return View("ErrorPage");
            }

        }

        // POST: CabinReservations/Delete/5
        // Deletes CabinReservation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, CabinOwner")]
        public async Task<ActionResult> Delete(int id, CabinReservation cabinReservation)
        {
            try
            {
                bool deleteCabinReservation = await _service.DeleteCabinReservation(User, id);

                if (deleteCabinReservation) return RedirectToAction("Index", new { success = 3 });

                ViewBag.ErrorMessage = "Jos yritit poistaa majoitusvarausta jonka alkamispäivämäärä on pienempi kuin huomenna, se ei ole sallittua";
                return View("ErrorPage");
            }
            catch
            {
                return View("ErrorPage");
            }
        }
    }
}
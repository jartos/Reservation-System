using System.Collections.Generic;
using System.Threading.Tasks;
using CabinReservationWebApplication.Areas.Identity.Data;
using CabinReservationWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CabinReservationWebApplication.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ResortsController : Controller
    {
        private readonly ServiceRepository _service;
        private readonly UserManager<CabinReservationWebApplicationUser> _userManager;

        public ResortsController(ServiceRepository service, UserManager<CabinReservationWebApplicationUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        // GET: Resorts/Index
        // Returns view with list of all Resorts
        public async Task<ActionResult> Index(int success)
        {
            // If request came from Create/Edit/Delete
            switch (success)
            {
                case 1:
                    ViewBag.Message = "Toimipiste lisätty onnistuneesti!";
                    break;
                case 2:
                    ViewBag.Message = "Toimipistettä muokattu onnistuneesti!";
                    break;
                case 3:
                    ViewBag.Message = "Toimipiste poistettu onnistuneesti!";
                    break;
                default:
                    ViewBag.Message = null;
                    break;
            }

            IEnumerable<Resort> resorts = await _service.GetResorts();
            return View(resorts);
        }

        // GET: Resorts/Create
        // Returns view where Administrator can create new Resort
        public ActionResult Create()
        {
            return View();
        }

        // POST: Resorts/Create
        // Creates new Resort 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Resort resort)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool postResort = await _service.PostResort(User, resort);

                    if (postResort) return RedirectToAction("Index", new { success = 1 });

                    ViewBag.ErrorMessage = "Jos yritit luoda toimipisteen nimellä joka on jo olemassa, se ei ole sallittua";
                    return View("ErrorPage");
                }
                return View(resort);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Resorts/Details/5
        // Returns view with Resort Details
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var resort = await _service.GetResort(User, id);
                return View(resort);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Resorts/Edit/5
        // Returns view where Administrator can edit Resort
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var resort = await _service.GetResort(User, id);
                return View(resort);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: Resorts/Edit/5
        // Edits Resort
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, Resort resort)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool editResort = await _service.PutResort(User, id, resort);

                    if (editResort) return RedirectToAction("Index", new { success = 2 });

                    ViewBag.ErrorMessage = "Jos yritit vaihtaa toimipisteen nimeksi sellaista joka on jo olemassa, se ei ole sallittua";
                    return View("ErrorPage");
                }
                return View(resort);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: Resorts/Delete/5
        // Returns view where Administrator can delete Resort
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var resort = await _service.GetResort(User, id);
                return View(resort);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // POST: Resorts/Delete/5
        // Deletes Resort
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, Resort resort)
        {
            try
            {
                bool deleteResort = await _service.DeleteResort(User, id);

                if (deleteResort) return RedirectToAction("Index", new { success = 3 });

                ViewBag.ErrorMessage = "Jos yritit poistaa toimipistettä jolla on majoituksia tai lisäpalveluita, poistaminen ei ole sallittua";
                return View("ErrorPage");
            }
            catch
            {
                return View("ErrorPage");
            }
        }
    }
}
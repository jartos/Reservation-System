using System.Threading.Tasks;
using CabinReservationWebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CabinReservationWebApplication.Controllers
{

    [Authorize]
    public class UserController : Controller
    {
        private readonly ServiceRepository _service;
        public UserController(ServiceRepository service)
        {
            _service = service;
        }

        // GET: User/CabinReservations
        // Returns List of User own CabinReservations
        public async Task<ActionResult> CabinReservations(int success)
        {
            try
            {
                // If request came from Delete
                switch (success)
                {
                    case 3:
                        ViewBag.Message = "Majoitusvaraus poistettu onnistuneesti!";
                        break;
                    default:
                        ViewBag.Message = null;
                        break;
                }

                var cabinReservations = await _service.GetCabinReservations(User);

                return View(cabinReservations);
            }
            catch
            {
                return View("ErrorPage");
            }
        }

        // GET: User/CabinReservationDetails/5
        // Returns view with CabinReservation Details
        public async Task<ActionResult> CabinReservationDetails(int id)
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

        // GET: User/DeleteCabinReservation/5
        // Returns view where User can delete CabinReservation, CabinOwner can delete his own Cabin CabinReservation or User can delete his own CabinReservation, 
        // but CabinOwner/Customer cannot delete CabinReservation if ReservationStartDate is earlier than tomorrow
        public async Task<ActionResult> DeleteCabinReservation(int id)
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

        // POST: CabinReservations/DeleteCabinReservation/5
        // Deletes CabinReservation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCabinReservation(int id, CabinReservation cabinReservation)
        {
            try
            {
                bool deleteCabinReservation = await _service.DeleteCabinReservation(User, id);

                if (deleteCabinReservation) return RedirectToAction("CabinReservations", new { success = 3 });

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
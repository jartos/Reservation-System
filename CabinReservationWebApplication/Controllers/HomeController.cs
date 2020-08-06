using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CabinReservationWebApplication.Models;
using X.PagedList;
using Microsoft.AspNetCore.Authorization;

namespace CabinReservationWebApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ServiceRepository _service;

        public HomeController(ServiceRepository service, ILogger<HomeController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: Home/Index
        // Returns view where user can search Cabins
        public IActionResult Index()
        {
            ViewBag.FirstEntry = true;
            return View();
        }

        // Post: Home/Index
        // Returns view with List of Cabins by given search conditions
        [HttpPost]
        public async Task<ActionResult> Index(CabinSearch cabinSearch)
        {
            ViewBag.PageArrival = cabinSearch.Arrival;
            ViewBag.PageDeparture = cabinSearch.Departure;
            ViewBag.PageRooms = cabinSearch.Rooms;
            ViewBag.PageSearchWord = cabinSearch.SearchWord;
            ViewBag.PageSort = cabinSearch.Sort;

            if (cabinSearch.Rooms == null) cabinSearch.Rooms = "1";
            else cabinSearch.Rooms = cabinSearch.Rooms.Remove(cabinSearch.Rooms.Length - 9);
            if (cabinSearch.Rooms == ">10") cabinSearch.Rooms = "11";

            var cabins = await _service.GetCabins(cabinSearch.SearchWord, cabinSearch.Arrival, cabinSearch.Departure, cabinSearch.Rooms);
            ViewBag.FirstEntry = false;

            if (cabinSearch.Arrival != DateTime.MinValue) ViewBag.Arrival = cabinSearch.Arrival.ToString("dd'.'MM'.'yyyy");
            if (cabinSearch.Departure != DateTime.MinValue) ViewBag.Departure = cabinSearch.Departure.ToString("dd'.'MM'.'yyyy");

            switch (cabinSearch.Sort)
            {
                case "Hinta - Halvimmat ensin":
                    cabins = cabins.OrderBy(cabin => cabin.CabinPricePerDay);
                    break;
                case "Hinta - Kalleimmat ensin":
                    cabins = cabins.OrderByDescending(cabin => cabin.CabinPricePerDay);
                    break;
                case "Pinta-ala - Suurimmat ensin":
                    cabins = cabins.OrderByDescending(cabin => cabin.Area);
                    break;
                case "Pinta-ala - Pienimmät ensin":
                    cabins = cabins.OrderBy(cabin => cabin.Area);
                    break;
                case "Makuuhuoneet - Max.":
                    cabins = cabins.OrderByDescending(cabin => cabin.Rooms);
                    break;
                case "Makuuhuoneet - Min.":
                    cabins = cabins.OrderBy(cabin => cabin.Rooms);
                    break;
                case "Nimi - Laskeva aakkosjärjestys":
                    cabins = cabins.OrderBy(cabin => cabin.CabinName);
                    break;
                case "Nimi - Nouseva aakkosjärjestys":
                    cabins = cabins.OrderByDescending(cabin => cabin.CabinName);
                    break;
            }

            int pageSize = 5;
            int pageNumber = 1;

            if (cabins != null) 
            {
                ViewBag.Cabins = cabins.ToPagedList(pageNumber, pageSize);
            }

            return View();
        }

        public async Task<ActionResult> IndexPagedList(DateTime PageArrival, DateTime PageDeparture, string PageRooms, string searchWord, string PageSort, int? page)
        {
            ViewBag.PageArrival = PageArrival;
            ViewBag.PageDeparture = PageDeparture;
            ViewBag.PageRooms = PageRooms;
            ViewBag.PageSearchWord = searchWord;
            ViewBag.PageSort = PageSort;

            CabinSearch cabinSearch = new CabinSearch
            {
                Arrival = DateTime.Now,
                Departure = DateTime.Now,
                Rooms = "",
                SearchWord = "",
                Sort = ""
            };

            cabinSearch.Arrival = PageArrival;
            cabinSearch.Departure = PageDeparture;
            cabinSearch.Rooms = PageRooms;
            cabinSearch.SearchWord = searchWord;
            cabinSearch.Sort = PageSort;

            if (cabinSearch.Rooms == null) cabinSearch.Rooms = "1";
            else cabinSearch.Rooms = cabinSearch.Rooms.Remove(cabinSearch.Rooms.Length - 9);
            if (cabinSearch.Rooms == ">10") cabinSearch.Rooms = "11";

            var cabins = await _service.GetCabins(cabinSearch.SearchWord, cabinSearch.Arrival, cabinSearch.Departure, cabinSearch.Rooms);
            ViewBag.FirstEntry = false;

            if (cabinSearch.Arrival != DateTime.MinValue) ViewBag.Arrival = cabinSearch.Arrival.ToString("dd'.'MM'.'yyyy");
            if (cabinSearch.Departure != DateTime.MinValue) ViewBag.Departure = cabinSearch.Departure.ToString("dd'.'MM'.'yyyy");

            switch (cabinSearch.Sort)
            {
                case "Hinta - Halvimmat ensin":
                    cabins = cabins.OrderBy(cabin => cabin.CabinPricePerDay);
                    break;
                case "Hinta - Kalleimmat ensin":
                    cabins = cabins.OrderByDescending(cabin => cabin.CabinPricePerDay);
                    break;
                case "Pinta-ala - Suurimmat ensin":
                    cabins = cabins.OrderByDescending(cabin => cabin.Area);
                    break;
                case "Pinta-ala - Pienimmät ensin":
                    cabins = cabins.OrderBy(cabin => cabin.Area);
                    break;
                case "Makuuhuoneet - Max.":
                    cabins = cabins.OrderByDescending(cabin => cabin.Rooms);
                    break;
                case "Makuuhuoneet - Min.":
                    cabins = cabins.OrderBy(cabin => cabin.Rooms);
                    break;
                case "Nimi - Laskeva aakkosjärjestys":
                    cabins = cabins.OrderBy(cabin => cabin.CabinName);
                    break;
                case "Nimi - Nouseva aakkosjärjestys":
                    cabins = cabins.OrderByDescending(cabin => cabin.CabinName);
                    break;
            }

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            if (cabins != null)
            {
                ViewBag.Cabins = cabins.ToPagedList(pageNumber, pageSize);
            }

            return View();
        }

        // GET: Home/Details/5
        // Return view with selected Cabin details
        public async Task<ActionResult> Details(int id)
        {
            var cabin = await _service.GetCabin(id);
            return View(cabin);
        }

        // GET: Home/BecomeCabinOwner
        // Returns View where User can send Request to become CabinOwner
        [Authorize]
        public ActionResult BecomeCabinOwner()
        {
            if (User.IsInRole("CabinOwner") || User.IsInRole("Administrator")) return RedirectToAction("Create", "Cabins");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    //return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}

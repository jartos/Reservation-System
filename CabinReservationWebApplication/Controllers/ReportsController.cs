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
    [Authorize(Roles = "Administrator, CabinOwner")]
    public class ReportsController : Controller
    {
        private readonly ServiceRepository _service;
        private readonly UserManager<CabinReservationWebApplicationUser> _userManager;

        public ReportsController(ServiceRepository service, UserManager<CabinReservationWebApplicationUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        // GET: Reports/Index
        // Returns view where Administrator can search Reports or CabinOwner can search his own Cabins Reports
        public async Task<ActionResult> Index()
        {
            if (User.IsInRole("Administrator"))
            {
                ViewBag.ResortsNotSelected = false;

                // Getting Resorts to CheckBoxList
                ViewBag.ResortsToCheckListBox = await _service.GetResortsSelectListItems();
                if (null == ViewBag.ResortsToCheckListBox)
                {
                    ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään toimipistettä";
                    return View("ErrorPage");
                }
            }
            return View();
        }

        // GET: Reports/Index
        // Returns view with List of Reports by search conditions
        // TODO: This can be done simplification
        [HttpPost]
        public async Task<ActionResult> Index(Report report)
        {
            if (User.IsInRole("Administrator"))
            {
                // Getting Resorts to CheckBoxList
                ViewBag.ResortsToCheckListBox = await _service.GetResortsSelectListItems();
                if (null == ViewBag.ResortsToCheckListBox)
                {
                    ViewBag.ErrorMessage = "Tietokannassa ei ole yhtään toimipistettä";
                    return View("ErrorPage");
                }

                // Getting selected ResortID:s
                List<int> resortIDs = new List<int>();
                foreach (SelectListItem item in report.Resorts)
                {
                    if (item.Selected) resortIDs.Add(int.Parse(item.Value));
                }

                if (resortIDs.Count() == 0 || report.Start > report.End || report.Start == DateTime.MinValue || report.End == DateTime.MinValue)
                {
                    if (resortIDs.Count() == 0) ViewBag.ResortsNotSelected = true;
                    else ViewBag.ResortsNotSelected = false;

                    if (report.Start > report.End) ViewBag.DatetimeError = "Alkaen päivämäärä täytyy olla pienempi kuin päättyen päivämäärä";

                    if (report.Start == DateTime.MinValue || report.End == DateTime.MinValue) ViewBag.DatetimeError = "Alkaen ja päättyen päivämäärät täytyy olla valittuna";

                    if (report.Start != DateTime.MinValue) ViewBag.Starting = report.Start.ToString("dd'.'MM'.'yyyy");
                    if (report.End != DateTime.MinValue) ViewBag.Ending = report.End.ToString("dd'.'MM'.'yyyy");

                    return View();
                }
                ViewBag.ResortsNotSelected = false;
                ViewBag.DatetimeError = "";
                ViewBag.Starting = report.Start.ToString("dd'.'MM'.'yyyy");
                ViewBag.Ending = report.End.ToString("dd'.'MM'.'yyyy");

                // CabinReservations by given conditions
                var cabinReservations = await _service.GetCabinReservations(User, report.Start, report.End, resortIDs);

                // Starting- and EndingDays difference
                int daysDifference = (report.End - report.Start).Days;

                List<Resort> resorts = new List<Resort>();
                for (int i = 0; i < resortIDs.Count(); i++)
                {
                    Resort resort = new Resort();

                    resort.ResortId = resortIDs[i];
                    // Getting and setting Cabins and Activitys in Resort, because JsonIgnore-attribute
                    resort.Cabins = (List<Cabin>)await _service.GetCabins(resortIDs[i]);
                    resort.Activities = (List<Activity>)await _service.GetActivities(resortIDs[i]);

                    Resort getResortName = await _service.GetResort(User, resortIDs[i]);
                    resort.ResortName = getResortName.ResortName;

                    // Calculating and setting Cabin ReservationPercentange
                    if (resort.Cabins != null)
                    {
                        foreach (var cabin in resort.Cabins)
                        {
                            try
                            {
                                var singleCabinCabinReservations = cabinReservations.Where(cabinReservation => cabinReservation.CabinId == cabin.CabinId);

                                int total = 0;

                                foreach (var cabinReservation in singleCabinCabinReservations)
                                {
                                    // Setting Reservation Start- and EndDate to Starting- and EndingDates if they are smaller/bigger
                                    if (cabinReservation.ReservationStartDate < report.Start) cabinReservation.ReservationStartDate = report.Start;
                                    if (cabinReservation.ReservationEndDate > report.End) cabinReservation.ReservationEndDate = report.End;

                                    // Starting- and EndingDays difference
                                    int reservationDaysDifference = (cabinReservation.ReservationEndDate - cabinReservation.ReservationStartDate).Days;

                                    total += reservationDaysDifference;
                                }
                                cabin.ReservationPercentange = (total / (decimal)daysDifference) * 100;
                            }
                            catch
                            {
                                cabin.ReservationPercentange = 0;
                            }

                        }
                        // Calculating and setting Resort ReservationPercentange
                        resort.CabinsReservationsPercentange = (resort.Cabins.Sum(cabin => cabin.ReservationPercentange)) / resort.Cabins.Count();
                    }

                    // Calculating and setting Activity ReservationCount
                    if (resort.Activities != null)
                    {
                        foreach (var activity in resort.Activities)
                        {
                            int total = 0;
                            if (cabinReservations != null)
                            {
                                foreach (var cabinReservation in cabinReservations)
                                {
                                    var singleActivityActivityReservations = cabinReservation.ActivityReservations.Where(activityReservation => activityReservation.ActivityId == activity.ActivityId);

                                    foreach (var activityReservation in singleActivityActivityReservations)
                                    {
                                        // If Activitys ActivityReservation time is smaller/bigger than Starting/Ending, dont count it to ReservationCount
                                        if (activityReservation.ActivityReservationTime < report.Start || activityReservation.ActivityReservationTime > report.End) { }
                                        else total += 1;
                                    }
                                }
                            }
                            activity.ReservationCount = total;
                        }
                        // Calculating and setting Resort ReservationCount
                        resort.ActivitiesReservationsCount = resort.Activities.Sum(activity => activity.ReservationCount);
                    }

                    resorts.Add(resort);
                }

                ViewBag.Resorts = resorts;

                // https://www.chartjs.org/
                //labels and datapoints for resortCabins-chart
                List<string> cabinsChartLabels = new List<string>();
                List<decimal> cabinsChartDataPoints = new List<decimal>();
                foreach (var item in resorts)
                {
                    cabinsChartLabels.Add(item.ResortName);
                    cabinsChartDataPoints.Add(decimal.Round(item.CabinsReservationsPercentange, 2));
                }
                ViewBag.cabinsChartLabels = Newtonsoft.Json.JsonConvert.SerializeObject(cabinsChartLabels);
                ViewBag.cabinsChartDataPoints = Newtonsoft.Json.JsonConvert.SerializeObject(cabinsChartDataPoints);

                //labels and datapoints for resortActivities-chart
                List<string> activitiesChartLabels = new List<string>();
                List<int> activitiesChartDataPoints = new List<int>();
                int chartMax = 0;
                foreach (var item in resorts)
                {
                    activitiesChartLabels.Add(item.ResortName);
                    activitiesChartDataPoints.Add(item.ActivitiesReservationsCount);
                    if (item.ActivitiesReservationsCount > chartMax) chartMax = item.ActivitiesReservationsCount;
                }
                ViewBag.activitiesChartLabels = Newtonsoft.Json.JsonConvert.SerializeObject(activitiesChartLabels);
                ViewBag.activitiesChartDataPoints = Newtonsoft.Json.JsonConvert.SerializeObject(activitiesChartDataPoints);

                // Removing last digit, because wanna set chartMax equal ten
                if (chartMax > 0) chartMax = (chartMax - (chartMax % 10));

                ViewBag.chartMax = chartMax + 10;
                ViewBag.chartStepSize = ((decimal)chartMax + 10) / 5;

                return View();
            }



            // If CabinOwner
            else
            {
                if (report.Start > report.End || report.Start == DateTime.MinValue || report.End == DateTime.MinValue)
                {
                    if (report.Start > report.End) ViewBag.DatetimeError = "Alkaen päivämäärä täytyy olla pienempi kuin päättyen päivämäärä";

                    if (report.Start == DateTime.MinValue || report.End == DateTime.MinValue) ViewBag.DatetimeError = "Alkaen ja päättyen päivämäärät täytyy olla valittuna";

                    if (report.Start != DateTime.MinValue) ViewBag.Starting = report.Start.ToString("dd'.'MM'.'yyyy");
                    if (report.End != DateTime.MinValue) ViewBag.Ending = report.End.ToString("dd'.'MM'.'yyyy");

                    return View();
                }

                ViewBag.ResortsNotSelected = false;
                ViewBag.DatetimeError = "";
                ViewBag.Starting = report.Start.ToString("dd'.'MM'.'yyyy");
                ViewBag.Ending = report.End.ToString("dd'.'MM'.'yyyy");

                var cabinReservation = new CabinReservation();
                cabinReservation.ReservationStartDate = report.Start;
                cabinReservation.ReservationEndDate = report.End;
                var cabin = new Cabin();
                cabin.CabinName = "-";
                var resort = new Resort();
                resort.ResortName = "-";
                cabin.Resort = resort;
                var person = new Person();
                person.LastName = "-";
                cabinReservation.Person = person;
                cabinReservation.Cabin = cabin;

                // CabinReservations by given conditions
                var cabinReservations = await _service.GetCabinReservations(User, cabinReservation);

                // Starting- and EndingDays difference
                int daysDifference = (report.End - report.Start).Days;

                IEnumerable<Cabin> cabins = await _service.GetCabins(User);

                // Calculating and setting Cabin ReservationPercentange
                if (cabins != null)
                {
                    foreach (var singleCabin in cabins)
                    {
                        try
                        {
                            var singleCabinCabinReservations = cabinReservations.Where(cabinReservation => cabinReservation.CabinId == singleCabin.CabinId);

                            int total = 0;

                            foreach (var singleCabinReservation in singleCabinCabinReservations)
                            {
                                // Setting Reservation Start- and EndDate to Starting- and EndingDates if they are smaller/bigger
                                if (singleCabinReservation.ReservationStartDate < report.Start) singleCabinReservation.ReservationStartDate = report.Start;
                                if (singleCabinReservation.ReservationEndDate > report.End) singleCabinReservation.ReservationEndDate = report.End;

                                // Starting- and EndingDays difference
                                int reservationDaysDifference = (singleCabinReservation.ReservationEndDate - singleCabinReservation.ReservationStartDate).Days;

                                total += reservationDaysDifference;
                            }
                            singleCabin.ReservationPercentange = (total / (decimal)daysDifference) * 100;
                        }
                        catch
                        {
                            singleCabin.ReservationPercentange = 0;
                        }

                    }
                }

                ViewBag.Cabins = cabins;

                // https://www.chartjs.org/
                //labels and datapoints for resortCabins-chart
                List<string> cabinsChartLabels = new List<string>();
                List<decimal> cabinsChartDataPoints = new List<decimal>();
                foreach (var item in cabins)
                {
                    cabinsChartLabels.Add(item.CabinName);
                    cabinsChartDataPoints.Add(decimal.Round(item.ReservationPercentange, 2));
                }
                ViewBag.cabinsChartLabels = Newtonsoft.Json.JsonConvert.SerializeObject(cabinsChartLabels);
                ViewBag.cabinsChartDataPoints = Newtonsoft.Json.JsonConvert.SerializeObject(cabinsChartDataPoints);

                ViewBag.activitiesChartLabels = "[\"\"]";
                ViewBag.activitiesChartDataPoints = "[\"\"]";

                ViewBag.chartMax = 0;
                ViewBag.chartStepSize = 0;

                return View();
            }
        }
    }
}
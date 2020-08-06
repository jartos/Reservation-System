using CabinReservationWebApplication.Models;
using System;
using System.Collections.Generic;

namespace CabinReservationWebApplication.Data
{
    public class ApiUrls
    {
        //--------------------------------------------------------------------------------- Resorts URL:s
        public static string ResortBase = "api/Resorts";

        public static string AllResorts = "api/Resorts/All";

        public static string ResortByResortId(int id) { return $"api/Resorts/{id}"; }

        public static string CabinsByResortId(int id) { return $"api/Resorts/Cabins/{id}"; }

        //-------------------------------------------------------------------------------------------------------------------------- Invoices

        public static string InvoiceBase = "api/Invoices";

        public static string InvoiceByInvoiceId(int id) { return $"api/Invoices/Id={id}"; }

        // Returns Url by Invoice
        // ResortName, CabinName, FirstName, LastName, Starting, Ending, Status
        // Sets parametres to - if parameter is null
        public static string InvoicesByConditions(Invoice invoice)
        {
            var startDate = invoice.CabinReservation.ReservationStartDate.ToString("MM'-'dd'-'yyyy");
            var endDate = invoice.CabinReservation.ReservationEndDate.ToString("MM'-'dd'-'yyyy");

            if (invoice.CabinReservation.Cabin.Resort.ResortName == null) invoice.CabinReservation.Cabin.Resort.ResortName = "-";
            if (invoice.CabinReservation.Cabin.CabinName == null) invoice.CabinReservation.Cabin.CabinName = "-";
            if (invoice.CabinReservation.Person.FirstName == null) invoice.CabinReservation.Person.FirstName = "-";
            if (invoice.CabinReservation.Person.LastName == null) invoice.CabinReservation.Person.LastName = "-";
            if (invoice.CabinReservation.ReservationStartDate == DateTime.MinValue) startDate = "-";
            if (invoice.CabinReservation.ReservationEndDate == DateTime.MinValue) endDate = "-";

            return $"api/Invoices/ResortName={invoice.CabinReservation.Cabin.Resort.ResortName}/CabinName={invoice.CabinReservation.Cabin.CabinName}" +
                $"/FirstName={invoice.CabinReservation.Person.FirstName}/LastName={invoice.CabinReservation.Person.LastName}/Starting={startDate}/Ending={endDate}/Status={invoice.Status}";
        }

        //--------------------------------------------------------------------------------- Cabins URL:s
        public static string CabinByCabinId(int CabinId) { return $"api/Cabins/{CabinId}"; }

        public static string CabinBase = "api/Cabins";

        // Returns url by resortName, cabinName, ownerLastName
        public static string CabinsByConditions(string resortName, string cabinName, string ownerLastName)
        {
            if (resortName == null) resortName = "-";
            if (cabinName == null) cabinName = "-";
            if (ownerLastName == null) ownerLastName = "-";
            return $"api/Cabins/{resortName}/{cabinName}/{ownerLastName}";
        }

        // Returns url by searchWord, arrival, departure, rooms
        public static string CabinsByConditions(string searchWord, DateTime arrival, DateTime departure, string rooms)
        {
            if (searchWord == null) searchWord = "-";

            var arrivalDate = arrival.ToString("MM'-'dd'-'yyyy");
            var departureDate = departure.ToString("MM'-'dd'-'yyyy");

            if (arrival == DateTime.MinValue) arrivalDate = "-";
            if (departure == DateTime.MinValue) departureDate = "-";

            return $"api/Cabins/{searchWord}/Arrival={arrivalDate}/Departure={departureDate}/Rooms={rooms}";
        }

        //--------------------------------------------------------------------------------- CabinImages URL:s
        public static string CabinImageBase = "api/CabinImages";

        public static string CabinImagesByCabinId(int cabinId) { return $"api/CabinImages/CabinId={cabinId}"; }

        //--------------------------------------------------------------------------------- Activities URL:s
        public static string ActivitiesByResortId(int ResortId) { return $"api/Activities/Resorts/{ResortId}"; }

        public static string ActivityByActivityId(int ActivityId) { return $"api/Activities/{ActivityId}"; }

        public static string ActivityBase = "api/Activities";

        // Returns url by resortName, activityName, activityProvider
        public static string ActivitiesByConditions(string resortName, string activityName, string activityProvider)
        {
            if (resortName == null) resortName = "-";
            if (activityName == null) activityName = "-";
            if (activityProvider == null) activityProvider = "-";
            return $"api/Activities/{resortName}/{activityName}/{activityProvider}";
        }
        //---------------------------------------------------------------------------------- CabinReservations URL:s

        public static string CabinReservationByReservationId(int CabinReservationId) { return $"api/CabinReservations/{CabinReservationId}"; }

        public static string CabinReservationsByCabinId(int CabinId) { return "api/CabinReservations/Cabin/" + CabinId; }

        public static string CabinReservationBase = "api/CabinReservations";

        // Returns Url by CabinReservation
        // Cabin.Resort.ResortName, Cabin.CabinName, Person.LastName, ReservationStartDate, ReservationEndDate
        // Sets parametres to - if parameter is null
        public static string CabinReservationsByConditions(CabinReservation cabinReservation)
        {
            var startDate = cabinReservation.ReservationStartDate.ToString("MM'-'dd'-'yyyy");
            var endDate = cabinReservation.ReservationEndDate.ToString("MM'-'dd'-'yyyy");
            if (cabinReservation.Cabin.Resort.ResortName == null) cabinReservation.Cabin.Resort.ResortName = "-";
            if (cabinReservation.Cabin.CabinName == null) cabinReservation.Cabin.CabinName = "-";
            if (cabinReservation.Person.LastName == null) cabinReservation.Person.LastName = "-";
            if (cabinReservation.ReservationStartDate == DateTime.MinValue) startDate = "-";
            if (cabinReservation.ReservationEndDate == DateTime.MinValue) endDate = "-";
            return $"api/CabinReservations/{cabinReservation.Cabin.Resort.ResortName}/{cabinReservation.Cabin.CabinName}/{cabinReservation.Person.LastName}/{startDate}/{endDate}";
        }

        // Returns Url by Starting, Ending, ResortIDs
        public static string CabinReservationsByConditions(DateTime Starting, DateTime Ending, List<int> ResortIDs)
        {
            var starting = Starting.ToString("MM'-'dd'-'yyyy");
            var ending = Ending.ToString("MM'-'dd'-'yyyy");

            string resortIDs = "";
            foreach (var item in ResortIDs)
            {
                resortIDs = $"{resortIDs}{item},";
            }
            resortIDs = resortIDs.Remove(resortIDs.Length - 1, 1);

            return $"api/CabinReservations/Starting={starting}/Ending={ending}/ResortIds={resortIDs}";
        }

        //------------------------------------------------------------------------------------ Person URL:s
        public static string PersonBase = "api/Persons";

        public static string PersonById(int PersonId) { return $"api/Persons/{PersonId}"; }

        // Returns url by firstName, lastName
        public static string PersonsByFirstNameLastName(string firstName, string lastName)
        {
            if (firstName == null) firstName = "-";
            if (lastName == null) lastName = "-";
            return $"api/Persons/{firstName}/{lastName}";
        }

        public static string PersonByEmail(string Email) { return $"api/Persons/Email={Email}"; }

        //------------------------------------------------------------------------------------ Post URL:s

        public static string PostalCodes = "api/Posts/Postalcodes";

        //------------------------------------------------------------------------------------ ActivityReservation URL:s

        // Returns Url by ActivityReservation
        // CabinReservation.Cabin.Resort.ResortName, Activity.ActivityName, Activity.ActivityProvider, CabinReservation.Person.LastName, Starting for and Ending for
        // Sets parametres to - if parameter is null
        public static string ActivityReservationsByConditions(ActivityReservation activityReservation)
        {
            var startDate = activityReservation.CabinReservation.ReservationStartDate.ToString("MM'-'dd'-'yyyy");
            var endDate = activityReservation.CabinReservation.ReservationEndDate.ToString("MM'-'dd'-'yyyy");
            if (activityReservation.CabinReservation.Cabin.Resort.ResortName == null) activityReservation.CabinReservation.Cabin.Resort.ResortName = "-";
            if (activityReservation.Activity.ActivityName == null) activityReservation.Activity.ActivityName = "-";
            if (activityReservation.Activity.ActivityProvider == null) activityReservation.Activity.ActivityProvider = "-";
            if (activityReservation.CabinReservation.Person.LastName == null) activityReservation.CabinReservation.Person.LastName = "-";
            if (activityReservation.CabinReservation.ReservationStartDate == DateTime.MinValue) startDate = "-";
            if (activityReservation.CabinReservation.ReservationEndDate == DateTime.MinValue) endDate = "-";
            return $"api/ActivityReservations/{activityReservation.CabinReservation.Cabin.Resort.ResortName}/{activityReservation.Activity.ActivityName}/{activityReservation.Activity.ActivityProvider}/{activityReservation.CabinReservation.Person.LastName}/{startDate}/{endDate}";
        }

        public static string ActivityReservationsById(int id) { return $"api/ActivityReservations/{id}"; }
    }
}

using CabinReservationWebApplication.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CommonModels;
using System.Net.Http.Formatting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CabinReservationWebApplication.Models
{
    public class ServiceRepository
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly RestApiConfig restApiConfig;
        private readonly JWTConfig jWTConfig;
        private readonly ILogger<ServiceRepository> logger;

        private readonly HttpClient defaultClient;
        private readonly JsonSerializerOptions caseInsensitiveOptions;

        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1
        public ServiceRepository(IHttpClientFactory httpClientFactory, RestApiConfig config, JWTConfig jWTConfig, ILogger<ServiceRepository> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.restApiConfig = config;
            this.jWTConfig = jWTConfig;
            this.logger = logger;

            this.defaultClient = this.httpClientFactory.CreateClient();
            this.defaultClient.BaseAddress = new Uri(this.restApiConfig.BaseUrl);

            caseInsensitiveOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
        }

        private string GenerateToken(ClaimsPrincipal user)
        {
#if DEBUG
            Stopwatch sw = Stopwatch.StartNew(); // for logging function execution time
#endif
            var tokenHandler = new JwtSecurityTokenHandler();
            DateTime now = DateTime.UtcNow;

            // all claims from user or use some other needed values (Role, Group etc.)
            List<Claim> claims = user.Claims.ToList();
            // Note that user.Claims can contain claims that the API does not need and also that these values can be decoded! Do NOT send any secrets in claims!
            // Capture the created token and paste it to https://jwt.io/ to see what happens.

            // ensure that Name claim is populated -> User.Identity.Name is by default populated from claim http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name (ClaimTypes.Name)
            if (false == claims.Any(c => c.Type == ClaimTypes.Name))
            {
                claims.Add(new Claim(ClaimTypes.Name, user.Identity.Name));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(user.Claims),
                IssuedAt = now,
                NotBefore = now,
                Expires = now.Add(jWTConfig.TokenLifetime),
                Issuer = jWTConfig.Issuer,
                Audience = jWTConfig.Audience,
                SigningCredentials = new SigningCredentials(jWTConfig.SecurityKey, SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            string tokenString = tokenHandler.WriteToken(token);

#if DEBUG
            sw.Stop();
            logger.LogInformation($"Token creation took {sw.ElapsedMilliseconds} ms. The created token is:\n{tokenString}");
#endif            
            return tokenString;
        }

        //------------------------------------------------------------------------------------------------- Resorts

        // Returns All Resorts
        public async Task<IEnumerable<Resort>> GetResorts()
        {
            string url = ApiUrls.AllResorts;

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Resort>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Resort by ClaimsPrincipal and ResortId
        // User must be role Administrator to execute API-call
        public async Task<Resort> GetResort(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ResortByResortId(id);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Resort>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns List<SelectListItems> of Resorts
        public async Task<List<SelectListItem>> GetResortsSelectListItems()
        {
            var resorts = await GetResorts();
            if (resorts == null) return null;
            List<SelectListItem> Resorts = new List<SelectListItem>();
            foreach (var item in resorts)
            {
                Resorts.Add(new SelectListItem { Value = item.ResortId.ToString(), Text = item.ResortName });
            }
            return Resorts;
        }

        // Executes API Post-Call by ClaimsPrincipal and Resort
        // Returns true if Resort created success
        // User must be role Administrator to execute API-call
        public async Task<bool> PostResort(ClaimsPrincipal user, Resort resort)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ResortBase;

            var response = await defaultClient.PostAsync(url, resort, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Put-Call by ClaimsPrincipal, ResortId and Resort
        // Returns true if Resort edited success
        // User must be role Administrator to execute API-call
        public async Task<bool> PutResort(ClaimsPrincipal user, int id, Resort resort)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ResortByResortId(id);

            var response = await defaultClient.PutAsync(url, resort, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Delete-Call by ClaimsPrincipal and ResortId
        // Returns true if Resort deleted success
        // User must be role Administrator to execute API-call
        public async Task<bool> DeleteResort(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ResortByResortId(id);

            var response = await defaultClient.DeleteAsync(url);

            return response.IsSuccessStatusCode ? true : false;
        }

        //------------------------------------------------------------------------------------------------- Resorts End

        //------------------------------------------------------------------------------------------------- Cabins

        // Returns Cabin by CabinId
        public async Task<Cabin> GetCabin(int CabinId)
        {
            string url = ApiUrls.CabinByCabinId(CabinId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Cabin>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Cabin by ClaimsPrincipal and CabinId, response includes Person
        // User must be role Administrator or CabinOwner can get his own Cabin
        public async Task<Cabin> GetCabin(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinByCabinId(id);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Cabin>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Cabins by ResortsId
        public async Task<IEnumerable<Cabin>> GetCabins(int ResortId)
        {
            string url = ApiUrls.CabinsByResortId(ResortId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Cabin>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Cabins by ClaimsPrincipal
        // CabinOwner getting his own Cabins
        public async Task<IEnumerable<Cabin>> GetCabins(ClaimsPrincipal user)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinBase;

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Cabin>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Cabins by ClaimsPrincipal, ResortName, CabinName, PersonLastName
        // User must be role Administrator to execute API-call
        public async Task<IEnumerable<Cabin>> GetCabins(ClaimsPrincipal user, string resortName, string cabinName, string personLastName)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinsByConditions(resortName, cabinName, personLastName);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Cabin>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Cabins by searchWord, arrival, departure, rooms
        public async Task<IEnumerable<Cabin>> GetCabins(string searchWord, DateTime arrival, DateTime departure, string rooms)
        {
            string url = ApiUrls.CabinsByConditions(searchWord, arrival, departure, rooms);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Cabin>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Executes API Post-Call by ClaimsPrincipal and Cabin
        // Returns true if Cabin created success
        // User must be role Administrator or CabinOwner can create Cabin by own PersonId
        public async Task<bool> PostCabin(ClaimsPrincipal user, Cabin cabin)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinBase;

            var response = await defaultClient.PostAsync(url, cabin, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Put-Call by ClaimsPrincipal, CabinId and Cabin
        // Returns true if Cabin edited success
        // User must be role Administrator or CabinOwner can edit his own Cabin
        public async Task<bool> PutCabin(ClaimsPrincipal user, int id, Cabin cabin)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinByCabinId(id);

            var response = await defaultClient.PutAsync(url, cabin, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Delete-Call by ClaimsPrincipal and CabinId
        // Returns true if Cabin deleted success
        // User must be role Administrator or CabinOwner can delete his own Cabin
        public async Task<bool> DeleteCabin(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinByCabinId(id);

            var response = await defaultClient.DeleteAsync(url);

            return response.IsSuccessStatusCode ? true : false;
        }

        //------------------------------------------------------------------------------------------------- Cabins End

        //------------------------------------------------------------------------------------------------- CabinImages

        // Executes API Post-Call by User and CabinImage 
        // Returns true if CabinImage created success
        public async Task<bool> PostCabinImage(ClaimsPrincipal user, CabinImage cabinImage)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinImageBase;

            var response = await defaultClient.PostAsync(url, cabinImage, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Returns list of CabinImages by CabinId
        public async Task<IEnumerable<CabinImage>> GetCabinImages(int CabinId)
        {
            string url = ApiUrls.CabinImagesByCabinId(CabinId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<CabinImage>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Executes API Delete-Call by ClaimsPrincipal and CabinId
        // Returns true if Cabin deleted success
        // User must be role Administrator or CabinOwner can delete his own CabinImage
        public async Task<bool> DeleteCabinImage(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinImagesByCabinId(id);

            var response = await defaultClient.DeleteAsync(url);

            return response.IsSuccessStatusCode ? true : false;
        }

        //------------------------------------------------------------------------------------------------- CabinImages End

        //------------------------------------------------------------------------------------------------- Activities

        // Returns list of Activities by ResortId
        public async Task<IEnumerable<Activity>> GetActivities(int ResortId)
        {
            string url = ApiUrls.ActivitiesByResortId(ResortId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Activity>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Activities by ClaimsPrincipal, ResortName, ActivityName, ActivityProvider
        // User must be role Administrator to execute API-call
        public async Task<IEnumerable<Activity>> GetActivities(ClaimsPrincipal user, string resort, string activityName, string activityProvider)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ActivitiesByConditions(resort, activityName, activityProvider);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Activity>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Activity by ActivityId
        public async Task<Activity> GetActivity(int ActivityId)
        {
            string url = ApiUrls.ActivityByActivityId(ActivityId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Activity>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Executes API Post-Call by ClaimsPrincipal and activity
        // Returns true if Activity created success
        // User must be role Administrator to execute API-call
        public async Task<bool> PostActivity(ClaimsPrincipal user, Activity activity)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ActivityBase;

            var response = await defaultClient.PostAsync(url, activity, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Put-Call by ClaimsPrincipal, ActivityId and Activity
        // Returns true if Activity edited success
        // User must be role Administrator to execute API-call
        public async Task<bool> PutActivity(ClaimsPrincipal user, int id, Activity activity)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ActivityByActivityId(id);

            var response = await defaultClient.PutAsync(url, activity, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Delete-Call by ClaimsPrincipal and ActivityId
        // Returns true if Activity deleted success
        // User must be role Administrator to execute API-call
        public async Task<bool> DeleteActivity(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ActivityByActivityId(id);

            var response = await defaultClient.DeleteAsync(url);

            return response.IsSuccessStatusCode ? true : false;
        }

        //------------------------------------------------------------------------------------------------- Invoices

        // Returns Invoices by ClaimsPrincipal and Invoice
        // User must be role Administrator or CabinOwner can get his own Cabins Invoices
        public async Task<IEnumerable<Invoice>> GetInvoices(ClaimsPrincipal user, Invoice invoice)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.InvoicesByConditions(invoice);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Invoice>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Invoice by ClaimsPrincipal and InvoiceId
        // User must be role Administrator or CabinOwner can get his own Cabin Invoice and own CabinReservation Invoice or Customer can get his own CabinReservation Invoice
        public async Task<Invoice> GetInvoice(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.InvoiceByInvoiceId(id);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Invoice>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Executes API Post-Call by ClaimsPrincipal and Invoice
        // Returns true if Invoice created success
        // User must be role Administrator or CabinOwner can create new Invoice only in his own Cabin
        public async Task<bool> PostInvoice(ClaimsPrincipal user, Invoice invoice)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.InvoiceBase;

            var response = await defaultClient.PostAsync(url, invoice, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Put-Call by ClaimsPrincipal, InvoiceId and Invoice
        // Returns true if Invoice edited success
        // User must be role Administrator or CabinOwner can edit his own Cabin Invoice
        public async Task<bool> PutInvoice(ClaimsPrincipal user, int id, Invoice invoice)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.InvoiceByInvoiceId(id);

            var response = await defaultClient.PutAsync(url, invoice, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Delete-Call by ClaimsPrincipal and InvoiceId
        // Returns true if Invoice deleted success
        // User must be role Administrator to execute API-call
        public async Task<bool> DeleteInvoice(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.InvoiceByInvoiceId(id);

            var response = await defaultClient.DeleteAsync(url);

            return response.IsSuccessStatusCode ? true : false;
        }

        //------------------------------------------------------------------------------------------------- Invoices End

        //------------------------------------------------------------------------------------------------- CabinReservations

        // Returns CabinReservation by CabinReservationId
        // User must be role Administrator or User can get only own CabinReservation or CabinOwner can get his own CabinReservation and own Cabin CabinReservation
        public async Task<CabinReservation> GetCabinReservation(ClaimsPrincipal user, int CabinReservationId)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinReservationByReservationId(CabinReservationId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<CabinReservation>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns User all CabinReservations by ClaimsPrincipal
        public async Task<IEnumerable<CabinReservation>> GetCabinReservations(ClaimsPrincipal user)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinReservationBase;

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<CabinReservation>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns list of CabinReservations by CabinId
        public async Task<IEnumerable<CabinReservation>> GetCabinReservations(int CabinId)
        {
            string url = ApiUrls.CabinReservationsByCabinId(CabinId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<CabinReservation>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns list of CabinReservations by ClaimsPrincipal and CabinId
        // User must be role Administrator or CabinOwner can get his own Cabin CabinReservations
        public async Task<IEnumerable<CabinReservation>> GetCabinReservations(ClaimsPrincipal user, int CabinId)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinReservationsByCabinId(CabinId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<CabinReservation>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns CabinReservations by ClaimsPrincipal, ResortIDs, Starting- and EndingDates
        // User must be role Administrator to execute API-call
        public async Task<IEnumerable<CabinReservation>> GetCabinReservations(ClaimsPrincipal user, DateTime Starting, DateTime Ending, List<int> ResortIDs)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinReservationsByConditions(Starting, Ending, ResortIDs);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<CabinReservation>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns CabinReservations by ClaimsPrincipal and CabinReservation
        // User must be role Administrator or CabinOwner can get his own Cabin CabinReservations
        public async Task<IEnumerable<CabinReservation>> GetCabinReservations(ClaimsPrincipal user, CabinReservation cabinReservation)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinReservationsByConditions(cabinReservation);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<CabinReservation>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Executes API Post-Call by User and CabinReservation 
        // Returns true if CabinReservation created success
        // Customer/CabinOwner can create CabinReservation by own PersonId or Administrator can create CabinReservation all PersonId:s
        public async Task<bool> PostCabinReservation(ClaimsPrincipal user, CabinReservation cabinReservation)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinReservationBase;

            var response = await defaultClient.PostAsync(url, cabinReservation, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Post-Call by User and CabinReservation 
        // Returns true if CabinReservation edited success
        public async Task<bool> PutCabinReservation(ClaimsPrincipal user, CabinReservation cabinReservation)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinReservationBase;

            var response = await defaultClient.PutAsync(url, cabinReservation, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Delete-Call by ClaimsPrincipal and CabinReservationId
        // Returns true if CabinReservation deleted success
        // User must be role Administrator or User can delete only own CabinReservation or CabinOwner can delete his own Cabin CabinReservation
        public async Task<bool> DeleteCabinReservation(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.CabinReservationByReservationId(id);

            var response = await defaultClient.DeleteAsync(url);

            return response.IsSuccessStatusCode ? true : false;
        }

        //------------------------------------------------------------------------------------------------- CabinReservations End

        //------------------------------------------------------------------------------------------------- Persons

        // Return Person by ClaimsPrincipal
        // User getting his own Person Information
        public async Task<Person> GetPerson(ClaimsPrincipal user)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.PersonBase;

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Person>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Person by ClaimsPrincipal and PersonId
        // User must be role Administrator to execute API-call
        public async Task<Person> GetPerson(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.PersonById(id);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Person>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Person by ClaimsPrincipal and Email
        // User must be role Administrator to execute API-call
        public async Task<Person> GetPerson(ClaimsPrincipal user, string Email)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.PersonByEmail(Email);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Person>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns Persons by ClaimsPrincipal, FirstName, LastName
        // User must be role Administrator to execute API-call
        public async Task<IEnumerable<Person>> GetPersons(ClaimsPrincipal user, string firstName, string lastName)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.PersonsByFirstNameLastName(firstName, lastName);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<Person>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Executes API Post-Call by Person
        // Returns true if Person created success
        public async Task<bool> PostPerson(Person person)
        {
            string url = ApiUrls.PersonBase;

            var response = await defaultClient.PostAsync(url, person, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Put-Call by ClaimsPrincipal, PersonId and Person
        // Returns true if Person edited success
        // User must be role Administrator or Person can edit only his own information
        public async Task<bool> PutPerson(ClaimsPrincipal user, int id, Person person)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.PersonById(id);

            var response = await defaultClient.PutAsync(url, person, new JsonMediaTypeFormatter());

            return response.IsSuccessStatusCode ? true : false;
        }

        // Executes API Delete-Call by ClaimsPrincipal and PersonId
        // Returns true if Person deleted success
        // User must be role Administrator to execute API-call
        // TODO: Must allow also that user can delete his own details
        public async Task<bool> DeletePerson(ClaimsPrincipal user, int id)
        {
            var token = GenerateToken(user); // this is for demo! Consider other means to create the token in real apps.
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.PersonById(id);

            var response = await defaultClient.DeleteAsync(url);

            return response.IsSuccessStatusCode ? true : false;
        }

        //------------------------------------------------------------------------------------------------- Persons End

        //-------------------------------------------------------------------------------------------------- PostalCodes

        // Returns string-List of PostalCodes
        public async Task<List<string>> GetPostalCodes()
        {
            string url = ApiUrls.PostalCodes;

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<List<string>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        // Returns List<SelectListItems> of PostalCodes for DropDownMenu
        public async Task<List<SelectListItem>> GetPostalCodesSelectListItems()
        {
            var postalCodes = await GetPostalCodes();
            if (postalCodes == null) return null;
            List<SelectListItem> PostalCodes = new List<SelectListItem>();
            foreach (var item in postalCodes)
            {
                PostalCodes.Add(new SelectListItem { Value = item, Text = item });
            }
            return PostalCodes;
        }

        //-------------------------------------------------------------------------------------------------- PostalCodes End

        //-------------------------------------------------------------------------------------------------- ActivityReservations

        // Returns ActivityReservations by ClaimsPrincipal and ActivityReservation
        // User must be role Administrator to execute API-call
        public async Task<IEnumerable<ActivityReservation>> GetActivityReservations(ClaimsPrincipal user, ActivityReservation activityReservation)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ActivityReservationsByConditions(activityReservation);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<ActivityReservation>>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }


        // Returns ActivityReservation by ActivityReservationId
        // User must be role Administrator to execute API-call
        // TODO: Can User get own ActivityReservation? Maybe not, because he can get own CabinReservations and ActivityReservations are linked in that
        public async Task<ActivityReservation> GetActivityReservation(ClaimsPrincipal user, int ActivityReservationId)
        {
            var token = GenerateToken(user);
            defaultClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string url = ApiUrls.ActivityReservationsById(ActivityReservationId);

            var response = await defaultClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<ActivityReservation>(responseStream, caseInsensitiveOptions);
            }
            return null;
        }

        //-------------------------------------------------------------------------------------------------- ActivityReservations End

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Timers;
using System.Data.Entity.SqlServer;
using System.Threading;
using System.Net.Http.Headers;

namespace CarWatch.Controllers
{
    public class ExchangeController : ApiController
    {
        private string k_TheServer = "TheServer";
        private string k_ExchangeCancelMessage = "The exchange has been canceled.";
        private HttpClient client = new HttpClient();

        public ExchangeController()
        {
            client.BaseAddress = new Uri("https://carwatchapp.azurewebsites.net/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public class ExchangeStatus
        {
            public int status { get; set; }
        }

        public class Point
        {
            public double Longitude { get; set; }
            public double Latitude { get; set; }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> AddSearch([FromBody] Search i_ParkingSpotSearch)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_ParkingSpotSearch.Nickname != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Search result = await entities.Searches.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if (result != null)
                {
                    entities.Searches.Remove(result);
                }
                DateTime timeUtc = DateTime.UtcNow;
                TimeZoneInfo iLZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
                i_ParkingSpotSearch.TimeOpened = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, iLZone);
                entities.Searches.Add(i_ParkingSpotSearch);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> RemoveSearch(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Search result = await entities.Searches.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if (result == null)
                {
                    return BadRequest("This nickname has not been searching for a parking spot.");
                }
                entities.Searches.Remove(result);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> CheckSearch(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Search result = await entities.Searches.FirstOrDefaultAsync(e => e.Nickname == nickname);
                return Ok(result);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> AddProposal([FromBody] Proposal i_ParkingSpotProposal)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_ParkingSpotProposal.Nickname != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Proposal result = await entities.Proposals.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if (result != null)
                {
                    entities.Proposals.Remove(result);
                }
                DateTime timeUtc = DateTime.UtcNow;
                TimeZoneInfo iLZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
                i_ParkingSpotProposal.TimeOpened = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, iLZone);
                entities.Proposals.Add(i_ParkingSpotProposal);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> RemoveProposal(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Proposal result = await entities.Proposals.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if (result == null)
                {
                    return BadRequest("This nickname has not proposed a parking spot.");

                }
                entities.Proposals.Remove(result);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> CheckProposal(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Proposal result = await entities.Proposals.FirstOrDefaultAsync(e => e.Nickname == nickname);
                return Ok(result);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> UpdateExchangeStatus([FromBody] ExchangeStatus i_Status)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Exchange result = await entities.Exchanges.FirstOrDefaultAsync(e => e.ConsumerNickname == nickname && e.Status == 0);
                if (result == null)
                {
                    return BadRequest("you are not involved with an open exchange.");
                }
                result.Status = i_Status.status;
                DateTime timeUtc = DateTime.UtcNow;
                TimeZoneInfo iLZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
                result.TimeExchanged = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, iLZone);
                await entities.SaveChangesAsync();
                return Ok(result);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> CheckExchange(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Exchange result = await entities.Exchanges.FirstOrDefaultAsync(e => (e.ConsumerNickname == nickname || e.ProviderNickname == nickname) && e.Status == 0);
                return Ok(result);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> UpdateDriverLocation(Point i_Point)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Exchange result = await entities.Exchanges.FirstOrDefaultAsync(e => e.ConsumerNickname == nickname && e.Status == 0);
                if (result == null)
                {
                    return BadRequest("you are not involved with an open exchange.");
                }
                result.DriverLongitude = i_Point.Longitude;
                result.DriverLatitude = i_Point.Latitude;
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetDriverLocation(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Exchange result = await entities.Exchanges.FirstOrDefaultAsync(e => e.ProviderNickname == nickname && e.Status == 0);
                if (result == null)
                {
                    return BadRequest("you are not involved with an open exchange.");
                }
                Point point = new Point();
                point.Longitude = result.DriverLongitude;
                point.Latitude = result.DriverLatitude;
                return Ok(point);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> CancelExchange(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Exchange result = await entities.Exchanges.FirstOrDefaultAsync(e => (e.ProviderNickname == nickname || e.ConsumerNickname == nickname) && e.Status == 0);
                if (result == null)
                {
                    return BadRequest("you are not involved with an open exchange.");
                }
                pushToClient(result.ConsumerNickname);
                pushToClient(result.ProviderNickname);
                entities.Exchanges.Remove(result);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        private async void pushToClient(string i_Nickname)
        {
            TodoItem todoItem = new TodoItem();
            todoItem.Text = k_TheServer + ";" + i_Nickname + ";send;" + k_ExchangeCancelMessage;
            var response = await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
        }
    }
}

using DataAccess;
using System;
using System.Data.Entity;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace CarWatch.Controllers
{
    public class ExchangeController : ApiController
    {
        private string k_TheServer = "TheServer";
        private string k_MessageToProposer = "בדקה הקרובה יגיע הנהג";
        private string k_MessageToSearcher = "בדקה הקרובה תגיעו לחניה";
        private string k_ExchangeSuccessMessage = "החלפת החניה בוצעה בהצלחה";
        private string k_ExchangeCancelByProposerMessage = "מציע החניה ביטל את ההחלפה";
        private string k_ExchangeCancelBySearcherMessage = "מחפש החניה ביטל את ההחלפה";
        private string k_ExchangeCancelNoParkingSpotMessage = "החלפת החניה בוטלה, החניה לא פונתה בזמן";

        enum e_ExchangeStatus
        {
            Success = 1,
            CanceledBySearcherPriorArrival = 2,
            CanceledByProposerPriorArrival = 3,
            CancelNoParkingSpot = 4};

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
                i_ParkingSpotSearch.TimeOpened = DateTime.UtcNow;
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
                i_ParkingSpotProposal.TimeOpened = DateTime.UtcNow;
                entities.Proposals.Add(i_ParkingSpotProposal);
                FacebookAccount proposerAccount = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == nickname);
                proposerAccount.Rank++;
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
                FacebookAccount proposerAccount = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if(proposerAccount.Rank > 0)
                    proposerAccount.Rank--;
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
                Exchange result = await entities.Exchanges.FirstOrDefaultAsync(e => (e.ConsumerNickname == nickname || e.ProviderNickname == nickname) && e.Status == 0);
                if (result == null)
                {
                    return BadRequest("you are not involved with an open exchange.");
                }

                if(i_Status.status == (int)e_ExchangeStatus.Success)
                {
                    pushToClient(result.ConsumerNickname, "exchangeStatusUpdate", k_ExchangeSuccessMessage);
                    pushToClient(result.ProviderNickname, "exchangeStatusUpdate", k_ExchangeSuccessMessage);
                    FacebookAccount proposer = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == result.ProviderNickname);
                    proposer.Rank += 5;
                }
                else if (i_Status.status == (int)e_ExchangeStatus.CanceledBySearcherPriorArrival)
                {
                    pushToClient(result.ConsumerNickname, "exchangeStatusUpdate", k_ExchangeCancelBySearcherMessage);
                    pushToClient(result.ProviderNickname, "exchangeStatusUpdate", k_ExchangeCancelBySearcherMessage);
                }
                else if (i_Status.status == (int)e_ExchangeStatus.CanceledByProposerPriorArrival)
                {
                    pushToClient(result.ConsumerNickname, "exchangeStatusUpdate", k_ExchangeCancelByProposerMessage);
                    pushToClient(result.ProviderNickname, "exchangeStatusUpdate", k_ExchangeCancelByProposerMessage);
                    FacebookAccount proposerAccount = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == result.ProviderNickname);
                    proposerAccount.Rank -= 2;
                    if (proposerAccount.Rank < 0)
                        proposerAccount.Rank = 0;
                }
                else if(i_Status.status == (int)e_ExchangeStatus.CancelNoParkingSpot)
                {
                    pushToClient(result.ConsumerNickname, "exchangeStatusUpdate", k_ExchangeCancelNoParkingSpotMessage);
                    pushToClient(result.ProviderNickname, "exchangeStatusUpdate", k_ExchangeCancelNoParkingSpotMessage);
                    FacebookAccount proposerAccount = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == result.ProviderNickname);
                    proposerAccount.Rank -= 3;
                    if (proposerAccount.Rank < 0)
                        proposerAccount.Rank = 0;
                }
                else
                {
                    return BadRequest("Illegal status code.");
                }
                result.Status = i_Status.status;
                result.TimeExchanged = DateTime.UtcNow;
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
        public async Task<IHttpActionResult> SendProximityAlert(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                Exchange result = await entities.Exchanges.FirstOrDefaultAsync(e => (e.ProviderNickname == nickname || e.ConsumerNickname == nickname) && e.Status == 0);
                if (result == null)
                {
                    return BadRequest("you are not involved with an open exchange.");
                }
                pushToClient(result.ConsumerNickname, "sendExchangeAlert", k_MessageToSearcher);
                pushToClient(result.ProviderNickname, "sendExchangeAlert", k_MessageToProposer);
                return Ok();
            }
        }


        private async void pushToClient(string i_Nickname, string i_MethodName, string i_Message)
        {
            TodoItem todoItem = new TodoItem();
            todoItem.Text = k_TheServer + ";" + i_Nickname + ";" + i_MethodName + ";" + i_Message;
            var response = await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
        }
    }
}

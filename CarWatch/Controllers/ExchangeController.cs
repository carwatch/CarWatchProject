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

namespace CarWatch.Controllers
{
    public class ExchangeController : ApiController
    {
        private int k_EarthRadius = 6371;
        private int k_SearchingTime = 30; // seconds

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> AddSearch([FromBody] Search i_ParkingSpotSearch)
        {
            string email = Thread.CurrentPrincipal.Identity.Name;
            if (i_ParkingSpotSearch.Email != email)
            {
                return BadRequest("Emails do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Searches.FirstOrDefaultAsync(e => e.Email == email);
                if (result != null)
                {
                    entities.Searches.Remove(result);
                }
                entities.Searches.Add(i_ParkingSpotSearch);
                await entities.SaveChangesAsync();
                return Created("Search has started.", i_ParkingSpotSearch);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> RemoveSearch()
        {
            string email = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Searches.FirstOrDefaultAsync(e => e.Email == email);
                if (result == null)
                {
                    return BadRequest("This email address has not been searching for a parking spot.");

                }
                entities.Searches.Remove(result);
                await entities.SaveChangesAsync();
                return Ok("Search has been canceled.");
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> AddProposal([FromBody] Proposal i_ParkingSpotProposal)
        {
            string email = Thread.CurrentPrincipal.Identity.Name;
            if (i_ParkingSpotProposal.Email != email)
            {
                return BadRequest("Emails do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                DateTime timeout = DateTime.Now.AddSeconds(k_SearchingTime);
                do
                {
                    var result = await entities.Searches.Where(
                        e => 2 * k_EarthRadius * (SqlFunctions.SquareRoot(SqlFunctions.Square(SqlFunctions.Sin((SqlFunctions.Radians(i_ParkingSpotProposal.Latitude) - SqlFunctions.Radians(e.Latitude)) / 2)) + SqlFunctions.Cos(SqlFunctions.Radians(i_ParkingSpotProposal.Latitude)) * SqlFunctions.Cos(SqlFunctions.Radians(e.Latitude)) * SqlFunctions.Square(SqlFunctions.Sin((SqlFunctions.Radians(i_ParkingSpotProposal.Longitude) - SqlFunctions.Radians(e.Longitude)) / 2)))) <= e.Distance).Select(e => e.Email).ToListAsync();
                    if (result.Count > 0)
                    {
                        var firstAccount = result[0];
                        var parkingSpotMatch = await entities.Accounts.FirstOrDefaultAsync(e => e.Email.CompareTo(firstAccount) == 0);
                        foreach (var item in result)
                        {
                            var account = await entities.Accounts.FirstOrDefaultAsync(e => e.Email.CompareTo(item) == 0);
                            if (account.Rank > parkingSpotMatch.Rank)
                                parkingSpotMatch = account;
                        }
                        return Ok(parkingSpotMatch);
                    }
                }
                while (DateTime.Now <= timeout);

                return BadRequest("No match has been found.");
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> RemoveProposal()
        {
            string email = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Proposals.FirstOrDefaultAsync(e => e.Email == email);
                if (result == null)
                {
                    return BadRequest("This email address has not proposed a parking spot.");

                }
                entities.Proposals.Remove(result);
                await entities.SaveChangesAsync();
                return Ok("Proposal has been canceled.");
            }
        }



        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> Exchange([FromBody] Exchange i_Transaction)
        {
            string email = Thread.CurrentPrincipal.Identity.Name;
            if (i_Transaction.ProviderEmail != email)
            {
                return BadRequest("Emails do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var account = await entities.Accounts.FirstOrDefaultAsync(e => e.Email == i_Transaction.ProviderEmail);
                if (account == null)
                {
                    return BadRequest("Invalid email address.");
                }
                account.Rank++;
                entities.Exchanges.Add(i_Transaction);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }
    }
}

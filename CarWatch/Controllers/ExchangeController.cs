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
        private int k_SearchingTime = 300; // seconds

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
                var result = await entities.Searches.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if (result != null)
                {
                    entities.Searches.Remove(result);
                }
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
                var result = await entities.Searches.FirstOrDefaultAsync(e => e.Nickname == nickname);
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
        public async Task<IHttpActionResult> AddProposal([FromBody] Proposal i_ParkingSpotProposal)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_ParkingSpotProposal.Nickname != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Proposals.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if (result != null)
                {
                    entities.Proposals.Remove(result);
                }
                entities.Proposals.Add(i_ParkingSpotProposal);
                await entities.SaveChangesAsync();

                DateTime timeout = DateTime.Now.AddSeconds(k_SearchingTime);
                do
                {
                    List<Search> searchList = await entities.Searches.Where(
                        e => 2 * k_EarthRadius * (SqlFunctions.SquareRoot(SqlFunctions.Square(SqlFunctions.Sin((SqlFunctions.Radians(i_ParkingSpotProposal.Latitude) - SqlFunctions.Radians(e.Latitude)) / 2)) + SqlFunctions.Cos(SqlFunctions.Radians(i_ParkingSpotProposal.Latitude)) * SqlFunctions.Cos(SqlFunctions.Radians(e.Latitude)) * SqlFunctions.Square(SqlFunctions.Sin((SqlFunctions.Radians(i_ParkingSpotProposal.Longitude) - SqlFunctions.Radians(e.Longitude)) / 2)))) <= e.Distance).ToListAsync();
                    if (searchList.Count > 0)
                    {
                        Search searchToRemove = searchList[0];
                        var firstAccountNickname = searchList[0].Nickname;
                        var parkingSpotMatch = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname.CompareTo(firstAccountNickname) == 0);
                        foreach (var item in searchList)
                        {
                            var account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname.CompareTo(item.Nickname) == 0);
                            if (account.Rank > parkingSpotMatch.Rank)
                            {
                                parkingSpotMatch = account;
                                searchToRemove = item;
                            }
                        }
                        var isAlive = await entities.Proposals.FirstOrDefaultAsync(e => e.Nickname == nickname);
                        if (isAlive == null)
                        {
                            return BadRequest("This nickname has not been providing a parking spot.");
                        }
                        entities.Searches.Remove(searchToRemove);
                        entities.Proposals.Remove(i_ParkingSpotProposal);
                        await entities.SaveChangesAsync();
                        parkingSpotMatch.FacebookSID = string.Empty;
                        return Ok(parkingSpotMatch);
                    }
                }
                while (DateTime.Now <= timeout);
                entities.Proposals.Remove(i_ParkingSpotProposal);
                await entities.SaveChangesAsync();
                return BadRequest("No match has been found.");
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> RemoveProposal(Object obj)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Proposals.FirstOrDefaultAsync(e => e.Nickname == nickname);
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
        public async Task<IHttpActionResult> Exchange([FromBody] Exchange i_Transaction)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Transaction.ConsumerNickname != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                FacebookAccount account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == i_Transaction.ProviderNickname);
                if (account == null)
                {
                    return BadRequest("Invalid nickname.");
                }

                // Exchange successful
                if(i_Transaction.Status == 1)
                    account.Rank++;

                entities.Exchanges.Add(i_Transaction);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }
    }
}

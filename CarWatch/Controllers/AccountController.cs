using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Threading;

namespace CarWatch.Controllers
{
    public class AccountController : ApiController
    {
        public class UserOnlineStatus
        {
            public int Status { get; set; }
        }

        public class ChatPartner
        {
            public string Partner { get; set; }
        }

        public static bool Authenticate(string i_Nickname, string i_SID)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                return entities.FacebookAccounts.Any(user => user.Nickname == i_Nickname && user.FacebookSID == i_SID);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> LoginWithFacebook([FromBody] FacebookAccount i_Account)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var isExistSID = await entities.FacebookAccounts.AnyAsync(e => e.FacebookSID == i_Account.FacebookSID);
                if (isExistSID)
                {
                    var account = await entities.FacebookAccounts.Where(e => e.FacebookSID == i_Account.FacebookSID).FirstOrDefaultAsync();
                    return Ok(account);
                }
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Register([FromBody] FacebookAccount i_Account)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var isExistNickname = await entities.FacebookAccounts.AnyAsync(e => e.Nickname == i_Account.Nickname);
                if (isExistNickname)
                {
                    return BadRequest("nameinuse");
                }

                if (i_Account.LicensePlate != null)
                {
                    var isExistLicensePlate = await entities.FacebookAccounts.AnyAsync(e => e.LicensePlate == i_Account.LicensePlate);
                    if (isExistLicensePlate)
                    {
                        return BadRequest("licenseinuse");
                    }
                }

                entities.FacebookAccounts.Add(i_Account);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetSIDByNickname([FromBody] FacebookAccount i_Account)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var isExist = await entities.FacebookAccounts.AnyAsync(e => e.Nickname == i_Account.Nickname);
                if (!isExist)
                {
                    return BadRequest("This nickname is not registered.");
                }
                var result = await entities.FacebookAccounts.Where(e => e.Nickname == i_Account.Nickname).FirstOrDefaultAsync();
                return Ok(result.FacebookSID);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetSIDByLicensePlate([FromBody] FacebookAccount i_Account)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var isExist = await entities.FacebookAccounts.AnyAsync(e => e.LicensePlate == i_Account.LicensePlate);
                if (!isExist)
                {
                    return BadRequest("This license plate is not registered.");
                }
                var result = await entities.FacebookAccounts.Where(e => e.LicensePlate == i_Account.LicensePlate).FirstOrDefaultAsync();
                return Ok(result.FacebookSID);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> SetLicensePlateByNickname([FromBody] FacebookAccount i_Account)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var isExist = await entities.FacebookAccounts.AnyAsync(e => e.Nickname == i_Account.Nickname);
                if (!isExist)
                {
                    return BadRequest("This nickname is not registered.");
                }
                var isTaken = await entities.FacebookAccounts.AnyAsync(e => e.LicensePlate == i_Account.LicensePlate);
                if (isTaken)
                {
                    return BadRequest("This license plate is already in use.");
                }
                var result = await entities.FacebookAccounts.Where(e => e.Nickname == i_Account.Nickname).FirstOrDefaultAsync();
                result.LicensePlate = i_Account.LicensePlate;
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> SetIsOnline([FromBody] UserOnlineStatus i_Status)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                FacebookAccount account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if(account == null)
                {
                    return BadRequest("This nickname is not registered.");
                }
                account.IsOnline = i_Status.Status;
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> SetChatPartner([FromBody] ChatPartner i_Partner)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                FacebookAccount account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == nickname);
                if (account == null)
                {
                    return BadRequest("This nickname is not registered.");
                }
                account.ChatPartner = i_Partner.Partner;
                await entities.SaveChangesAsync();
                return Ok();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DataAccess;
using System.Threading.Tasks;
using System.Data.Entity;

namespace CarWatch.Controllers
{
    public class AccountController : ApiController
    {
        [HttpPost]
        public async Task<IHttpActionResult> Register([FromBody] Account i_Account)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var account = await entities.Accounts.FindAsync(i_Account.Email);
                if (account != null)
                {
                    return BadRequest("Email is already in use.");
                }

                i_Account.Rank = 0;
                entities.Accounts.Add(i_Account);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Login([FromBody] Account i_Account)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Accounts.AnyAsync(e => e.Email == i_Account.Email & e.Password == i_Account.Password);
                if (result)
                {
                    return Ok();
                }

                return BadRequest("The username or password is incorrect.");
            }
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
                    var result = await entities.FacebookAccounts.Where(e => e.FacebookSID == i_Account.FacebookSID).FirstOrDefaultAsync();
                    return Ok(result.Nickname);
                }
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> SetNickname([FromBody] FacebookAccount i_Account)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var isExistNickname = await entities.FacebookAccounts.AnyAsync(e => e.Nickname == i_Account.Nickname);
                if (isExistNickname)
                {
                    return BadRequest("nameinuse");
                }

                if (i_Account.LicensePlate != "")
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
    }
}

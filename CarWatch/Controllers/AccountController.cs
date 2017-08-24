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
                    return Ok(i_Account);
                }

                return BadRequest("The username or password is incorrect.");
            }
        }

        public static bool Authenticate(string i_Email, string i_Password)
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                return entities.Accounts.Any(user => user.Email == i_Email && user.Password == i_Password);
            }
        }
    }
}

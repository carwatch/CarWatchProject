using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using DataAccess;
using System.Data.Entity;
using System.Threading;

namespace CarWatch.Controllers
{
    public class MessageController : ApiController
    {
        public class MessageDetails
        {
            public string SenderNickname { get; set; }
            public string ReceiverNickname { get; set; }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> Send([FromBody] Message i_Message)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.SenderNickname != nickname)
            {
                return BadRequest("Emails do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                entities.Messages.Add(i_Message);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpGet]
        public async Task<IHttpActionResult> GetMessagesHistory([FromBody] MessageDetails i_Message)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.SenderNickname != nickname)
            {
                return BadRequest("Emails do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Messages.Where(e => (e.SenderNickname == i_Message.SenderNickname && e.ReceiverNickname == i_Message.ReceiverNickname) || (e.SenderNickname == i_Message.ReceiverNickname && e.ReceiverNickname == i_Message.SenderNickname)).ToListAsync();
                return Ok(result);
            }
        }

    }
}

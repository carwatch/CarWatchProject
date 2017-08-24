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
            public string SenderEmail { get; set; }
            public string ReceiverEmail { get; set; }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> Send([FromBody] Message i_Message)
        {
            string email = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.SenderEmail != email)
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
            string email = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.SenderEmail != email)
            {
                return BadRequest("Emails do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Messages.Where(e => (e.SenderEmail == i_Message.SenderEmail && e.ReceiverEmail == i_Message.ReceiverEmail) || (e.SenderEmail == i_Message.ReceiverEmail && e.ReceiverEmail == i_Message.SenderEmail)).ToListAsync();
                return Ok();
            }
        }

    }
}

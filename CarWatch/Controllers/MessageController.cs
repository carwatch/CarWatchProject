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
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                entities.Messages.Add(i_Message);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetMessagesHistory([FromBody] MessageDetails i_Message)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.SenderNickname != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                var result = await entities.Messages.Where(e => (e.SenderNickname == i_Message.SenderNickname && e.ReceiverNickname == i_Message.ReceiverNickname) || (e.SenderNickname == i_Message.ReceiverNickname && e.ReceiverNickname == i_Message.SenderNickname)).ToListAsync();
                return Ok(result);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetContacts([FromBody] MessageDetails i_Message)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.SenderNickname != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                List<string> nicknameList = new List<string>();
                var messages = await entities.Messages.Where(e => (e.SenderNickname == i_Message.SenderNickname) || (e.ReceiverNickname == i_Message.SenderNickname)).ToListAsync();
                foreach (Message msg in messages)
                {
                    if (i_Message.SenderNickname != msg.SenderNickname)
                    {
                        if (!nicknameList.Contains(msg.SenderNickname))
                            nicknameList.Add(msg.SenderNickname);
                    }
                    else
                    {
                        if (!nicknameList.Contains(msg.ReceiverNickname))
                            nicknameList.Add(msg.ReceiverNickname);
                    }
                }

                FacebookAccount account;
                List<FacebookAccount> fbList = new List<FacebookAccount>();
                foreach (string item in nicknameList)
                {
                    account = await entities.FacebookAccounts.Where(e => e.Nickname == item).FirstOrDefaultAsync();
                    fbList.Add(account);
                }
                return Ok(fbList);
            }
        }
    }
}

using DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace CarWatch.Controllers
{
    public class MessageController : ApiController
    {
        private string k_TheServer = "TheServer";
        private string k_LicensePlateNotFound = "מספר הרישוי לא נמצא במערכת";
        private int k_AmountOfMessagesToUser = 30;
        private HttpClient client = new HttpClient();

        public MessageController()
        {
            client.BaseAddress = new Uri("https://carwatchapp.azurewebsites.net/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public class MessageDetails
        {
            public string Sender { get; set; }
            public string Receiver { get; set; }
        }

        public class PushMessage
        {
            public string LicensePlate { get; set; }
            public string Content { get; set; }
        }

        public class ConversationMessage
        {
            public DateTime Time { get; set; }
            public String Nickname { get; set; }
        }

        public class MessageComparer : IComparer<Message>
        {
            public int Compare(Message i_Message1, Message i_Message2)
            {
                if (i_Message1.Time > i_Message2.Time)
                    return 1;
                else if (i_Message1.Time < i_Message2.Time)
                    return -1;
                else
                    return 0;
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> SendByNickname([FromBody] Message i_Message)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.Sender != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                FacebookAccount account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname == i_Message.Receiver);
                if (account == null)
                {
                    return BadRequest("The receiver nickname was not found.");
                }
                i_Message.Time = DateTime.UtcNow;
                entities.Messages.Add(i_Message);
                if(account.ChatPartner != i_Message.Sender)
                {
                    TodoItem todoItem = new TodoItem();
                    todoItem.Text = i_Message.Sender + ";" + account.Nickname + ";sendChatMessage;" + i_Message.Content;
                    var response = await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
                }
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> SendByLicensePlate([FromBody] Message i_Message)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.Sender != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                TodoItem todoItem = new TodoItem();
                FacebookAccount account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.LicensePlate == i_Message.Receiver);
                if (account == null)
                {
                    todoItem.Text = k_TheServer + ";" + i_Message.Sender + ";sendToLicense;" + k_LicensePlateNotFound;
                    await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
                    return BadRequest(k_LicensePlateNotFound);
                }
                i_Message.Time = DateTime.UtcNow;
                i_Message.Receiver = account.Nickname;
                entities.Messages.Add(i_Message);
                todoItem.Text = i_Message.Sender + ";" + account.Nickname + ";sendToLicense;" + i_Message.Content;
                var response = await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetContacts([FromBody] MessageDetails i_Message)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.Sender != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                List<string> nicknameList = new List<string>();
                List<Message> messages = await entities.Messages.Where(e => (e.Sender == i_Message.Sender) || (e.Receiver == i_Message.Sender)).ToListAsync();
                foreach (Message msg in messages)
                {
                    if (i_Message.Sender != msg.Sender)
                    {
                        if (!nicknameList.Contains(msg.Sender))
                            nicknameList.Add(msg.Sender);
                    }
                    else
                    {
                        if (!nicknameList.Contains(msg.Receiver))
                            nicknameList.Add(msg.Receiver);
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

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetMessageHistoryByTime([FromBody] ConversationMessage i_ConversationMessage)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                List<Message> messages = await entities.Messages.Where(e => ((e.Sender == nickname && e.Receiver == i_ConversationMessage.Nickname) || (e.Sender == i_ConversationMessage.Nickname && e.Receiver == nickname)) && e.Time < i_ConversationMessage.Time).ToListAsync();
                int startingIndex, amountToRetrieve;
                if (messages.Count < k_AmountOfMessagesToUser)
                {
                    startingIndex = 0;
                    amountToRetrieve = messages.Count;
                }
                else
                {
                    startingIndex = messages.Count - k_AmountOfMessagesToUser;
                    amountToRetrieve = k_AmountOfMessagesToUser;
                }
                messages.Sort(new MessageComparer());
                List<Message> messagesToUser = messages.GetRange(startingIndex, amountToRetrieve);
                return Ok(messagesToUser);
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetUnreadMessages([FromBody] ConversationMessage i_ConversationMessage)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                List<Message> messages = await entities.Messages.Where(e => ((e.Sender == nickname && e.Receiver == i_ConversationMessage.Nickname) || (e.Sender == i_ConversationMessage.Nickname && e.Receiver == nickname)) && e.Time > i_ConversationMessage.Time).ToListAsync();
                messages.Sort(new MessageComparer());
                return Ok(messages);
            }
        }
    }
}

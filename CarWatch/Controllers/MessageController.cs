﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using DataAccess;
using System.Data.Entity;
using System.Threading;
using System.Net.Http.Headers;

namespace CarWatch.Controllers
{
    public class MessageController : ApiController
    {
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
                if(account == null)
                {
                    return BadRequest("The receiver nickname was not found.");
                }
                DateTime timeUtc = DateTime.UtcNow;
                TimeZoneInfo iLZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
                i_Message.Time = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, iLZone);
                entities.Messages.Add(i_Message);
                TodoItem todoItem = new TodoItem();
                todoItem.Text = i_Message.Sender + ";" + account.Nickname + ";send;" + i_Message.Content;
                var response = await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
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
                FacebookAccount account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.LicensePlate == i_Message.Receiver);
                if (account == null)
                {
                    return BadRequest("The receiver nickname was not found.");
                }
                DateTime timeUtc = DateTime.UtcNow;
                TimeZoneInfo iLZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
                i_Message.Time = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, iLZone);
                entities.Messages.Add(i_Message);
                TodoItem todoItem = new TodoItem();
                todoItem.Text = i_Message.Sender + ";" + account.Nickname + ";send;" + i_Message.Content;
                var response = await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
                await entities.SaveChangesAsync();
                return Ok();
            }
        }

        [BasicAuthentication]
        [HttpPost]
        public async Task<IHttpActionResult> GetMessagesHistory([FromBody] MessageDetails i_Message)
        {
            string nickname = Thread.CurrentPrincipal.Identity.Name;
            if (i_Message.Sender != nickname)
            {
                return BadRequest("Nicknames do not match.");
            }

            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                List<Message> result = await entities.Messages.Where(e => (e.Sender == i_Message.Sender && e.Receiver == i_Message.Receiver) || (e.Sender == i_Message.Receiver && e.Receiver == i_Message.Sender)).ToListAsync();
                return Ok(result);
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
    }
}

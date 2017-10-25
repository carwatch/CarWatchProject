using DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CarWatch
{
    public class MatchMaker
    {
        private string k_TheServer = "TheServer";
        private string k_ProposerMessage = "נמצא מחפש חניה!";
        private string k_SearcherMessage = "נמצאה חניה!";
        private int k_EarthRadius = 6371;
        private HttpClient client = new HttpClient();

        public MatchMaker()
        {
            client.BaseAddress = new Uri("https://carwatchapp.azurewebsites.net/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async void Match()
        {
            using (CarWatchDBEntities entities = new CarWatchDBEntities())
            {
                while (true)
                {
                    List<Proposal> proposalList = entities.Proposals.ToList();
                    foreach (Proposal proposal in proposalList)
                    {
                        List<Search> searchList = await entities.Searches.Where(
                                                        e => 2 * k_EarthRadius * (SqlFunctions.SquareRoot(SqlFunctions.Square(SqlFunctions.Sin((SqlFunctions.Radians(proposal.Latitude) - SqlFunctions.Radians(e.Latitude)) / 2)) + SqlFunctions.Cos(SqlFunctions.Radians(proposal.Latitude)) * SqlFunctions.Cos(SqlFunctions.Radians(e.Latitude)) * SqlFunctions.Square(SqlFunctions.Sin((SqlFunctions.Radians(proposal.Longitude) - SqlFunctions.Radians(e.Longitude)) / 2)))) <= e.Distance).ToListAsync();
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

                            Exchange exchange = createExchangeObject(searchToRemove, proposal);
                            entities.Exchanges.Add(exchange);
                            entities.Searches.Remove(searchToRemove);
                            entities.Proposals.Remove(proposal);
                            await entities.SaveChangesAsync();
                            sendPushNotifications(searchToRemove, proposal);
                        }
                    }
                }
            }
        }

        private Exchange createExchangeObject(Search i_Search, Proposal i_Proposal)
        {
            Exchange exchange = new Exchange();
            exchange.ProviderNickname = i_Proposal.Nickname;
            exchange.ConsumerNickname = i_Search.Nickname;
            exchange.ProviderLicensePlate = i_Proposal.LicensePlate;
            exchange.ConsumerLicensePlate = i_Search.LicensePlate;
            exchange.Location = i_Proposal.Location;
            exchange.Longitude = i_Proposal.Longitude;
            exchange.Latitude = i_Proposal.Latitude;
            exchange.Country = i_Proposal.Country;
            exchange.City = i_Proposal.City;
            exchange.Street = i_Proposal.Street;
            exchange.StreetNumber = i_Proposal.StreetNumber;
            exchange.TimeOpened = i_Search.TimeOpened;
            exchange.TimeMatched = DateTime.UtcNow;
            exchange.TimeExchanged = exchange.TimeMatched;
            exchange.Status = 0;
            exchange.DriverLongitude = i_Search.Longitude;
            exchange.DriverLatitude = i_Search.Latitude;
            return exchange;
        }

        private void sendPushNotifications(Search i_Search, Proposal i_Proposal)
        {
            sendToProposer(i_Proposal.Nickname, i_Search.LicensePlate);
            sendToSeacher(i_Search.Nickname, i_Proposal.LicensePlate, i_Proposal.Longitude, i_Proposal.Latitude);
        }

        private async void sendToProposer(string i_ProposerNickname, string i_SearcherLicensePlate)
        {

            TodoItem todoItem = new TodoItem();
            todoItem.Text = k_TheServer + ";" + i_ProposerNickname + ";foundSeeker;" + i_SearcherLicensePlate + ";" + k_ProposerMessage;
            await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
        }

        private async void sendToSeacher(string i_SeacherNickname, string i_ProposerLicensePlate, double i_Longitude, double i_Latitude)
        {
            TodoItem todoItem = new TodoItem();
            todoItem.Text = k_TheServer + ";" + i_SeacherNickname + ";foundParking;" + i_ProposerLicensePlate + ";" + i_Latitude.ToString() + ";" + i_Longitude.ToString() + ";" + k_SearcherMessage;
            await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
        }
    }

    public class TodoItem
    {
        public string Text { get; set; }
        public bool Complete { get; set; }
    }
}
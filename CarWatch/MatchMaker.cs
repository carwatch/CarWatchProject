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
        private string k_ParkingSpotTakenMessage = "חניה נתפסה בכתובת ";
        private string k_RankMessage = "והינך במיקום ה-";
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
                            List<ExtendedSearch> extendedSearchList = new List<ExtendedSearch>();
                            foreach(Search search in searchList)
                            {
                                FacebookAccount account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname.CompareTo(search.Nickname) == 0);
                                extendedSearchList.Add(new ExtendedSearch(search, account));
                            }
                            extendedSearchList.Sort(new ExtendedSearchComparer());
                            /*Search searchToRemove = searchList[0];
                            string firstAccountNickname = searchList[0].Nickname;
                            FacebookAccount parkingSpotMatch = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname.CompareTo(firstAccountNickname) == 0);
                            foreach (var item in searchList)
                            {
                                FacebookAccount account = await entities.FacebookAccounts.FirstOrDefaultAsync(e => e.Nickname.CompareTo(item.Nickname) == 0);
                                if (account.Rank > parkingSpotMatch.Rank)
                                {
                                    parkingSpotMatch = account;
                                    searchToRemove = item;
                                }
                            }*/
                            
                            Search searchToRemove = extendedSearchList[0].Search;
                            Exchange exchange = createExchangeObject(searchToRemove, proposal);
                            entities.Exchanges.Add(exchange);
                            entities.Searches.Remove(searchToRemove);
                            entities.Proposals.Remove(proposal);
                            await entities.SaveChangesAsync();
                            sendPushNotifications(searchToRemove, proposal);
                            alertOtherSearchers(extendedSearchList, proposal);
                        }
                    }
                }
            }
        }

        private async void alertOtherSearchers(List<ExtendedSearch> i_ExtendedSearchList, Proposal i_Proposal)
        {
            int index = 2;
            foreach(ExtendedSearch extendedSearch in i_ExtendedSearchList)
            {
                TodoItem todoItem = new TodoItem();
                todoItem.Text = k_TheServer + ";" + extendedSearch.Search.Nickname + ";systemMessage;" + k_ParkingSpotTakenMessage + i_Proposal.Location + k_RankMessage + index;
                await client.PostAsJsonAsync("tables/TodoItem/PostTodoItem?ZUMO-API-VERSION=2.0.0", todoItem);
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

        private double calculateScore(DateTime i_Time, double i_Rank)
        {
            int minutes = (DateTime.UtcNow - i_Time).Minutes;
            return (i_Rank * 0.9) + (minutes * 0.1);
        }
    }

    public class TodoItem
    {
        public string Text { get; set; }
        public bool Complete { get; set; }
    }

    public class ExtendedSearchComparer : IComparer<ExtendedSearch>
    {
        public int Compare(ExtendedSearch i_ExtendedSearch1, ExtendedSearch i_ExtendedSearch2)
        {
            double rank1 = (i_ExtendedSearch1.Account.Rank * 0.95) + ((DateTime.UtcNow - i_ExtendedSearch1.Search.TimeOpened).TotalMinutes * 0.05);
            double rank2 = (i_ExtendedSearch2.Account.Rank * 0.95) + ((DateTime.UtcNow - i_ExtendedSearch2.Search.TimeOpened).TotalMinutes * 0.05);
            if (rank1 > rank2)
                return 1;
            else if (rank1 < rank2)
                return -1;
            else
                return 0;
        }
    }

    public class ExtendedSearch
    {
        public Search Search { get; set; }
        public FacebookAccount Account { get; set; }

        public ExtendedSearch(Search i_Search, FacebookAccount i_Account)
        {
            Search = i_Search;
            Account = i_Account;
        }
    }
}
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MockStockBackend.DataModels;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using MockStockBackend.Services;

namespace MockStockBackend.Services
{
    public class LeagueService
    {
        private readonly ApplicationDbContext _context;
        private readonly StockService _stockService;
        
        public LeagueService(ApplicationDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public async Task<League> createLeague(int leagueHost, string leagueName, bool openEnrollment)
        {
            // Auto generate leagueID as a string, and return the entire league instead of a booleon.
            // O0IL and 1 have been removed for their confusing similarities.
            var chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            var leagueID = new char[8];
            var random = new Random();

            for (int i = 0; i < leagueID.Length; i++)
            {
                leagueID[i] = chars[random.Next(chars.Length)];
            }


            // Create New League using passed in information.
            League league = new League();

            league.LeagueId = new String(leagueID);
            league.LeagueHost = leagueHost;
            league.LeagueName = leagueName;
            league.LeagueCreationDate = DateTime.Now.ToString();
            league.OpenEnrollment = openEnrollment;

            // Add league to the entity model
            var dbResponse = _context.Leagues.Add(league);
            var addedLeague = dbResponse.Entity;

            // Add league to the database
            try
            {
                await _context.SaveChangesAsync();
                LeagueUser hosting  = await joinLeague(league.LeagueId, leagueHost);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                league = null;
            }
            
            return league;
        }

        public async Task<bool> modifyLeague(int leagueHost, string leagueID, bool openEnrollment)
        {
            bool response = false;

            // Find league using LeagueID and LeagueHost.  Host is to make sure the user has the privilege to actually make this modification.
            var result = (from League in _context.Leagues
                            where League.LeagueId == leagueID && League.LeagueHost == leagueHost
                            select new League
                            {
                                LeagueId = League.LeagueId,
                                LeagueName = League.LeagueName,
                                LeagueHost = League.LeagueHost,
                                LeagueCreationDate = League.LeagueCreationDate,
                                OpenEnrollment = openEnrollment
                            }).SingleOrDefault();

            // Assign new enrollment status.
            result.OpenEnrollment = openEnrollment;

            // Push changes onto Database.
            try
            {
                _context.Leagues.Update(result);
                await _context.SaveChangesAsync();
                response = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            
            return response;
        }

        public async Task<bool> deleteLeague(int leagueHost, string leagueID)
        {
            bool response = false;

            // Find league using LeagueID and LeagueHost.  Host is to make sure the user has the privilege to make the decision to delete.
            var result = (from League in _context.Leagues
                                where League.LeagueId == leagueID && League.LeagueHost == leagueHost
                                select new League
                                {
                                    LeagueId = League.LeagueId,
                                    LeagueName = League.LeagueName,
                                    LeagueHost = League.LeagueHost,
                                    LeagueCreationDate = League.LeagueCreationDate
                                }).SingleOrDefault();

            // If this succeeds, it needs to make an extra request at the same time that removes that leagueID from every leagueUser that contains it.
            var res =   from leagueUser in _context.LeagueUsers
                        where leagueUser.LeagueId == leagueID
                        select new LeagueUser
                        {
                            LeagueId = leagueUser.LeagueId,
                            UserId = leagueUser.UserId,
                        };

            var leagueUsers = await res.ToListAsync();

            // Attempt to remove the League as well as LeagueUsers at the same time, and then make a batch save changes to the database.
            try
            {
                _context.Leagues.Remove(result);
                foreach (LeagueUser user in leagueUsers)
                {
                    _context.LeagueUsers.Remove(user);
                }
                await _context.SaveChangesAsync();
                response = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return response;
        }

        public async Task<LeagueUser> joinLeague(string leagueID, int userID)
        {
            // Find league using its ID.
            var leagues =   (from League in _context.Leagues
                        where League.LeagueId == leagueID
                        select new League
                        {
                            LeagueId = League.LeagueId,
                            LeagueName = League.LeagueName,
                            LeagueHost = League.LeagueHost,
                            LeagueCreationDate = League.LeagueCreationDate
                        }).SingleOrDefault();


            if (leagues.LeagueId == leagueID) {
                // Create new LeagueUser Entry
                LeagueUser leagueUser = new LeagueUser();
                leagueUser.LeagueId = leagueID;
                leagueUser.UserId = userID;
                leagueUser.PrivilegeLevel = 0;

                // Add league to the entity model
                var dbResponse = _context.LeagueUsers.Add(leagueUser);
                var addedLeague = dbResponse.Entity;

                // Add LeagueUser to the database
                try
                {
                    await _context.SaveChangesAsync();
                    return leagueUser;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            
            return null;
        }

        public async Task<bool> leaveLeague(string leagueID, int userID)
        {
            bool response = false;

            // Find LeagueUser, and making sure it is not the Host who is attempting to leave.
            LeagueUser result = (from leagueUser in _context.LeagueUsers
                                join league in _context.Leagues
                                on leagueUser.LeagueId equals league.LeagueId
                                where leagueUser.LeagueId == leagueID && leagueUser.UserId == userID && league.LeagueHost != leagueUser.UserId
                                select new LeagueUser
                                {
                                    UserId = leagueUser.UserId,
                                    LeagueId = leagueUser.LeagueId
                                }).SingleOrDefault();

            // Remove the LeagueUser from the database if it were found.
            try
            {
                _context.LeagueUsers.Remove(result);
                await _context.SaveChangesAsync();
                response = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return response;
        }

        public async Task<bool> kickFromLeague(string leagueID, int userID, int kickedUserID)
        {
            bool response = false;

            // Find user to be removed, and makes sure that the user doing the removal is the LeagueHost.
            LeagueUser result = (from leagueUser in _context.LeagueUsers
                                join league in _context.Leagues
                                on leagueUser.LeagueId equals league.LeagueId
                                where leagueUser.LeagueId == leagueID && leagueUser.UserId == kickedUserID && league.LeagueHost == userID
                                select new LeagueUser
                                {
                                    UserId = leagueUser.UserId,
                                    LeagueId = leagueUser.LeagueId
                                }).SingleOrDefault();

            // Remove the LeagueUser from the database if it were found.
            try
            {
                _context.LeagueUsers.Remove(result);
                await _context.SaveChangesAsync();
                response = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return response;
        }

        public async Task<List<League>> GetLeaguesForUser(int userId)
        {
            // Obtain all Leagues taht user is a part of and return the list.
            var res =   from leagueUsers in _context.LeagueUsers
                        join league in _context.Leagues
                        on leagueUsers.LeagueId equals league.LeagueId
                        where leagueUsers.UserId == userId
                        select new League
                        {
                            LeagueId = league.LeagueId,
                            LeagueName = league.LeagueName,
                            LeagueHost = league.LeagueHost,
                            LeagueCreationDate = league.LeagueCreationDate
                        };
            var leagues = await res.ToListAsync();
            return leagues;
        }

        public async Task<List<User>> viewLeaderboard(string leagueID)
        {
            // Get userIDs from the LeagueID
            var res = from leagueUsers in _context.LeagueUsers
            join user in _context.Users on leagueUsers.UserId equals user.UserId
            where leagueUsers.LeagueId == leagueID && leagueUsers.UserId == user.UserId
            orderby user.UserCurrency
            select leagueUsers.UserId;

            var userIds = await res.ToArrayAsync();

            // Get user objects using the userIds
            var users = await _context.Users
                        .Where(x => userIds.Contains(x.UserId))
                        .Include(x => x.Stocks)
                        .ToListAsync();
            
            // Obtain a full list of unique stocks.
            var listOfUniqueStocks = new List<string>();
            foreach (var user in users)
            {
                var stocksForUser = user.Stocks;
                foreach (var stock in stocksForUser)
                {
                    var tickerSymbol = stock.StockId;
                    // Ensure it does not repeat the same stock twice in the List of Strings.
                    if (!listOfUniqueStocks.Contains(tickerSymbol))
                    {
                        listOfUniqueStocks.Add(tickerSymbol);
                    }
                }
            }

            // Prevent it from moving forward with 0 Unique Stocks.  Returns an empty List.
            List<User> leaderBoard = new List<User>();
            if (listOfUniqueStocks.Count == 0)
            {
                return leaderBoard;
            }

            // Use Stock Service and have it compute the current price of every stock.
            List<StockBatch> batch = await _stockService.FetchBatch(listOfUniqueStocks);

            // For every user; have it calculate their current available cash with the amount they hold in stocks.
            decimal funds;
            foreach (var user in users)
            {
                funds = user.UserCurrency;
                var stocksForUser = user.Stocks;
                foreach (var stock in stocksForUser)
                {
                    // Multiply the quantity owned of each stock with the price, and add it to a sum total.
                    decimal price = Convert.ToDecimal(batch.Find(x => x.symbol.Equals(stock.StockId.ToUpper())).price);
                    funds = funds + (stock.StockQuantity * price);
                }
                leaderBoard.Add(new User() {UserName = user.UserName, UserId = user.UserId, UserCurrency = funds});
            }

            // Sort the LeaderBoard from the best first, worst last and then return the List.
            List<User> sortedLeaderBoard = leaderBoard.OrderByDescending(x => x.UserCurrency).ToList();
            return sortedLeaderBoard;
        }
    }
}
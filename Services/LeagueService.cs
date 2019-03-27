using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MockStockBackend.DataModels;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MockStockBackend.Services
{
    public class LeagueService
    {
        private readonly ApplicationDbContext _context;
        public LeagueService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<League> createLeague(int leagueHost, string leagueName, bool openEnrollment)
        {
            // Auto generate leagueID as a string, and return the entire league instead of a booleon.
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ23456789";
            var leagueID = new char[8];
            var random = new Random();

            for (int i = 0; i < leagueID.Length; i++)
            {
                leagueID[i] = chars[random.Next(chars.Length)];
            }


            // Create New League
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

            //var result = _context.Leagues.Find(leagueID);


            result.OpenEnrollment = openEnrollment;

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

            var result = (from League in _context.Leagues
                                where League.LeagueId == leagueID && League.LeagueHost == leagueHost
                                select new League
                                {
                                    LeagueId = League.LeagueId,
                                    LeagueName = League.LeagueName,
                                    LeagueHost = League.LeagueHost,
                                    LeagueCreationDate = League.LeagueCreationDate
                                }).SingleOrDefault();

            //League result = _context.Leagues.First(i => i.LeagueId == leagueID);

            // If this succeeds, it needs to make an extra request at the same time that removes that leagueID from every leagueUser that contains it.

            var res =   from leagueUser in _context.LeagueUsers
                        where leagueUser.LeagueId == leagueID
                        select new LeagueUser
                        {
                            LeagueId = leagueUser.LeagueId,
                            UserId = leagueUser.UserId,
                        };

            var leagueUsers = await res.ToListAsync();

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
            var leagues =   (from League in _context.Leagues
                        where League.LeagueId == leagueID
                        select new League
                        {
                            LeagueId = League.LeagueId,
                            LeagueName = League.LeagueName,
                            LeagueHost = League.LeagueHost,
                            LeagueCreationDate = League.LeagueCreationDate
                        }).SingleOrDefault();


            //var leagues = await res.ToListAsync();

            if (leagues.LeagueId == leagueID) {
                // Create New League
                LeagueUser leagueUser = new LeagueUser();
                leagueUser.LeagueId = leagueID;
                leagueUser.UserId = userID;
                leagueUser.PrivilegeLevel = 0;

                // Add league to the entity model
                var dbResponse = _context.LeagueUsers.Add(leagueUser);
                var addedLeague = dbResponse.Entity;

                // Add league to the database
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

            LeagueUser result = (from leagueUser in _context.LeagueUsers
                                join league in _context.Leagues
                                on leagueUser.LeagueId equals league.LeagueId
                                where leagueUser.LeagueId == leagueID && leagueUser.UserId == userID && league.LeagueHost != leagueUser.UserId
                                select new LeagueUser
                                {
                                    UserId = leagueUser.UserId,
                                    LeagueId = leagueUser.LeagueId
                                }).SingleOrDefault();

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
    }
}
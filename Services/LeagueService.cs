using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MockStockBackend.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MockStockBackend.Services
{
    public class LeagueService
    {
        private readonly ApplicationDbContext _context;
        public LeagueService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<League>> GetLeaguesForUser(int userId)
        {
            var res =   from leagueuser in _context.LeagueUsers
                        join league in _context.Leagues
                        on leagueuser.LeagueId equals league.LeagueId
                        where leagueuser.UserId == userId
                        select new League
                        {
                            LeagueId = league.LeagueId,
                            OpenEnrollment = league.OpenEnrollment,
                            LeagueUsers = league.LeagueUsers
                        };
            var leagues = await res.ToListAsync();
            return leagues;
        }

        public async Task<List<User>> GetUsersForLeague(string leagueId)
        {
            // Get user ids from the league
            var res =   from leagueuser in _context.LeagueUsers
                        where leagueuser.LeagueId == leagueId
                        select leagueuser.UserId;
            var userIds = await res.ToArrayAsync();

            // Get user objects using the userIds
            var users = await _context.Users
                        .Where(x => userIds.Contains(x.UserId))
                        .Include(x => x.Stocks)
                        .ToListAsync();
            
            return users;
        }

    }
}
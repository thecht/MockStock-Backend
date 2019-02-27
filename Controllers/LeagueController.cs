using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockStockBackend.Services;

namespace MockStockBackend.Controllers
{
    [Authorize]
    [Route("api/leagues")]
    [ApiController]
    public class LeagueController: ControllerBase
    {
        private readonly LeagueService _leagueService;
        public LeagueController(LeagueService leagueService)
        {
            _leagueService = leagueService;
        }

        [HttpGet]
        public async Task<string> GetLeaguesForUser()
        {
            var userId = (int)HttpContext.Items["userId"];
            var leagues = await _leagueService.GetLeaguesForUser(userId);
            return Newtonsoft.Json.JsonConvert.SerializeObject(leagues);
        }

        [HttpGet("{leagueId}")]
        public async Task<string> GetUsersForLeague(string leagueId)
        {
            var users = await _leagueService.GetUsersForLeague(leagueId);
            return Newtonsoft.Json.JsonConvert.SerializeObject(users);
        }

    }
}
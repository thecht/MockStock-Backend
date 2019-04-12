using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockStockBackend.DataModels;
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

        [HttpPost("createLeague")]
        public async Task<string> createLeague()
        {
            // Obtain LeagueHost, LeagueName, and set Open Enrollment to default.
            var leagueHost = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            var leagueName = (string)HttpContext.Request.Headers["leagueName"];
            var openEnrollment = false;
        
            // Returns League Object.
            League result = await _leagueService.createLeague(leagueHost, leagueName, openEnrollment);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpPost("modifyLeague")]
        public async Task<string> modifyLeague()
        {
            // Obtain LeagueHost, LeagueID, and new Open Enrollment type.
            var leagueHost = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            var leagueID = (string)HttpContext.Request.Headers["leagueID"];
            var openEnrollment = Convert.ToBoolean(HttpContext.Request.Headers["openEnrollment"]);

            // Return Boolean showing success or failure.
            bool result = await _leagueService.modifyLeague(leagueHost, leagueID, openEnrollment);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpDelete("deleteLeague")]
        public async Task<string> deleteLeague()
        {
            // Obtain LeagueHost and LeagueID.
            var leagueHost = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            var leagueID = (string)HttpContext.Request.Headers["leagueID"];

            // Return boolean showing success or failure.
            bool result = await _leagueService.deleteLeague(leagueHost, leagueID);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpPost("join/{leagueId}")]
        public async Task<string> joinLeague(string leagueID)
        {
            // Obtain LeagueID and UserID
            var userID = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);

            // Return LeagueUser Object.
            LeagueUser result = await _leagueService.joinLeague(leagueID, userID);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpDelete("leave/{leagueId}")]
        public async Task<string> leaveLeague(string leagueID)
        {
            // Obtain LeagueID and UserID
            var userID = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);

            // Return boolean showing success or failure.
            bool result = await _leagueService.leaveLeague(leagueID, userID);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpDelete("kick/{leagueId}/{kickedUserID}")]
        public async Task<string> kickFromLeague(string leagueID, int kickedUserID)
        {
            // Obtain LeagueID and two UserIDs.  One is meant to be the Host, while the other is the one to be kicked.
            var userID = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);

            // Return boolean showing success or failure.
            bool result = await _leagueService.kickFromLeague(leagueID, userID, kickedUserID);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpGet]
        public async Task<string> GetLeaguesForUser()
        {
            // Obtain UserID.
            var userID = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);

            // Returns a list of League Objects.
            var leagues = await _leagueService.GetLeaguesForUser(userID);
            return Newtonsoft.Json.JsonConvert.SerializeObject(leagues);
        }

        [HttpGet("leaderboard/{leagueId}")]
        //[HttpGet]
        public async Task<string> viewLeaderboard(string leagueID)
        {
            // Retrieve userID from token, and check to see if they belong to league before showing leaderboard.
            var users = await _leagueService.viewLeaderboard(leagueID);

            // Returns a list of User Objects.
            return Newtonsoft.Json.JsonConvert.SerializeObject(users);
        }
    }
}
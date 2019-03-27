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
            var leagueHost = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            var leagueName = (string)HttpContext.Request.Headers["leagueName"];
            var openEnrollment = false;
        
            League result = await _leagueService.createLeague(leagueHost, leagueName, openEnrollment);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpPost("modifyLeague")]
        public async Task<string> modifyLeague()
        {
            var leagueHost = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            var leagueID = (string)HttpContext.Request.Headers["leagueID"];
            var openEnrollment = Convert.ToBoolean(HttpContext.Request.Headers["openEnrollment"]);
            bool result = await _leagueService.modifyLeague(leagueHost, leagueID, openEnrollment);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpDelete("deleteLeague")]
        public async Task<string> deleteLeague()
        {
            var leagueHost = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            var leagueID = (string)HttpContext.Request.Headers["leagueID"];
            bool result = await _leagueService.deleteLeague(leagueHost, leagueID);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpPost("join/{leagueId}")]
        public async Task<string> joinLeague(string leagueID)
        {
            var userID = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            LeagueUser result = await _leagueService.joinLeague(leagueID, userID);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        [HttpDelete("leave/{leagueId}")]
        public async Task<string> leaveLeague(string leagueID)
        {
            var userID = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            bool result = await _leagueService.leaveLeague(leagueID, userID);
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockStockBackend.DataModels;
using MockStockBackend.Services;

namespace MockStockBackend.Controllers
{
    [Authorize]
    [Route("api/portfolio")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly PortfolioService _portfolioService;
        public PortfolioController(PortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        [HttpGet]
        public async Task<string> retrievePortfolio()
        {
            // Obtain UserID.
            var userId = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);

            // Returns a combination of the user's available currency and a list of all of their stocks.
            var portfolio = await _portfolioService.retrievePortfolio(userId);
            return Newtonsoft.Json.JsonConvert.SerializeObject(portfolio);
        }
    }
}
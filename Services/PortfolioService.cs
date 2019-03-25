using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MockStockBackend.DataModels;
using MockStockBackend.Helpers;
using System.Collections.Generic;
using MockStockBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace MockStockBackend.Services
{
    public class PortfolioService
    {
        private readonly ApplicationDbContext _context;
        private readonly AppSettings _appSettings;
        public PortfolioService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }

        //public async Task<(decimal, List<Stock>)> retrievePortfolio(int userId)
        public async Task<Object> retrievePortfolio(int userId)
        {
            // Get user ids from the league
            var res = from Stock in _context.Stocks
                        where Stock.UserId == userId
                        select new Stock
                        {
                            StockId = Stock.StockId,
                            UserId = Stock.UserId,
                            StockQuantity = Stock.StockQuantity
                        };


            var stockInfo = await res.ToListAsync();


            decimal userCurrency = (from User in _context.Users
                               where User.UserId == userId
                               select User.UserCurrency).SingleOrDefault();
            

            var retVal = new {
                UserCurrency = userCurrency,
                Stock = stockInfo
            };




            //return (userCurrency, stockInfo);
            return retVal;
        }
    }
}
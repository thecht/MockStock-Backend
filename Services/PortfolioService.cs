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
using Microsoft.EntityFrameworkCore;
using System.Dynamic;
using Newtonsoft.Json.Linq;

namespace MockStockBackend.Services
{
    public class PortfolioService
    {
        private readonly ApplicationDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly StockService _stockService;

        public PortfolioService(ApplicationDbContext context, IOptions<AppSettings> appSettings, StockService stockService)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _stockService = stockService;
        }

        // Created to facilitate a new return object.
        public class StockInfoPrice
        {
            public string StockId { get; set; }
            public int UserId { get; set; }
            public int StockQuantity { get; set; }
            public decimal StockPrice { get; set; }
            public decimal ChangePercent { get; set; }
        }

        public async Task<Object> retrievePortfolio(int userId)
        {
            // Get user's stocks.
            var res = from Stock in _context.Stocks
                        where Stock.UserId == userId
                        select new Stock
                        {
                            StockId = Stock.StockId,
                            UserId = Stock.UserId,
                            StockQuantity = Stock.StockQuantity
                        };

            // Create new list for unique Stock IDs.
            var listOfUniqueStocks = new List<string>();
            foreach (var Stock in res)
            {
                var tickerSymbol = Stock.StockId;
                listOfUniqueStocks.Add(tickerSymbol);
            }

            // Create new StockInfoPrice array, to match the values in Res with those from the Batch result.
            StockInfoPrice[] stockInfoPrice = new StockInfoPrice[listOfUniqueStocks.Count];

            // Prevent it from moving forward with 0 Unique Stocks.  Returns an empty Array.
            if (listOfUniqueStocks.Count == 0)
            {
                decimal currency = (from User in _context.Users
                                    where User.UserId == userId
                                    select User.UserCurrency).SingleOrDefault();
                var ret = new {
                    UserCurrency = currency,
                    Stock = stockInfoPrice
                };
                return ret;
            }

            // Send Stock IDs to stockService for a price check.
            List<StockBatch> batch = await _stockService.FetchBatch(listOfUniqueStocks);
            
            int i = 0;

            // Put together information from Res and Batch to form an array of a new object.
            foreach (var Stock in res)
            {
                stockInfoPrice[i] = new StockInfoPrice();
                stockInfoPrice[i].StockId = Stock.StockId;
                stockInfoPrice[i].UserId = Stock.UserId;
                stockInfoPrice[i].StockQuantity = Stock.StockQuantity;
                decimal price = Convert.ToDecimal(batch.Find(x => x.symbol.Equals(Stock.StockId.ToUpper())).price);
                stockInfoPrice[i].StockPrice = price;
                decimal change = Convert.ToDecimal(batch.Find(x => x.symbol.Equals(Stock.StockId.ToUpper())).changePercent);
                stockInfoPrice[i].ChangePercent = change;
                i++;
            }

            // Retrieve the User's available currency.
            decimal userCurrency = (from User in _context.Users
                               where User.UserId == userId
                               select User.UserCurrency).SingleOrDefault();
            
            // Package the userCurrency with the new Stock Object, for a single return.
            var retVal = new {
                UserCurrency = userCurrency,
                Stock = stockInfoPrice
            };

            return retVal;
        }
    }
}
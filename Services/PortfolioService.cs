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
using System.Net.Http;
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
            // Get user stocks.
            var res = from Stock in _context.Stocks
                        where Stock.UserId == userId
                        select new Stock
                        {
                            StockId = Stock.StockId,
                            UserId = Stock.UserId,
                            StockQuantity = Stock.StockQuantity
                        };

            // Convert results to List.
            //var stockInfo = await res.ToListAsync();

            // Create new list for unique Stock IDs.
            var listOfUniqueStocks = new List<string>();
            foreach (var Stock in res)
            {
                var tickerSymbol = Stock.StockId;
                listOfUniqueStocks.Add(tickerSymbol);
                Trace.WriteLine(tickerSymbol);
            }

            foreach (var item in listOfUniqueStocks)
            {
                Trace.WriteLine(item);
            }
            // Send Stock IDs to stockService for a price check.
            List<StockBatch> batch = await _stockService.FetchBatch(listOfUniqueStocks);

            // Create new StockInfoPrice array, to match the values in Res with those from the Batch result.
            StockInfoPrice[] stockInfoPrice = new StockInfoPrice[listOfUniqueStocks.Count];
            int i = 0;

            foreach (var Stock in res)
            {
                stockInfoPrice[i] = new StockInfoPrice();
                stockInfoPrice[i].StockId = Stock.StockId;
                stockInfoPrice[i].UserId = Stock.UserId;
                stockInfoPrice[i].StockQuantity = Stock.StockQuantity;
                decimal price = Convert.ToDecimal(batch.Find(x => x.symbol.Contains(Stock.StockId.ToUpper())).price);
                stockInfoPrice[i].StockPrice = price;
                decimal change = Convert.ToDecimal(batch.Find(x => x.symbol.Contains(Stock.StockId.ToUpper())).changePercent);
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

        public async Task<Object> TestPort(List<string> symbols)
        {
            var tickerSymbols = new List<string>();
            tickerSymbols.Add("msft");
            tickerSymbols.Add("googl");
            tickerSymbols.Add("air");
            var res = await _stockService.FetchBatch(tickerSymbols);
            return res;
        }

        public async Task<Object> TestPortfolioRequest(int userId)
        {
            // Get user stocks.
            var stocks = await (from Stock in _context.Stocks
                                where Stock.UserId == userId
                                select new Stock
                                {
                                    StockId = Stock.StockId,
                                    UserId = Stock.UserId,
                                    StockQuantity = Stock.StockQuantity
                                }).ToListAsync();
            
            var tickerSymbols = new List<string>();
            foreach (var stock in stocks)
            {
                tickerSymbols.Add(stock.StockId.ToUpper());
            }
            
            foreach (var s in tickerSymbols)
            {
                Console.WriteLine(s);
            }

            var tickerSymbols2 = new List<string>();
            tickerSymbols2.Add("msft");
            tickerSymbols2.Add("googl");
            tickerSymbols2.Add("air");

            List<StockBatch> batch = new List<StockBatch>();
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.iextrading.com/1.0/");

            var symbols = tickerSymbols2;
            var response = await httpClient.GetStringAsync("stock/market/batch?symbols=" + string.Join(",", symbols) + "&types=price,previous");
            var list = JObject.Parse(response);
            for(int i = 0; i < symbols.Count(); i++){
                StockBatch x = new StockBatch();
                x.symbol = symbols.ElementAt(i);
                x.price = (decimal)list[x.symbol]["price"];
                x.changePercent = (decimal)list[x.symbol]["previous"]["changePercent"];
                batch.Add(x);
            }
            
            
            //var stockServiceResponse = await _stockService.FetchBatch(tickerSymbols);

            return batch;
        }
    }
}
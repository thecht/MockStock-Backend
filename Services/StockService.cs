using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MockStockBackend.DataModels;
using MockStockBackend.Helpers;
using Newtonsoft.Json.Linq;

namespace MockStockBackend.Services
{
    public class StockService
    {
        private readonly ApplicationDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly HttpClient httpClient;

        public StockService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.iextrading.com/1.0/");
        }

        public async Task<string> PriceQuery(string symbol)
        {
            //Query the IEX API for the price using an HTTP GET request
            try{
            string responseBody = await httpClient.GetStringAsync("stock/" + symbol + "/price");
            return responseBody;
            }catch(Exception){
                return null;
            }
        }

        public async Task<Transaction> GenerateTransaction(string symbol, string amount, string price, int userId, string type){
            // enforce capital stocks
            symbol = symbol.ToUpper();
            
            //Create the new transaction
            Transaction transaction = new Transaction();
            transaction.UserId = userId;
            transaction.StockId = symbol;
            transaction.StockQty = Convert.ToInt32(amount);
            transaction.StockPrice = Convert.ToDecimal(price);
            transaction.TransactionDate = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));
            transaction.TransactionType = type;

            //Find the user and deduct from or add to their currency
            var user = _context.Users.Find(userId);
            if(type == "buy"){
                user.UserCurrency -= (Convert.ToDecimal(amount) * Convert.ToDecimal(price));
                //Check to see if stock is in the database already for the user
                //If so then update the amount instead of creating a new entry
                var result = _context.Stocks.Find(symbol, userId);

                //Buying Stock
                if(result == null){
                    //Generate the new stock entry
                    Stock stock = new Stock();
                    stock.UserId = userId;
                    stock.StockId = symbol;
                    stock.StockQuantity = Convert.ToInt32(amount);

                    _context.Stocks.Add(stock);
                }
                else{
                //Update stock entry found
                result.StockQuantity += Convert.ToInt32(amount);
                }
            }
            //Selling Stcok
            else{
                user.UserCurrency += (Convert.ToDecimal(amount) * Convert.ToDecimal(price));
                //Check for how much of the stock the user owns
                var result = _context.Stocks.Find(symbol, userId);
                //Make sure user cannot sell more than they own
                if(result.StockQuantity < Convert.ToInt32(amount)){
                    return null;
                }

                //Update stock entry found
                result.StockQuantity -= Convert.ToInt32(amount);
                //If amount drops to zero, drop it from the user's inventory
                if(result.StockQuantity == 0){
                    _context.Remove(result);
                }
            }

            //Add transaction to the entity model
            var dbResponse = _context.Transactions.Add(transaction);
            var addedTransaction = dbResponse.Entity;

            //Add transaction to the database
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                addedTransaction = null;
            }

            return addedTransaction;
        }

        /*
        Data Fetching
        */


        public async Task<DetailedStock> FetchDetails(String symbol){
            //Query the API for key details about a certain stock
            try{
            string price = await PriceQuery(symbol);

            var responseBody = await httpClient.GetStringAsync("stock/" + symbol + "/stats");
            var results = JObject.Parse(responseBody);

            DetailedStock stock = new DetailedStock();

            stock.symbol = symbol;
            stock.price = Decimal.Parse(price);
            stock.changePercent = (Decimal) results["day5ChangePercent"];
            stock.ytdChange = (Decimal) results["ytdChangePercent"];
            stock.high = (Decimal) results["week52high"];
            stock.low = (Decimal) results["week52low"];

            return stock;
            }catch(Exception){
                return null;
            }
        }

        public async Task<MarketBatch> FetchMarket(String sort){
            MarketBatch marketBatch = new MarketBatch();
            int calls = 1;
            //Get the entire list of stock symbols and only grab the first 100

            var referenceData = await httpClient.GetStringAsync("ref-data/symbols");
            JArray results = JArray.Parse(referenceData);
            List<String> symbols = new List<String>();
            //Sort method
            if(sort == "asc"){
                for(int i = 0; i < 500; i++){
                    //Plus Symbols cause errors in URL's
                    string str = (string) results.ElementAt(i)["symbol"];
                    if(str.Contains("+"))
                        continue;
                    symbols.Add(str);
                }
            }
            else{
                int index = 0;
                int start = results.Count()-1;
                //Skip crypto at the end
                while((string) results.ElementAt(start)["type"] == "crypto")
                    start--;
                for(int i = start; i > (start-500); i--){
                    //Plus Symbols cause errors in URL's
                    string str = (string) results.ElementAt(i)["symbol"];
                    if(str.Contains("+"))
                        continue;
                    symbols.Add(str);
                    index++;
                }
            }

            //Split up symbols to separate API calls
            calls += symbols.Count()/100;
            for(int i = 0; i < calls; i++){
                int max = 100;
                //Limit the max if it's the final call since it may be below 100
                if(i+1 == calls){
                    max = symbols.Count() % 100;
                }
                List<String> callSymbols = symbols.GetRange(i*100, max);
                var responseBody = await httpClient.GetStringAsync("stock/market/batch?symbols=" + string.Join(",", callSymbols) + 
                    "&types=price,logo,previous");
                var market = JObject.Parse(responseBody);
                //Get only the data needed for each stock
                foreach(string symbol in callSymbols)
                {
                    MarketStock x = new MarketStock();
                    x.symbol = symbol;
                    x.logo = (string) market[x.symbol]["logo"]["url"];
                    //Some stocks may have null price or changePercent
                    if((string) market[x.symbol]["price"] == null || (string)market[x.symbol]["previous"]["changePercent"] == null){
                        continue;
                    }
                    else{
                        x.price = (decimal) market[x.symbol]["price"];
                        x.changePercent = (decimal) market[x.symbol]["previous"]["changePercent"];
                    }
                    marketBatch.stocks.Add(x);
                }
            }

            //Get today's biggest gainers and losers
            var response = await httpClient.GetStringAsync("stock/market/list/gainers");
            var gainers = JArray.Parse(response);
            foreach(JToken stock in gainers)
            {
                MarketStock x = new MarketStock();
                x.symbol = (string) stock["symbol"];
                x.price = (decimal) stock["latestPrice"];
                x.changePercent = (decimal) stock["changePercent"];
                marketBatch.gainers.Add(x);
            }
            response = await httpClient.GetStringAsync("stock/market/list/losers");
            var losers = JArray.Parse(response);
            foreach(JToken stock in losers)
            {
                MarketStock x = new MarketStock();
                x.symbol = (string) stock["symbol"];
                x.price = (decimal) stock["latestPrice"];
                x.changePercent = (decimal) stock["changePercent"];
                marketBatch.losers.Add(x);
            }
            
            //Get the logo for gainers and losers
            List<string> list = new List<string>();
            foreach(MarketStock stock in marketBatch.gainers){
                list.Add(stock.symbol);
            }
            foreach(MarketStock stock in marketBatch.losers){
                list.Add(stock.symbol);
            }
            response = await httpClient.GetStringAsync("stock/market/batch?symbols=" + string.Join(",", list) + 
                "&types=logo");
            var logos = JObject.Parse(response);
            for(int i = 0; i < gainers.Count(); i++){
                marketBatch.gainers.ElementAt(i).logo = (string) logos[list.ElementAt(i)]["logo"]["url"];
            }
            for(int i = 0; i < losers.Count(); i++){
                marketBatch.losers.ElementAt(i).logo = (string) logos[list.ElementAt(i+gainers.Count())]["logo"]["url"];
            }

            return marketBatch;
        }

        public async Task<List<MarketStock>> SearchMarket(String search){
            List<string> symbols = new List<string>();
            int resultsCount = 0;
            int calls = 1;
            List<MarketStock> results = new List<MarketStock>();
            //Check for a blank search, return null if so
            if(search == "")
                return null;

            //Get the entire list of stock symbols and only grab the first 100 that match
            var referenceData = await httpClient.GetStringAsync("ref-data/symbols");
            JArray refer = JArray.Parse(referenceData);
            //Capitalize search string
            search = search.ToUpper();

            //Iterate through the list searching for stocks that start with the search string
            //Skip anything with a plus symbol, it causes issues with batch requests
            foreach(JObject stock in refer){
                string str = (string) stock["symbol"];
                if(str.Contains("+") || (string)stock["type"] == "crypto"){
                    continue;
                }
                if(str.StartsWith(search)){
                    symbols.Add(str);
                    resultsCount++;
                }
            }
            //If no results, then return nothing
            if(symbols.Count() == 0)
                return null;
            //If results are more than 100, split up the API calls
            calls += (resultsCount-1)/100;
            //Get the information needed and trim it for the front end for each call
            for(int i = 0; i < calls; i++){
                int max = 100;
                //If it's the last call, limit the max to the end of the list
                if(i+1 == calls){
                    max = symbols.Count()%100;
                }
                List<string> callSymbols = symbols.GetRange(i*100, max);
                var responseBody = await httpClient.GetStringAsync("stock/market/batch?symbols=" + string.Join(",", callSymbols) + 
                    "&types=price,logo,previous");
                var market = JObject.Parse(responseBody);
                //Add each stock to the results list
                for(int j = 0; j < max; j++){
                    MarketStock stock = new MarketStock();
                    stock.symbol = symbols.ElementAt(i*100+j);
                    //Error check for a missing price or changePercent
                    if((string) market[stock.symbol]["price"] == null || (string) market[stock.symbol]["previous"]["changePercent"] == null){
                        continue;
                    }
                    stock.price = (decimal) market[stock.symbol]["price"];
                    stock.logo = (string) market[stock.symbol]["logo"]["url"];
                    stock.changePercent = (decimal) market[stock.symbol]["previous"]["changePercent"];
                    results.Add(stock);
                }
            }
            return results;
        }

        public async Task<List<ChartPoint>> FetchChart(String symbol, String range){
            try{
                var responseBody = await httpClient.GetStringAsync("stock/" + symbol + "/chart/" + range);
                var points = JArray.Parse(responseBody);
                List<ChartPoint> chart = new List<ChartPoint>();
                foreach(JToken point in points)
                {
                    ChartPoint x = new ChartPoint();
                    x.date = (string) point["date"];
                    x.closingPrice = (decimal) point["close"];
                    chart.Add(x);
                }
                return chart;
            }
            catch(Exception e){
                Debug.WriteLine(e);
                return null;
            }
        }

        public async Task<List<StockBatch>> FetchBatch(List<string> symbols){
            //Error checking for an empty list
            if(symbols.Count() == 0){
                return null;
            }
            //Capitalize every symbol
            symbols = symbols.ConvertAll(symbol => symbol.ToUpper());
            
            //Check if the batch request is over 100
            //split it up into chunks if it is
            List<StockBatch> batch = new List<StockBatch>();

            if(symbols.Count() > 100){
                int requests = (symbols.Count() / 100) + 1;
                //For every 100 symbols, do a separate API request
                for(int i = 1; i <= requests; i++){
                    //Check if it's the last request of the batch
                    //If so then a limit must be placed on the "GetRange" function
                    int max;
                    if(i != requests){
                        max=100;
                    }else{
                        max=(symbols.Count() - (i*100-100));
                    }
                    string responseBody = await httpClient.GetStringAsync("stock/market/batch?symbols=" + String.Join(",", symbols.GetRange(i*100-100, max)) +
                            "&types=price,previous");
                    //Get only the data needed to return
                    var tempList = JObject.Parse(responseBody);
                    for(int j = 0; j < max; j++){
                        StockBatch x = new StockBatch();
                        x.symbol = symbols.ElementAt(j + (i*100 - 100));
                        //Price and changePercent error checking
                        if((string)tempList[x.symbol]["price"] == null || (string)tempList[x.symbol]["previous"]["changePercent"] == null)
                            continue;
                        x.price = (decimal)tempList[x.symbol]["price"];
                        x.changePercent = (decimal)tempList[x.symbol]["previous"]["changePercent"];
                        batch.Add(x);
                    }
                }
                return batch;
            }

            //When request is 100 or less stocks
            var response = await httpClient.GetStringAsync("stock/market/batch?symbols=" + string.Join(",", symbols) + "&types=price,previous");
            //Get only the required data fields and return a list of that
            var list = JObject.Parse(response);
            for(int i = 0; i < symbols.Count(); i++){
                StockBatch x = new StockBatch();
                x.symbol = symbols.ElementAt(i);
                //Price and changePercent error checking
                if((string)list[x.symbol]["price"] == null || (string)list[x.symbol]["previous"]["changePercent"] == null)
                    continue;
                x.price = (decimal)list[x.symbol]["price"];
                x.changePercent = (decimal)list[x.symbol]["previous"]["changePercent"];
                batch.Add(x);
            }

            return batch;
        }
    }
}
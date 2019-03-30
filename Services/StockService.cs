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

        public StockService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }

        public async Task<string> PriceQuery(string symbol, HttpClient httpClient)
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


        public async Task<String> FetchDetails(String symbol, HttpClient httpClient){
            //Query the API for key details about a certain stock
            try{
            string price = await PriceQuery(symbol, httpClient);

            var responseBody = await httpClient.GetStringAsync("stock/" + symbol + "/stats");
            var results = JObject.Parse(responseBody);

            string fiveDayChange = (string) results["day5ChangePercent"];
            string high = (string) results["week52high"];
            string low = (string) results["week52low"];
            string yearChange = (string) results["ytdChangePercent"];

            var details = new Dictionary<string, string>();
                details.Add("price", price);
                details.Add("day5ChangePercent", fiveDayChange);
                details.Add("week52high", high);
                details.Add("week52low", low);
                details.Add("ytdChangePercent", yearChange);
            string batch = Newtonsoft.Json.JsonConvert.SerializeObject(details);
            return batch;

            }catch(Exception){
                return null;
            }
        }

        public async Task<String> FetchMarket(HttpClient httpClient){
            //Get the entire list of stock symbols and only grab the first 100
            var referenceData = await httpClient.GetStringAsync("ref-data/symbols");
            JArray results = JArray.Parse(referenceData);
            string[] symbols = new string[100];
            for(int i = 0; i < 100; i++){
                symbols[i] = (string) results.ElementAt(i)["symbol"];
            }
            var info = await httpClient.GetStringAsync("stock/market/batch?symbols=" + string.Join(",", symbols) + 
                "&types=price,logo,previous");

            //Get only the changePercent from "previous"
            var list = JObject.Parse(info);
            string batch = "{\"market\":{\"";
            for(int i = 0; i < symbols.Count(); i++){
                if(i != 0){
                    batch += ",\"";
                }
                batch += symbols[i] + "\":{\"logo\":\"" + (string) list[symbols.ElementAt(i)]["logo"]["url"] +
                    "\",\"price\":" + (string) list[symbols.ElementAt(i)]["price"] + 
                    ",\"changePercent\":" + (string) list[symbols.ElementAt(i)]["previous"]["changePercent"] + "}";
            }
            batch += "},";

            //Get today's biggest gainers and losers
            var gainers = await httpClient.GetStringAsync("stock/market/list/gainers");
            batch += "\"gainers\":" + gainers + ",";
            var losers = await httpClient.GetStringAsync("stock/market/list/losers");
            batch += "\"losers\":" + losers;

            return batch += "}";
        }

        public async Task<String> FetchChart(String symbol, String range, HttpClient httpClient){
            try{
                var responseBody = await httpClient.GetStringAsync("stock/" + symbol + "/chart/" + range);
                return responseBody;
            }
            catch(Exception e){
                Debug.WriteLine(e);
                return null;
            }
        }

        public async Task<String> FetchBatch(String stocks, HttpClient httpClient){
            //Check if the batch request is over 100
            //split it up into chunks if it is
            string batch = "{\"";
            List<string> symbols = stocks.Split(",").ToList();

            if(symbols.Count() > 100){
                int lists = (symbols.Count() / 100) + 1;
                //For every 100 symbols, do a separate API request
                for(int i = 1; i <= lists; i++){
                    //Check if it's the last iteration of the batch
                    //If so then a limit must be placed on the "GetRange" function
                    int max;
                    if(i != lists){
                        max=100;
                    }else{
                        max=(symbols.Count() - (i*100-100));
                    }
                    string responseBody = await httpClient.GetStringAsync("stock/market/batch?symbols=" + String.Join(",", symbols.GetRange(i*100-100, max)) +
                            "&types=price,previous");
                    //Get only changePercent from "previous" and return that with stock symbol and price
                    var tempList = JObject.Parse(responseBody);
                    for(int j = 0; j < max; j++){
                        if(j != 0 || i != 1){
                            batch += ",\"";
                        }
                        batch += symbols.ElementAt(j+(i*100-100)) + "\":{\"price\":" + (string)tempList[symbols.ElementAt(j+(i*100-100))]["price"] + ",\"changePercent\":" + 
                        (string)tempList[symbols.ElementAt(j+(i*100-100))]["previous"]["changePercent"] + "}";
                    }                 
                }
                batch += "}";
                return batch; 
            }

            //When request is 100 or less stocks
            var results = await httpClient.GetStringAsync("stock/market/batch?symbols=" + stocks + "&types=price,previous");
            //Get only changePercent from "previous" data field and return that with stock symbol and price
            var list = JObject.Parse(results);
            for(int i = 0; i < symbols.Count(); i++){
                if(i != 0){
                    batch += ",\"";
                }
                batch += symbols.ElementAt(i) + "\":{\"price\":" + (string)list[symbols.ElementAt(i)]["price"] + ",\"changePercent\":" + 
                (string)list[symbols.ElementAt(i)]["previous"]["changePercent"] + "}";
            }

            batch += "}";
            return batch;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockStockBackend.DataModels;
using MockStockBackend.Services;
using Newtonsoft.Json.Linq;

namespace MockStockBackend.Controllers
{
    [Authorize]
    [Route("api/stock")]
    [ApiController]

    public class StockController: ControllerBase
    {
        private readonly StockService _stockService;
        private readonly HttpClient httpClient;
        public StockController(StockService stockService)
        {
            _stockService = stockService;
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.iextrading.com/1.0/");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<string> GetPrice()
        {
            //Search for the stock price with the symbol given
            var symbol = (string)HttpContext.Request.Headers["symbol"];
            string price = await _stockService.PriceQuery(symbol, httpClient);
            if(price == null){
                return "{\"error\": \"incorrect stock symbol\"}";
            }
            return price;
        }

        [HttpPost("buy")]
        public async Task<string> BuyStock()
        {
            //Authenticate the user
            var userId = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            //Get the stock symbol and amount to purchase
            var symbol = (string)HttpContext.Request.Headers["symbol"];
            var amount = (string)HttpContext.Request.Headers["amount"];
            var price = await _stockService.PriceQuery(symbol, httpClient);
            //Make sure stock symbol exists
            if(price == null){
                return "{\"error\": \"incorrect stock symbol\"}";
            }
            //Make sure amount is numeric
            int value;
            if(!int.TryParse(amount, out value)){
                return "{\"error\": \"amount is not numeric\"}";
            }
            //Make sure amount is more than 0
            else if(Convert.ToInt32(amount) <= 0){
                return "{\"error\": \"insufficient amount\"}";
            }
            
            //Generate the transaction details
            Transaction newTransaction = await _stockService.GenerateTransaction(symbol, amount, price, userId, "buy");

            //Send information to the client
            HttpContext.Response.StatusCode = 201;
            return Newtonsoft.Json.JsonConvert.SerializeObject(newTransaction);
        }

        [HttpPost("sell")]
        public async Task<string> SellStock()
        {
            //Verify the user
            var userId = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            //Get stock symbol and amount to sell
            var symbol = (string)HttpContext.Request.Headers["symbol"];
            var amount = (string)HttpContext.Request.Headers["amount"];
            var price = await _stockService.PriceQuery(symbol, httpClient);
            //Check for valid symbol
            if(price == null){
                return "{\"error\": \"incorrect stock symbol\"}";
            }
            int value;
            //Check for numeric amount
            if(!int.TryParse(amount, out value)){
                return "{\"error\": \"amount is not numeric\"}";
            }
            //Cehck for non-negative amount
            else if(Convert.ToInt32(amount) <= 0){
                return "{\"error\": \"insufficient amount\"}";
            }
            
            //Generate transaction
            Transaction newTransaction = await _stockService.GenerateTransaction(symbol, amount, price, userId, "sell");

            //Send Information to the client
            return Newtonsoft.Json.JsonConvert.SerializeObject(newTransaction);
        }

        //Data Fetching
        
        [AllowAnonymous]
        [HttpGet("details")]
        public async Task<string> getDetails(){
            string symbol = (string)HttpContext.Request.Headers["symbol"];

            var details = await _stockService.FetchDetails(symbol, httpClient);

            if(details == null){
                return "Please enter a valid ticker symbol.";
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(details);
        }

        [AllowAnonymous]
        [HttpGet("marketplace")]
        public async Task<String> getMarket(){

            //Fetch the batch data needed for the marketplace
            var market = await _stockService.FetchMarket(httpClient);
            
            return Newtonsoft.Json.JsonConvert.SerializeObject(market);
        }

        [AllowAnonymous]
        [HttpGet("chart")]
        public async Task<String> getChart(){
            string symbol = (string)HttpContext.Request.Headers["symbol"];
            string range = (string)HttpContext.Request.Headers["range"];

            var chart = await _stockService.FetchChart(symbol, range, httpClient);

            return Newtonsoft.Json.JsonConvert.SerializeObject(chart);
        }

        [AllowAnonymous]
        [HttpGet("Batch")]
        public async Task<String> getBatch([FromBody]SymbolContainer symbols){
            //Get the list of symbols needed for the batch request
            var listOfSymbols = symbols.Symbols;

            //Capitalize every symbol
            listOfSymbols = listOfSymbols.ConvertAll(symbol => symbol.ToUpper());

            //Return the price and percent change of each symbol
            var batch = await _stockService.FetchBatch(listOfSymbols, httpClient);

            return Newtonsoft.Json.JsonConvert.SerializeObject(batch);
        }
    }

}
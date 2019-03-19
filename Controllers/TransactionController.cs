using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockStockBackend.DataModels;
using MockStockBackend.Services;

namespace MockStockBackend.Controllers
{
    [Authorize]
    [Route("api/transaction")]
    [ApiController]

    public class TransactionController: ControllerBase
    {
        private readonly TransactionService _transactionService;
        private readonly HttpClient httpClient;
        public TransactionController(TransactionService transactionService)
        {
            _transactionService = transactionService;
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.iextrading.com/1.0/");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<string> GetPrice()
        {
            //Search for the stock price with the symbol given
            var symbol = (string)HttpContext.Request.Headers["symbol"];
            string price = await _transactionService.PriceQuery(symbol, httpClient);
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
            var price = await _transactionService.PriceQuery(symbol, httpClient);
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
            Transaction newTransaction = await _transactionService.GenerateTransaction(symbol, amount, price, userId, "buy");

            //Add or update stock object in database
            Stock stock = await _transactionService.AddStock(symbol, amount, userId);

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
            var price = await _transactionService.PriceQuery(symbol, httpClient);
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
            Transaction newTransaction = await _transactionService.GenerateTransaction(symbol, amount, price, userId, "sell");

            //Update stock in database
            Stock stock = await _transactionService.SubtractStock(symbol, amount, userId);

            //Send Information to the client
            HttpContext.Response.StatusCode = 201;
            return Newtonsoft.Json.JsonConvert.SerializeObject(newTransaction);
        }
    }

}
using System;
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

namespace MockStockBackend.Services
{
    public class TransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly AppSettings _appSettings;

        public TransactionService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
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

        public async Task<Transaction> GenerateTransaction(string symbol, string amount, string price, int userID, string type){
            //Create the new transaction
            Transaction transaction = new Transaction();
            transaction.UserId = userID;
            transaction.StockId = symbol;
            transaction.StockQty = Convert.ToInt32(amount);
            transaction.StockPrice = Convert.ToDecimal(price);
            transaction.TransactionDate = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));
            transaction.TransactionType = type;

            //Find the user and deduct from or add to their currency
            var user = _context.Users.Find(userID);
            if(type == "buy"){
                user.UserCurrency -= (Convert.ToDecimal(amount) * Convert.ToDecimal(price));
                await _context.SaveChangesAsync();
            }
            else{
                user.UserCurrency += (Convert.ToDecimal(amount) * Convert.ToDecimal(price));
                await _context.SaveChangesAsync();
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

        public async Task<Stock> AddStock(String symbol, String amount, int userId){
            //Check to see if stock is in the database already for the user
            //If so then update the amount instead of creating a new entry
            var result = _context.Stocks.Find(symbol, userId);

            if(result == null){
                //Generate the new stock entry
                Stock stock = new Stock();
                stock.UserId = userId;
                stock.StockId = symbol;
                stock.StockQuantity = Convert.ToInt32(amount);

                var dbResponse = _context.Stocks.Add(stock);
                var addedStock = dbResponse.Entity;

                //Add the stock to the database
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    addedStock = null;
                }

                return addedStock;
            }

            //Update stock entry found
            result.StockQuantity += Convert.ToInt32(amount);
            await _context.SaveChangesAsync();

            return result;
        }

        public async Task<Stock> SubtractStock(String symbol, String amount, int userId){
            //Check for how much of the stock the user owns
            //frontend will make sure user does not enter more stocks than they own
            var result = _context.Stocks.Find(symbol, userId);

            //Update stock entry found
            result.StockQuantity -= Convert.ToInt32(amount);
            //If amount drops to zero, drop it from the user's inventory
            if(result.StockQuantity == 0){
                var dbResponse = _context.Remove(result);
                var removedStock = dbResponse.Entity;
                await _context.SaveChangesAsync();
                return removedStock;
            }
            await _context.SaveChangesAsync();

            return result;
        }
    }

}
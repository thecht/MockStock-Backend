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

        public async Task<Transaction> GenerateTransaction(string symbol, string amount, string price){
            //Create the new transaction
            Transaction transaction = new Transaction();
            transaction.UserId = 1;
            transaction.StockId = symbol;
            transaction.StockQty = Convert.ToInt32(amount);
            transaction.StockPrice = Convert.ToDecimal(price);
            transaction.TransactionDate = 0;
            transaction.TransactionType = "buy";

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
    }

}
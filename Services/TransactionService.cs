using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
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

        public List<Transaction> UserHistory(int userId)
        {
            try
            {
                var result = _context.Transactions.Where(transaction => transaction.UserId == userId).ToList();
                return result;
            }
            //If user has no transaction history, return nothing
            catch(Exception e){
                Debug.WriteLine(e);
                return null;
            }
        }
    }
}
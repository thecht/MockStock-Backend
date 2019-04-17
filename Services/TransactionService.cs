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

namespace MockStockBackend.Services
{
    public class TransactionService
    {
        private readonly ApplicationDbContext _context;
        public TransactionService(ApplicationDbContext context)
        {
            _context = context;
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
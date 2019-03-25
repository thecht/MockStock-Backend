using System;
using System.Collections.Generic;
using System.Linq;
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
        
        public TransactionController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public string GetHistory()
        {
            //Authenticate the user
            var userId = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.Name).Value);
            //Query the database for every transaction from the User ID
            var history = _transactionService.UserHistory(userId);
            if(history.Count == 0){
                return "There is no stock history.";
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(history);
        }
    }


}
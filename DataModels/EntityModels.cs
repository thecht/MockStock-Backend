

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockStockBackend.DataModels
{
    public class Stock
    {
        // Column Attributes
        public string StockId { get; set; }
        public int UserId { get; set; }
        public int StockQuantity { get; set; }
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; }
    }

    public class User
    {
        // Column Attributes
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public decimal UserCurrency { get; set; }

        // FK Collections
        public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
        public ICollection<LeagueUser> LeagueUsers { get; set; } = new List<LeagueUser>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class League
    {
        // Column Attributes
        public string LeagueId { get; set; }
        public bool OpenEnrollment { get; set; }

        // FK Collections
        public ICollection<LeagueUser> LeagueUsers { get; set; } = new List<LeagueUser>();
    }

    public class LeagueUser
    {
        // Column Attributes
        public int UserId { get; set; }
        public string LeagueId { get; set; }
        public bool Owner { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; }
        [ForeignKey("LeagueId")]
        public League League { get; set; }
    }

    public class Transaction
    {
        // Column Attributes
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string StockId { get; set; }
        public int StockQty { get; set; }
        public decimal StockPrice { get; set; }
        public int TransactionDate { get; set; }
        public string TransactionType { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; }
    }

    
}
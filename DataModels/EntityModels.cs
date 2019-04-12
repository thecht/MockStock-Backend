using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

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
        [JsonIgnore]
        public User User { get; set; }
    }

    public class User
    {
        // Column Attributes
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public decimal UserCurrency { get; set; }
        public string Token { get; set; }

        // FK Collections
        public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
        public ICollection<LeagueUser> LeagueUsers { get; set; } = new List<LeagueUser>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class League
    {
        // Column Attributes
        public string LeagueId { get; set; }
        public int LeagueHost { get; set; }
        public string LeagueName { get; set; }
        public string LeagueCreationDate { get; set; }
        public bool OpenEnrollment { get; set; }

        // FK Collections
        [JsonIgnore]
        public ICollection<LeagueUser> LeagueUsers { get; set; } = new List<LeagueUser>();
    }

    public class LeagueUser
    {
        // Column Attributes
        public int UserId { get; set; }
        public string LeagueId { get; set; }
        public int PrivilegeLevel { get; set; }

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
        [JsonIgnore]
        public User User { get; set; }
    }

    public class SymbolContainer
    {
        public List<string> Symbols { get; set;}
    }
    
    public class StockBatch
    {
        public string symbol { get; set; }
        public decimal price { get; set; }
        public decimal changePercent { get; set; }
    }

    public class DetailedStock
    {
        public string symbol { get; set; }
        public decimal price { get; set; }
        public decimal changePercent { get; set; }
        public decimal ytdChange { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }
    }

    public class MarketStock
    {
        public string symbol { get; set; }
        public string logo { get; set; }
        public decimal price { get; set; }
        public decimal changePercent {get; set; }
    }

    public class MarketBatch
    {
        public ICollection<MarketStock> stocks { get; set; } = new List<MarketStock>();
        public ICollection<MarketStock> gainers { get; set; } = new List<MarketStock>();
        public ICollection<MarketStock> losers { get; set; } = new List<MarketStock>();
    }

    public class ChartPoint
    {
        public string date { get; set; }
        public decimal closingPrice { get; set; }
    }
}
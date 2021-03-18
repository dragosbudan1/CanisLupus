using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using MongoDB.Driver;
using NLog;

namespace CanisLupus.Worker.Trader
{
    public interface ITradingClient
    {
        Task<List<Trade>> FindActiveTrades(string symbol);
        Task<Trade> CreateActiveTrade(Trade trade);
        Task<Trade> CloseTrade(string tradeId, Order trade);
        Task<Trade> UpdateTrade(string tradeId, Order order);
    }
    public class TradingClient : ITradingClient
    {
        public static string TradesCollectionName = "Trades";
        private readonly ILogger logger;
        private readonly IDbClient dbClient;

        public TradingClient(IDbClient dbClient)
        {
            this.dbClient = dbClient;
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public async Task<Trade> CloseTrade(string tradeId, Order order)
        {
            var collection = dbClient.GetCollection<Trade>(TradesCollectionName);
            Expression<Func<Trade, bool>> filter = m => (m.Id == tradeId);

            var update = Builders<Trade>.Update
                .Set(m => m.UpdatedDate, DateTime.Now)
                .Set(m => m.TradeStatus, TradeStatus.Finished)
                .Set(m => m.CloseSpend, order.SpendAmount)
                .Set(m => m.CloseOrderId, order.Id);

            var cancelledOrder = await collection.FindOneAndUpdateAsync<Trade>(filter, update);

            return cancelledOrder;
        }

        public async Task<Trade> CreateActiveTrade(Trade trade)
        {
            trade.CreatedDate = DateTime.UtcNow;
            trade.TradeStatus = TradeStatus.Active;
            var result = await dbClient.InsertAsync(trade, TradesCollectionName);
            return result;
        }

        public async Task<List<Trade>> FindActiveTrades(string symbol)
        {
            var tradesCollection = dbClient.GetCollection<Trade>(TradesCollectionName);
            Expression<Func<Trade, bool>> filter = m => (m.TradeStatus == TradeStatus.Active && m.Symbol == symbol);
            var openOrders = (await tradesCollection.FindAsync<Trade>(filter)).ToList();

            return openOrders;
        }

        public async Task<Trade> UpdateTrade(string tradeId, Order order)
        {
            var collection = dbClient.GetCollection<Trade>(TradesCollectionName);
            Expression<Func<Trade, bool>> filter = m => (m.Id == tradeId);

            var update = Builders<Trade>.Update
                .Set(m => m.UpdatedDate, DateTime.Now)
                .Set(m => m.TradeStatus, TradeStatus.Finished)
                .Set(m => m.CloseSpend, order.SpendAmount)
                .Set(m => m.CloseOrderId, order.Id);

            var cancelledOrder = await collection.FindOneAndUpdateAsync<Trade>(filter, update);

            return cancelledOrder;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Exchange;
using MongoDB.Driver;
using NLog;

namespace CanisLupus.Worker.Trader
{
    public interface IOrderClient
    {
        Task<List<Order>> FindOpenOrders(string symbol);
        Task<Order> CreateAsync(Order order);
        Task<bool> CancelAsync(string orderId);
        Task<Order> FindByIdAsync(string orderId);
        Task<Order> UpdateAsync(Order order);
    }
    public class OrderClient : IOrderClient
    {
        private readonly IDbClient dbClient;
        private readonly ILogger logger;
        public const string OrdersCollectionName = "Orders";
        private readonly IBinanceClient binanceClient;

        public OrderClient(IDbClient dbClient, IBinanceClient binanceClient)
        {
            this.binanceClient = binanceClient;
            this.dbClient = dbClient;
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public async Task<bool> CancelAsync(string orderId)
        {
            try
            {
                var ordersCollection = dbClient.GetCollection<Order>(OrdersCollectionName);
                Expression<Func<Order, bool>> filter = m => (m.Id == orderId);
                var existingOrder = (await ordersCollection.FindAsync<Order>(filter)).FirstOrDefault();

                if(existingOrder == null)
                {
                    logger.Warn($"Cannot cancel order id {orderId}: Order not found");
                }

                var binanceResponse = await binanceClient.CancelOrder(existingOrder.Symbol, existingOrder.Id);

                if(binanceResponse == null)
                {
                    logger.Warn($"Cannot cancel order id {orderId}: Binance failed. Order details {existingOrder}");
                }

                var update = Builders<Order>.Update
                    .Set(m => m.UpdatedDate, DateTime.Now)
                    .Set(m => m.Status, BinanceHelpers.MapToOrderStatus(binanceResponse?.Status));

                var updateResult = await ordersCollection.UpdateOneAsync<Order>(filter, update);

                return updateResult?.ModifiedCount > 0;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, $"Error cancelling order {orderId}: {ex.Message}");
                return false;
            }
        }

        public async Task<Order> CreateAsync(Order order)
        {
            try
            {
                var binanceOrder = await binanceClient.CreateOrder(new BinanceOrderRequest
                {
                    ClientOrderId = order.Id,
                    Price = order.Price,
                    Quantity = order.Quantity,
                    Side = order.Side,
                    Symbol = order.Symbol
                });

                if (binanceOrder == null)
                {
                    logger.Error($"Error creating binance order for details {order}");
                }

                var confirmedOrder = binanceOrder.MapToOrder(order);

                var ordersCollection = dbClient.GetCollection<Order>(OrdersCollectionName);
                await ordersCollection.InsertOneAsync(confirmedOrder);
                return confirmedOrder;
            }
            catch (SystemException ex)
            {
                logger.Error(ex, $"Error inserting new order: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Order>> FindOpenOrders(string symbol)
        {
            var ordersCollection = dbClient.GetCollection<Order>(OrdersCollectionName);
            Expression<Func<Order, bool>> filter = m => ((m.Status == OrderStatus.New || m.Status == OrderStatus.Partial) && m.Symbol == symbol);
            var openOrders = (await ordersCollection.FindAsync<Order>(filter)).ToList();

            List<Order> returnOpenOrders = new List<Order>();
            foreach(var openOrder in openOrders)
            {
                var binanceOrder = await binanceClient.GetOrder(openOrder.Symbol, openOrder.Id);
                var mappedOrder = binanceOrder.MapToOrder(openOrder);
                if(mappedOrder.Status != openOrder.Status)
                {
                    var updatedOrder = await UpdateAsync(mappedOrder);
                } 
                else
                {
                    returnOpenOrders.Add(mappedOrder);
                }
            }

            return returnOpenOrders;
        }

        public async Task<Order> FindByIdAsync(string orderId)
        {
            var ordersCollection = dbClient.GetCollection<Order>(OrdersCollectionName);
            Expression<Func<Order, bool>> filter = m => (m.Id == orderId);
            var openOrders = (await ordersCollection.FindAsync<Order>(filter)).FirstOrDefault();

            return openOrders;
        }

        public async Task<Order> UpdateAsync(Order order)
        {
            var collection = dbClient.GetCollection<Order>(OrdersCollectionName);
            Expression<Func<Order, bool>> filter = m => (m.Id == order.Id);

            var update = Builders<Order>.Update
                .Set(m => m.Status, order.Status)
                .Set(m => m.UpdatedDate, DateTime.Now);

            var updatedIntersection = await collection.FindOneAndUpdateAsync<Order>(filter, update);
            
            return order;
        }
    }
}
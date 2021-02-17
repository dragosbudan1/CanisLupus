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
    public interface IOrderClient
    {
        Task<List<Order>> FindOpenOrders();
        Task<string> UpdateOrderAsync(string orderId, OrderStatus newStatus);
        Task<Order> CreateOrder(Order order);
        Task<Order> FindOrderById(string orderId);
    }
    public class OrderClient : IOrderClient
    {
        private readonly IDbClient dbClient;
        private readonly ILogger logger;
        public const string OrdersCollectionName = "Orders";

        public OrderClient(IDbClient dbClient)
        {
            this.dbClient = dbClient;
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public async Task<string> UpdateOrderAsync(string orderId, OrderStatus newStatus)
        {
            try
            {
                var collection = dbClient.GetCollection<Order>(OrdersCollectionName);
                Expression<Func<Order, bool>> filter = m => (m.Id == orderId);

                var update = Builders<Order>.Update
                    .Set(m => m.UpdatedDate, DateTime.Now)
                    .Set(m => m.Status, newStatus);

                var updateResult = await collection.UpdateOneAsync<Order>(filter, update);

                if(updateResult?.ModifiedCount > 0)
                {
                    return orderId;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, $"Error cancelling order {orderId}: {ex.Message}");
                return null;
            }
        }

        public async Task<Order> CreateOrder(Order order)
        {
            try
            {
                //create in binance

                var ordersCollection = dbClient.GetCollection<Order>(OrdersCollectionName);
                order.CreatedDate = DateTime.UtcNow;
                order.Status = OrderStatus.New;
                await ordersCollection.InsertOneAsync(order);
                return order;
            }
            catch (SystemException ex)
            {
                logger.Error(ex, $"Error inserting new order: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Order>> FindOpenOrders()
        {
            var ordersCollection = dbClient.GetCollection<Order>(OrdersCollectionName);
            Expression<Func<Order, bool>> filter = m => (m.Status == OrderStatus.New || m.Status == OrderStatus.Partial);
            var openOrders = (await ordersCollection.FindAsync<Order>(filter)).ToList();

            return openOrders;
        }

        public async Task<Order> FindOrderById(string orderId)
        {
            var ordersCollection = dbClient.GetCollection<Order>(OrdersCollectionName);
            Expression<Func<Order, bool>> filter = m => (m.Id == orderId);
            var openOrders = (await ordersCollection.FindAsync<Order>(filter)).FirstOrDefault();

            return openOrders;
        }
    }
}
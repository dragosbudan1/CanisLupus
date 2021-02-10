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
        Task<Order> CancelOrder(Order order);
        Task<Order> CreateOrder(Order order);
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

        public async Task<Order> CancelOrder(Order order)
        {
            if(order.Status != OrderStatus.Open)
            {
                logger.Error($"Order {order.Id} {order.Status.ToString()} can't be cancelled");
                return null;
            }

            var collection = dbClient.GetCollection<Order>(OrdersCollectionName);
            Expression<Func<Order, bool>> filter = m => (m.Id == order.Id);

            var update = Builders<Order>.Update
                .Set(m => m.UpdatedDate, DateTime.Now)
                .Set(m => m.Status, OrderStatus.Cancelled);

            var cancelledOrder = await collection.FindOneAndUpdateAsync<Order>(filter, update);

            logger.Info($"Order {order.Id} {order.Status.ToString()} was cancelled");
            
            return cancelledOrder;
        }

        public async Task<Order> CreateOrder(Order order)
        {
            order.CreatedDate = DateTime.UtcNow;
            order.Status = OrderStatus.Open;
            var result = await dbClient.InsertAsync(order, OrdersCollectionName);
            return result;
        }

        public async Task<List<Order>> FindOpenOrders()
        {
            var ordersCollection = dbClient.GetCollection<Order>(OrdersCollectionName);
            Expression<Func<Order, bool>> filter = m => (m.Status == OrderStatus.Open || m.Status == OrderStatus.Partial);
            var openOrders = (await ordersCollection.FindAsync<Order>(filter)).ToList();

            return openOrders;
        }
    }
}
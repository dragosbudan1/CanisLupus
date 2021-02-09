using System.Collections.Generic;
using System.Threading.Tasks;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Extensions;
using Microsoft.Extensions.Logging;

namespace CanisLupus.Worker.Trader
{
    public interface ITradingClient
    {
        Task<OrderResult> CreateBuyOrder(decimal spend, decimal price);
        Task<OrderResult> CreateSellOrder(decimal amount, decimal price);
    }
    public class TradingClient : ITradingClient
    {
        public static List<Order> OpenOrders = new List<Order>();
        public static List<Order> SellOrders = new List<Order>();
        public static Wallet Wallet = new Wallet() { Amount = 1000.0m };

        private readonly ILogger<TradingClient> logger;

        public TradingClient(ILogger<TradingClient> logger)
        {
            this.logger = logger;
        }

        public async Task<OrderResult> CreateBuyOrder(decimal price, decimal spend)
        {
            var order = new Order
            {
                Type = OrderType.Buy,
                Amount = spend / price,
                Spend = spend,
                Price = price
            };

            OpenOrders.Add(order);
            Wallet.Amount -= spend;

            var message = $"Created order: {order.ToLoggable()}";

            return await Task.FromResult(new OrderResult { Success = true });
        }

        public async Task<OrderResult> CreateSellOrder(decimal amount, decimal price)
        {
            var order = new Order
            {
                Type = OrderType.Sell,
                Amount = amount,
                Spend = amount * price,
                Price = price
            };

            OpenOrders.RemoveAt(0);
            SellOrders.Add(order);
            Wallet.Amount += order.Spend;

            var message = $"Created order: {order.ToLoggable()}";

            return await Task.FromResult(new OrderResult { Success = true });
        }
    }
}
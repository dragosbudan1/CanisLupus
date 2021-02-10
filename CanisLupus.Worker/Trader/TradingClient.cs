using System.Collections.Generic;
using System.Threading.Tasks;
using CanisLupus.Common.Models;
using NLog;

namespace CanisLupus.Worker.Trader
{
    public interface ITradingClient
    {
        Task<List<Trade>> FindActiveTrades();
    }
    public class TradingClient : ITradingClient
    {
        private readonly ILogger logger;

        public TradingClient()
        {
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public Task<List<Trade>> FindActiveTrades()
        {
            logger.Error("Not Implemented");
            return Task.FromResult(new List<Trade>());
        }
    }
}
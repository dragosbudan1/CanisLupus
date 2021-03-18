using System.Threading.Tasks;
using CanisLupus.Common;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Account;
using Newtonsoft.Json;
using NLog;

namespace CanisLupus.Worker.Events
{
    public interface IInsertTradingSettingsRpcServer : IEventRpcServerBase { }
    public class InsertTradingSettingsRpcServer : EventRpcServerBase, IInsertTradingSettingsRpcServer
    {
        private readonly ITradingSettingsService tradingSettingsService;

        public InsertTradingSettingsRpcServer(ITradingSettingsService tradingSettingsService)
        {
            this.tradingSettingsService = tradingSettingsService;
            base.Logger = LogManager.GetCurrentClassLogger();
            base.QueueName = EventConst.InsertTradingSettingsQueueName;
        }

        protected override async Task<string> ProcessMessage(string message)
        {
            var result = await tradingSettingsService.InsertOrUpdateAsync(new TradingSettings()
            {
                Symbol = message
            });
            return JsonConvert.SerializeObject(result);
        }
    }
}
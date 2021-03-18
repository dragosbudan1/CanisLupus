using System.Threading.Tasks;
using CanisLupus.Common;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Account;
using Newtonsoft.Json;
using NLog;

namespace CanisLupus.Worker.Events
{
    public interface IUpdateTradingSettingsRpcServer : IEventRpcServerBase { }
    public class UpdateTradingSettingsRpcServer : EventRpcServerBase, IUpdateTradingSettingsRpcServer
    {
        private readonly ITradingSettingsService tradingSettingsService;

        public UpdateTradingSettingsRpcServer(ITradingSettingsService tradingSettingsService)
        {
            this.tradingSettingsService = tradingSettingsService;
            base.Logger = LogManager.GetCurrentClassLogger();
            base.QueueName = EventConst.UpdateTradingSettingsQueueName;
        }

        protected override async Task<string> ProcessMessage(string message)
        {
            var settings = JsonConvert.DeserializeObject<TradingSettings>(message);
            Logger.Info($"{message} {settings.ProfitPercentage}");
            var result = await tradingSettingsService.InsertOrUpdateAsync(settings);
            return JsonConvert.SerializeObject(result);
        }
    }
}
using System.Threading.Tasks;
using CanisLupus.Common;
using CanisLupus.Worker.Account;
using Newtonsoft.Json;
using NLog;

namespace CanisLupus.Worker.Events
{
    public interface IGetTradingSettingsRpcServer : IEventRpcServerBase { }
    public class GetTradingSettingsRpcServer : EventRpcServerBase, IGetTradingSettingsRpcServer
    {
        private readonly ITradingSettingsService tradingSettingsService;

        public GetTradingSettingsRpcServer(ITradingSettingsService tradingSettingsService)
        {
            this.tradingSettingsService = tradingSettingsService;
            base.Logger = LogManager.GetCurrentClassLogger();
            base.QueueName = EventConst.GetTradingSettingsQueueName;
        }

        protected override async Task<string> ProcessMessage(string message)
        {
            var result = await tradingSettingsService.GetAllAsync();
            return JsonConvert.SerializeObject(result);
        }
    }
}
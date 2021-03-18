using System.Threading.Tasks;
using CanisLupus.Common;
using CanisLupus.Worker.Account;
using Newtonsoft.Json;
using NLog;

namespace CanisLupus.Worker.Events
{
    public interface IDeleteTradingSettingsRpcServer : IEventRpcServerBase { }
    public class DeleteTradingSettingsRpcServer : EventRpcServerBase, IDeleteTradingSettingsRpcServer
    {
        private readonly ITradingSettingsService tradingSettingsService;

        public DeleteTradingSettingsRpcServer(ITradingSettingsService tradingSettingsService)
        {
            this.tradingSettingsService = tradingSettingsService;
            base.Logger = LogManager.GetCurrentClassLogger();
            base.QueueName = EventConst.DeleteTradingSettingsQueueName;
        }

        protected override async Task<string> ProcessMessage(string message)
        {
            var result = await tradingSettingsService.DeleteAsync(message);
            return JsonConvert.SerializeObject(result);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CanisLupus.Common;
using CanisLupus.Common.Models;
using CanisLupus.Web.Events;
using CanisLupus.Web.Models;
using CanisLupus.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLog;

namespace CanisLupus.Web.Controllers
{
    public class SymbolsPostRequest
    {
        public string Symbol { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public partial class SymbolsController : ControllerBase
    {

        private readonly ILogger logger;

        public SymbolsController()
        {
            this.logger = LogManager.GetCurrentClassLogger();
        }

        [HttpPost]
        public async Task<string> Post(SymbolsPostRequest req)
        {
            logger.Info("Symbol: {0}", req.Symbol);

            var result = await new RpcService().Call(EventConst.InsertTradingSettingsQueueName, req.Symbol);

            return result;
        }

        [HttpGet("all")]
        public async Task<List<TradingSettings>> GetAll()
        {
            logger.Info("Get All trading settings");

            var result = await new RpcService().Call(EventConst.GetTradingSettingsQueueName);

            var tradingSettings = JsonConvert.DeserializeObject<List<TradingSettings>>(result);

            return tradingSettings;
        }

        [HttpDelete]
        public async Task<bool> Delete(string symbol)
        {
            if(string.IsNullOrEmpty(symbol))
            {
                return false;
            }

            logger.Info($"Delete {symbol} trading settings");

            var result = await new RpcService().Call(EventConst.DeleteTradingSettingsQueueName, symbol);

            return result != null;
        }

        [HttpPut]
        public async Task<TradingSettings> Update(TradingSettings settings)
        {
            logger.Info($"Update {settings.Symbol} {settings.TradingStatus} trading settings");

            var result = await new RpcService().Call(EventConst.UpdateTradingSettingsQueueName, JsonConvert.SerializeObject(settings));

            return JsonConvert.DeserializeObject<TradingSettings>(result);
        }
    }
}

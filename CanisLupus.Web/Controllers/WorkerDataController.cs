using System.Collections.Generic;
using System.Threading.Tasks;
using CanisLupus.Common.Models;
using CanisLupus.Web.Events;
using CanisLupus.Web.Models;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace CanisLupus.Web.Controllers
{
    public class SessionInitRequest
    {
        public string Id { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public partial class WorkerDataController : ControllerBase
    {

        private readonly ILogger logger;
        private readonly IEventReceiver eventReceiver;

        public WorkerDataController(IEventReceiver eventReceiver)
        {
            this.eventReceiver = eventReceiver;
            this.logger = LogManager.GetCurrentClassLogger();
        }

        [HttpPost]
        public Task<bool> Post(SessionInitRequest req)
        {
            logger.Info("SessionId: {0}", req.Id);
            //QueuePool.CreateQueue(req.Id)
            return Task.FromResult(true);
        }

        [HttpGet]
        public async Task<object> Get()
        {
            var task1 = eventReceiver.ReceiveAsync<List<WorkerData>>("candleData");
            var task4 = eventReceiver.ReceiveAsync<List<Common.Models.Vector2>>("wmaData");
            var task5 = eventReceiver.ReceiveAsync<List<Common.Models.Vector2>>("smmaData");
            var task6 = eventReceiver.ReceiveAsync<List<string>>("tradingLogs");
            var task7 = eventReceiver.ReceiveAsync<TradingInfo>("tradingInfo");

            Task.WaitAll(task1, task4, task5, task6);

            var candleData = await Task.FromResult(task1.Result);
            var wmaData = await Task.FromResult(task4.Result);
            var smmaData = await Task.FromResult(task5.Result);
            var tradingLogsData = await Task.FromResult(task6.Result);
            var tradingInfoData = await Task.FromResult(task7.Result);

            TryMergeMovingAverageData(candleData, wmaData, smmaData);
            logger.Info("returning data");
            return new
            {
                candleData = candleData,
                tradingLogsData = tradingLogsData,
                tradingInfoData = tradingInfoData
            };
        }

        private void TryMergeMovingAverageData(List<WorkerData> candleData, List<Vector2> wmaData, List<Vector2> smmaData)
        {
            if (candleData != null && wmaData != null && smmaData != null)
            {
                for (int i = 0; i < candleData.Count; i++)
                {
                    candleData[i].Wma = wmaData[i].Y;
                    candleData[i].Smma = smmaData[i].Y;
                }
            }
        }
    }
}

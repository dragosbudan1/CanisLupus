using System.Collections.Generic;
using System.Linq;
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
            var viewData = await eventReceiver.ReceiveAsync<ViewData>("viewData");

            logger.Info("returning data");
            var workerData = MapToWorkerData(viewData);

            return new
            {
                candleData = workerData,
                tradingLogsData = viewData.TradingLogs
            };
        }

        private List<WorkerData> MapToWorkerData(ViewData data)
        {
            var workerData = data.CandleData.Select(x => new WorkerData()
            {   
                Orientation = x.Orientation,
                Bottom = x.Bottom,
                Top = x.Top
            }).ToList();

            TryMergeMovingAverageData(workerData, data.WmaData, data.SmaData);

            return workerData;
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

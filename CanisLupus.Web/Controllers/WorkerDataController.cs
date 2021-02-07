using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CanisLupus.Web.Events;
using CanisLupus.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

        private readonly ILogger<WorkerDataController> logger;
        private readonly IEventReceiver eventReceiver;

        public WorkerDataController(ILogger<WorkerDataController> logger, IEventReceiver eventReceiver)
        {
            this.eventReceiver = eventReceiver;
            this.logger = logger;
        }

        [HttpPost]
        public Task<bool> Post(SessionInitRequest req)
        {
            logger.LogInformation("SessionId: {0}", req.Id);
            //QueuePool.CreateQueue(req.Id)
            return Task.FromResult(true);
        }

        [HttpGet]
        public async Task<object> Get()
        {
            var task1 = eventReceiver.ReceiveAsync<List<WorkerData>>("candleData");
            var task2 = eventReceiver.ReceiveAsync<List<System.Numerics.Vector2>>("highClusterData");
            var task3 = eventReceiver.ReceiveAsync<List<System.Numerics.Vector2>>("lowClusterData");
            var task4 = eventReceiver.ReceiveAsync<List<System.Numerics.Vector2>>("wmaData");
            var task5 = eventReceiver.ReceiveAsync<List<System.Numerics.Vector2>>("smmaData");

            Task.WaitAll(task1, task2, task3, task4, task5);

            var candleData = await Task.FromResult(task1.Result);
            var highClusterData= await Task.FromResult(task2.Result);
            var lowClusterData= await Task.FromResult(task3.Result);
            var wmaData = await Task.FromResult(task4.Result);
            var smmaData = await Task.FromResult(task5.Result);

            TryMergeMovingAverageData(candleData, wmaData, smmaData);

            return new
            {
                candleData = candleData,
                highClusterData = MapToVector2Class(highClusterData),
                lowClusterData = MapToVector2Class(lowClusterData)
            };
        }

        private void TryMergeMovingAverageData(List<WorkerData> candleData, List<System.Numerics.Vector2> wmaData, List<System.Numerics.Vector2> smmaData)
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

        private List<Vector2> MapToVector2Class(List<System.Numerics.Vector2> vectors)
        {
            return vectors?.Select(x => new Vector2 { X = x.X, Y = x.Y }).ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CanisLupus.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkerDataController : ControllerBase
    {

        private readonly ILogger<WorkerDataController> _logger;

        public WorkerDataController(ILogger<WorkerDataController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<object> Get()
        {
            _logger.LogInformation("Getting data");
            var factory = new ConnectionFactory();
            using var connection = factory.CreateConnection(new List<string>() { "rabbitmq", "localhost" });

            var candleDataResponse = await GetQueueData("candleData", connection);
            var highClusterDataResponse = await GetQueueData("highClusterData", connection);
            var lowClusterDataResponse = await GetQueueData("lowClusterData", connection);
            var wmaDataResponse = await GetQueueData("wmaData", connection);
            var smmaDataResponse = await GetQueueData("smmaData", connection);

            var candleData = JsonConvert.DeserializeObject<List<WorkerData>>(candleDataResponse);
            var highClusterData = JsonConvert.DeserializeObject<List<System.Numerics.Vector2>>(highClusterDataResponse);
            var lowClusterData = JsonConvert.DeserializeObject<List<System.Numerics.Vector2>>(lowClusterDataResponse);
            var wmaData = JsonConvert.DeserializeObject<List<System.Numerics.Vector2>>(wmaDataResponse);
            var smmaData = JsonConvert.DeserializeObject<List<System.Numerics.Vector2>>(smmaDataResponse);

            MergeMovingAverageData(candleData, wmaData, smmaData);

            return new
            {
                candleData = candleData,
                highClusterData = MapToVector2Class(highClusterData),
                lowClusterData = MapToVector2Class(lowClusterData)
            };
        }

        private void MergeMovingAverageData(List<WorkerData> candleData, List<System.Numerics.Vector2> wmaData, List<System.Numerics.Vector2> smmaData)
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

        public class Vector2
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        private async Task<string> GetQueueData(string queueName, IConnection connection)
        {
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue: queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            string message = string.Empty;

            var msgCount = channel.MessageCount(queue: queueName);
            if (msgCount > 0)
            {
                await Task.Run(async () =>
                {
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        message = Encoding.UTF8.GetString(body);
                    };

                    var result = channel.BasicConsume(queue: queueName,
                                            autoAck: false,
                                            consumer: consumer);
                    await Task.Delay(500);
                });
            }

            return message;
        }
    }
}

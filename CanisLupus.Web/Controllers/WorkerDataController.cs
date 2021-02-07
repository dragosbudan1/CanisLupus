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
    public class SessionInitRequest
    {
        public string Id { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class WorkerDataController : ControllerBase
    {

        private readonly ILogger<WorkerDataController> _logger;

        public WorkerDataController(ILogger<WorkerDataController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public Task<bool> Post(SessionInitRequest req)
        {
            _logger.LogInformation("SessionId: {0}", req.Id);
            //QueuePool.CreateQueue(req.Id)
            return Task.FromResult(true);
        }

        [HttpGet]
        public async Task<object> Get()
        {
            _logger.LogInformation("Getting data");
            var factory = new ConnectionFactory();
            using var connection = factory.CreateConnection(new List<string>() { "rabbitmq", "localhost" });

            // var candleDataResponse = await GetQueueData("candleData", connection);
            // var highClusterDataResponse = await GetQueueData("highClusterData", connection);
            // var lowClusterDataResponse = await GetQueueData("lowClusterData", connection);
            // var wmaDataResponse = await GetQueueData("wmaData", connection);
            // var smmaDataResponse = await GetQueueData("smmaData", connection);

            var task1 = GetQueueData("candleData", connection);
            var task2 = GetQueueData("highClusterData", connection);
            var task3 = GetQueueData("lowClusterData", connection);
            var task4 = GetQueueData("wmaData", connection);
            var task5 = GetQueueData("smmaData", connection);

            Task.WaitAll(task1, task2, task3, task4, task5);

            var candleDataResponse = await Task.FromResult(task1.Result);
            var highClusterDataResponse = await Task.FromResult(task2.Result);
            var lowClusterDataResponse = await Task.FromResult(task3.Result);
            var wmaDataResponse = await Task.FromResult(task4.Result);
            var smmaDataResponse = await Task.FromResult(task5.Result);

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

        private async Task<string> GetQueueData(string exhName, IConnection connection)
        {

            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: exhName, type: ExchangeType.Fanout, durable: false, autoDelete: false);
            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: queueName,
                              exchange: exhName,
                              routingKey: "");

            Console.WriteLine(" [*] Waiting for logs. {0} {1}", queueName, exhName);

            string message = string.Empty;

            await Task.Run(async () =>
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] {0}", exhName);
                };
                channel.BasicConsume(queue: queueName,
                                     autoAck: false,
                                     consumer: consumer);
                while (string.IsNullOrEmpty(message))
                {
                    await Task.Delay(500);
                }
            });
            
            return message;
        }
    }
}

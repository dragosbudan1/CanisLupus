using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace CanisLupus.Worker.Events
{
    public interface IEventPublisher
    {
        Task<bool> PublishAsync(EventRequest req);
    }

    public class CandleDataPublisher : IEventPublisher
    {
        private readonly ILogger<CandleDataPublisher> logger;

        public CandleDataPublisher(ILogger<CandleDataPublisher> logger)
        {
            this.logger = logger;
        }

        public async Task<bool> PublishAsync(EventRequest req)
        {
            try
            {
                var factory = new ConnectionFactory();

                using var connection = factory.CreateConnection(new List<string>(){"rabbitmq", "localhost"});
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: req.QueueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                string message = req.Value;
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: req.QueueName,
                                     basicProperties: null,
                                     body: body);
                logger.LogInformation(" [{0}] Sent {1}", message.Length, req.QueueName);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return false;
            }

        }
    }
}
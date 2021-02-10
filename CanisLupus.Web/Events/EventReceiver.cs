using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CanisLupus.Web.Events
{
    public interface IEventReceiver
    {
        Task<T> ReceiveAsync<T>(string exchangeName);
    }
    public class EventReceiver : IEventReceiver
    {
        private readonly ILogger logger;
        public EventReceiver()
        {
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public async Task<T> ReceiveAsync<T>(string exchangeName)
        {
            var factory = new ConnectionFactory();
            using var connection = factory.CreateConnection(new List<string>() { "rabbitmq", "localhost" });
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout, durable: false, autoDelete: false);
            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: queueName,
                              exchange: exchangeName,
                              routingKey: "");

            logger.Info("Waiting for data {0}: {1}", queueName, exchangeName);

            string message = string.Empty;

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                message = Encoding.UTF8.GetString(body);
                logger.Info("Received from {0}: {1}", exchangeName, queueName);
            };

            channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);
            
            while (string.IsNullOrEmpty(message))
            {
                await Task.Delay(500);
            }
        
            return JsonConvert.DeserializeObject<T>(message);
        }
    }
}
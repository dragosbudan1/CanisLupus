using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;

namespace CanisLupus.Worker.Events
{
    public interface IEventPublisher
    {
        Task<bool> PublishAsync(EventRequest req);
    }

    public class EventPublisher : IEventPublisher
    {
        private readonly ILogger logger;

        public EventPublisher()
        {
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public async Task<bool> PublishAsync(EventRequest req)
        {
            try
            {
                var result = false;
                await Task.Run(() =>
                {
                    var factory = new ConnectionFactory();

                    using var connection = factory.CreateConnection(new List<string>() { "rabbitmq", "localhost" });
                    using var channel = connection.CreateModel();

                    channel.ExchangeDeclare(exchange: req.QueueName, type: ExchangeType.Fanout);

                    string message = req.Value;
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: req.QueueName,
                                         routingKey: "",
                                         basicProperties: null,
                                         body: body);
                    logger.Info(" [{0}] Sent {1}", message.Length, req.QueueName);

                    result = true;
                });


                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                return false;
            }

        }
    }
}
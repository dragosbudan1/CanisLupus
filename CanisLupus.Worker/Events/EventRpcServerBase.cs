using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CanisLupus.Worker.Events
{
    public interface IEventRpcServerBase
    {
        Task ListenAsync();
        Task InitialiseAsync();
        void Close();
    }
    public abstract class EventRpcServerBase : IEventRpcServerBase
    {
        protected string QueueName;
        protected Logger Logger;
        private IConnection connection;
        private IModel channel;
        private EventingBasicConsumer consumer;
        private string response;

        public void Close()
        {
            channel.Close();
            connection.Close();
        }

        protected virtual Task<string> ProcessMessage(string message)
        {
            return Task.FromResult(message);
        }

        public async Task InitialiseAsync()
        {
            if (string.IsNullOrEmpty(QueueName))
            {
                throw new ArgumentNullException("QueueName cannot be null");
            }

            var factory = new ConnectionFactory();
            connection = factory.CreateConnection(new List<string>() { "rabbitmq", "localhost" });
            channel = connection.CreateModel();
            channel.QueueDeclare(queue: QueueName, durable: false,
              exclusive: false, autoDelete: false, arguments: null);
            channel.BasicQos(0, 1, false);
            consumer = new EventingBasicConsumer(channel);
            response = null;
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    response = await ProcessMessage(message);
                }
                catch (Exception e)
                {
                    Logger.Info($" [.] {e.Message}");
                    response = "";
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                      basicProperties: replyProps, body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag,
                      multiple: false);
                }
            };

            await Task.Delay(500);
        }

        public async Task ListenAsync()
        {
            channel.BasicConsume(queue: QueueName,
                autoAck: false, consumer: consumer);
            Logger.Info($"[x] Awaiting {QueueName} requests");

            while (string.IsNullOrEmpty(response))
            {
                await Task.Delay(500);
            }

            response = null;
        }
    }
}
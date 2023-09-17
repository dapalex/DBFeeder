using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace Common.AMQP
{
    public class AMQPQueueClient
    {
        ILogger _logger;
        static IConnection Connection;
        public IModel Channel;
        public string QueueName;
        //Keep attempting for 1 min
        readonly int ConnectionAttemptsLimit = 12;
        readonly uint MessageCountLimit = 1000;

        public AMQPQueueClient(ILogger logger)
        {
            _logger = logger;
        }

        public bool Connect(string host)
        {
            int attempt = 0;

            try
            {
                var factory = new ConnectionFactory { HostName = host };
                do
                {
                    try
                    {
                        if(Connection == null)
                            Connection = factory.CreateConnection();
                        if(Channel == null)
                            Channel = Connection.CreateModel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Attempt {0} failed... {1}", attempt, ex.Message);
                        Thread.Sleep(5000);
                        attempt++;
                    }
                } while (Connection == null && attempt < ConnectionAttemptsLimit);

                if (Connection != null)
                {
                    _logger.LogInformation("AMQP connection instantiated");
                    return true;
                }
                else
                    throw new ConnectFailureException(string.Format("Unable to connect to {0}", host), null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool DeclareQueue(string queueName, bool isProducer, int queueSizeLimit = 10000)
        {
            int attempt = 0;

            try
            {
                Dictionary<string, object> queueArgs = new Dictionary<string, object>
                                                        {
                                                            { "x-max-length", queueSizeLimit },
                                                            { "x-dead-letter-exchange", "dlx-" + queueName }
                                                        };

                QueueDeclareOk retdeclare = null;
                if (isProducer)
                {
                    _logger.LogDebug("Declaring AMQP queue {0} as producer...", queueName);
                    retdeclare = Channel.QueueDeclare(queue: queueName,
                                                     durable: true,
                                                     exclusive: false,
                                                     autoDelete: false,
                                                     arguments: queueArgs);
                }
                else
                    do
                    {
                        try
                        {
                            _logger.LogDebug("Declaring AMQP queue {0} as consumer...", queueName);
                            retdeclare = Channel.QueueDeclarePassive(queue: queueName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Attempt {0} failed...", attempt);
                            Thread.Sleep(5000);
                            attempt++;
                        }
                    } while (retdeclare == null && attempt < ConnectionAttemptsLimit);


                if (retdeclare != null)
                {
                    _logger.LogInformation("AMQP queue {0} declared", retdeclare.QueueName);
                    this.QueueName = retdeclare.QueueName;
                    return true;
                }
                else
                    _logger.LogError("AMQP queue not declared");
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return false;
        } 
    

        public bool Send<T>(object message, string messageType) where T : AMQPMessage
        {
            try
            {
                uint messageCount = Channel.MessageCount(QueueName);
                _logger.LogInformation("Current message count for {0}: {1}", QueueName, messageCount);
                while ((messageCount = Channel.MessageCount(QueueName)) >= MessageCountLimit)
                {
                    _logger.LogWarning("Message count for queue {1} over limit {2}, waiting...", this.QueueName, MessageCountLimit);
                    Thread.Sleep(60000);
                }

                string jsonMessage = Common.Serializer.Serializer.Serialize<T>(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                var msgProperties = Channel.CreateBasicProperties();
                msgProperties.Persistent = true;
                msgProperties.Type = messageType;
                
                Channel.BasicPublish(exchange: string.Empty,
                                 routingKey: this.QueueName,
                                 basicProperties: msgProperties,
                                 body: body);

                _logger.LogInformation($" [x] Sent {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Exception sending message to queue {0}", this.QueueName));
                return false;
            }

            return true;
        }

        public T ParseMessage<T>(ReadOnlyMemory<byte> byteMessage) where T : AMQPMessage
        {
            try
            {
                var body = byteMessage.ToArray();
                var message = Encoding.UTF8.GetString(body);
                return (T) Common.Serializer.Serializer.Deserialize<T>(message, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Exception sending message to queue {0}", this.QueueName));
                return null;
            }
        }
    }
}

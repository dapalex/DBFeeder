﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace Common.AMQP
{
    public class AMQPQueueClient
    {
        ILogger _logger;
        CancellationToken executeAsyncToken;

        [ThreadStatic]
        static IConnection Connection;
        [ThreadStatic]
        public IModel Channel;
        [ThreadStatic]
        public string QueueName;
        //Keep attempting for 1 min
        readonly int ConnectionAttemptsLimit = 12;
        readonly int sleepTime = 5000;
        readonly uint MessageCountLimit = 1000;

        public AMQPQueueClient(ILogger logger, CancellationToken parentToken)
        {
            _logger = logger;
            executeAsyncToken = parentToken;
        }

        public bool Connect(string host)
        {
            int attempt = 0;

            try
            {
                var factory = new ConnectionFactory { HostName = host };
                factory.AutomaticRecoveryEnabled = true;
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
                        if(_logger != null) _logger.LogWarning("Attempt {0} failed... {1}", attempt, ex.Message);
                        Thread.Sleep(sleepTime);
                        attempt++;
                    }
                } while (Connection == null && attempt < ConnectionAttemptsLimit);

                if (Connection != null)
                {
                    if (_logger != null) _logger.LogInformation("AMQP connection instantiated");
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
                                                            { "x-dead-letter-exchange", "dlx-" + queueName + "-exchange" }
                                                        };

                QueueDeclareOk retdeclare = null;
                if (isProducer)
                {
                    if (_logger != null) _logger.LogDebug("Declaring AMQP queue {0} as producer...", queueName);
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
                            if (_logger != null) _logger.LogDebug("Declaring AMQP queue {0} as consumer...", queueName);
                            retdeclare = Channel.QueueDeclarePassive(queue: queueName);
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null) _logger.LogWarning("Attempt {0} failed...", attempt);
                            Thread.Sleep(sleepTime);
                            attempt++;
                        }
                    } while (retdeclare == null && attempt < ConnectionAttemptsLimit);


                if (retdeclare != null)
                {
                    if (_logger != null) _logger.LogInformation("AMQP queue {0} declared", retdeclare.QueueName);
                    this.QueueName = retdeclare.QueueName;
                    return true;
                }
                else
                    if (_logger != null) _logger.LogError("AMQP queue not declared");
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
                uint messageCount;
                while ((messageCount = Channel.MessageCount(QueueName)) >= MessageCountLimit)
                {
                    if (_logger != null) _logger.LogWarning("Message count for queue {0} is {1} over limit {2}, waiting...", this.QueueName, messageCount, MessageCountLimit);
                    Thread.Sleep(sleepTime);
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

                if (_logger != null) _logger.LogDebug($" [x] Sent {message}");
            }
            catch (Exception ex)
            {
                if (_logger != null) _logger.LogError(ex, string.Format("Exception sending message to queue {0}", this.QueueName));
                return false;
            }

            return true;
        }

        public object CloseQueueClient()
        {
            Channel.Close();
            return Channel.IsClosed ? Channel.CloseReason.Cause : null;
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
                if (_logger != null) _logger.LogError(ex, string.Format("Exception sending message to queue {0}", this.QueueName));
                return null;
            }
        }
    }
}

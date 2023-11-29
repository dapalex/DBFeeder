using Common;
using Common.AMQP;
using DBFeederEntity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using DACService.Properties;
using Microsoft.Identity.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DACService
{
    public class Worker : BackgroundService
    {
        Type EFServiceType;
        Type entityType;
        internal static ILogger _logger;
        static AMQPQueueClient amqpClient;
        Assembly EFCoreLibAssembly;
        DbContext context;
        EventingBasicConsumer consumer;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            Thread.CurrentThread.Name = "DACService-" + Program.SourceDeclaration;
            //Crawler Queue definition

            try
            {
                _logger.LogDebug("Loading assembly {0}...", Resources.EFCoreLibraryName);
                EFCoreLibAssembly = Assembly.Load(Resources.EFCoreLibraryName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, string.Format("Error during loading of EFCore library"));
            }
            try
            {
                //Connect to SQL Server DB
                amqpClient = new AMQPQueueClient(_logger);

                if (!amqpClient.Connect(Program.RabbitMqHost))
                    _logger.LogError("Error during connection to {0}", Program.RabbitMqHost);

                amqpClient.DeclareQueue(Program.SourceDeclaration, false);
                //#endif
            }
            catch (Exception e)
            {
                _logger.LogError(e, string.Format("Error during management of Event bus for host {0}, queue {1}", Program.RabbitMqHost, Program.SourceDeclaration));
            }

            consumer = new EventingBasicConsumer(amqpClient.Channel);
            consumer.Registered += Consumer_Registered;
            consumer.Received += ReceivedHandler;

            amqpClient.Channel.BasicConsume(Program.SourceDeclaration, false, Program.SourceDeclaration + "-consumer", false, true, null, consumer);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Thread.Sleep(10000);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Queue {0} containing {1} messages", Program.SourceDeclaration, amqpClient.Channel.MessageCount(Program.SourceDeclaration));
            }

            await Task.Delay(1000, stoppingToken);
        }

        private void Consumer_Registered(object? sender, ConsumerEventArgs e)
        {
            _logger.LogInformation("Consumer {0} successfully registered to queue", Process.GetCurrentProcess().Id);
        }

        private void ReceivedHandler(object? sender, BasicDeliverEventArgs e)
        {
            AMQPEntityMessage message = null;
            try
            {
                Thread.CurrentThread.Name = "DACServiceWorker-" + Program.SourceDeclaration;

                message = amqpClient.ParseMessage<AMQPEntityMessage>(e.Body);

            }
            catch (Exception ex)
            {
                var ackNack = ExceptionMgr.ManageException(ex, _logger);

                //NACK
                _logger.LogError("Sending KO...");
                amqpClient.Channel.BasicNack(e.DeliveryTag, false, true);
            }
            try
            {
                SetReflectionTypes(e.BasicProperties.Type);

                _logger.LogDebug("Creating intance of service...");

                Type contextType = EFCoreLibAssembly.GetType(Resources.DBContextTypeDeclaration);
                context = (DbContext)Activator.CreateInstance(contextType);
                var EFGenericService = Activator.CreateInstance(EFServiceType, new UnitOfWork(context));

                if (message != null) //While elements in queue
                {
                    Program.CurrentEnricher = new Serilog.Core.Enrichers.PropertyEnricher("MESSAGE", message.EntityData, true);

                    object currEntity = Activator.CreateInstance(entityType);

                    foreach (KeyValuePair<string, string> kvp in message.EntityData)
                    {
                        string propertyName = null;
                        if ((propertyName = entityType.GetProperty(kvp.Key)?.Name) != null)
                        {
                            _logger.LogDebug("Setting {0}: {1} ", propertyName, kvp.Value);
                            entityType.GetProperty(propertyName).SetValue(currEntity, kvp.Value);
                        }
                        else
                            _logger.LogWarning("Field {0} not present in message for type {1}", kvp.Key, entityType.Name);
                    }
                    _logger.LogDebug("Created entity {0}", currEntity.ToString());

                    _logger.LogDebug("Created service instance {0}", EFGenericService.ToString());
                    object methodExec = EFServiceType.InvokeMember("Add", BindingFlags.InvokeMethod, null, EFGenericService, new object[] { currEntity });

                    Guid addExecuter = ((Guid)methodExec);
                    if (addExecuter == Guid.Empty)
                    {
                        //NACK
                        _logger.LogError("Guid Empty: Add failed, Sending KO...\n {0}", message.ToString());
                        amqpClient.Channel.BasicNack(e.DeliveryTag, false, true);
                    }
                    else
                    {
                        //ACK
                        _logger.LogInformation("Sending OK...");
                        amqpClient.Channel.BasicAck(e.DeliveryTag, false);
                    }
                }
                else
                {
                    //NACK --> XDQ
                    _logger.LogError("Message null, Sending KO...\n {0}", e.Body);
                    amqpClient.Channel.BasicNack(e.DeliveryTag, false, true);
                }
            }
            catch (Exception ex)
            {
                var ackNack = ExceptionMgr.ManageException(ex, _logger);

                if(ackNack)
                {
                    _logger.LogInformation("Sending OK...");
                    amqpClient.Channel.BasicAck(e.DeliveryTag, false);
                }
                else
                {
                    //NACK
                    _logger.LogError("Sending KO...");
                    amqpClient.Channel.BasicNack(e.DeliveryTag, false, true);
                }
            }
        }

        private void SetReflectionTypes(string messageType)
        {

            _logger.LogDebug("Extract type structure from {0}...", messageType);
            string[] fullClassName = messageType.Split('.');

            entityType = EFCoreLibAssembly.GetType(messageType);

            if(EFServiceType == null)
            {
                Type openGenericServiceType = Type.GetType(Resources.GenericServiceTypeDeclaration);
                _logger.LogDebug("Creating open Generic Type for {0}...", entityType);
                EFServiceType = openGenericServiceType.MakeGenericType(entityType);
            }
        }
    }
}
using Common;
using Common.AMQP;
using Common.Serializer;
using Docker.DotNet.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Serilog.Core.Enrichers;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading.Channels;

namespace ScraperService
{
    public class Worker : BackgroundService
    {
        public static ILogger<Worker> _logger;
        public CancellationToken executionToken;

        AbstractModel? model;

        static AMQPQueueClient ReceivingClient;
        internal static AMQPQueueClient SendingClient;
        EventingBasicConsumer consumer;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            Thread.CurrentThread.Name = "Scraper-" + Program.SourceDeclaration;

            ///MODEL DESERIALIZATION
            string jsonFileName = Path.Combine(Directory.GetCurrentDirectory(), Program.config["ConfigFolder"], Program.SourceDeclaration + ".json");
            logger.LogInformation("Deserializing {0}...", Program.SourceDeclaration);
            model = Serializer.DeserializeConfig(jsonFileName);

            if (model == null || model.extraction == null)
                throw new Exception("Config not parsed");
            else
                logger.LogInformation("Config parsed");

            string sendingQueueName = model.extraction.name;

            try
            {
                SendingClient = new AMQPQueueClient(_logger, executionToken);

                if (!SendingClient.Connect(Program.RabbitMqHost))
                    _logger.LogError("Error during connection to {0}", Program.RabbitMqHost);

                SendingClient.DeclareQueue(sendingQueueName, true);
                //#endif
            }
            catch (Exception e)
            {
                _logger.LogError(e, string.Format("Error during management of Event bus for host {0}, queue {1}", Program.RabbitMqHost, model.extraction.name));
            }

            //Crawler Queue definition
            SetupCrawlerQueue();
        }

        private void SetupCrawlerQueue()
        {
            bool isRecQueueReady = false;
            try
            {
                ReceivingClient = new AMQPQueueClient(_logger, executionToken);

                if (!ReceivingClient.Connect(Program.RabbitMqHost))
                    _logger.LogError("Error during connection to {0}", Program.RabbitMqHost);

                isRecQueueReady = ReceivingClient.DeclareQueue(Program.SourceDeclaration, false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, string.Format("Error during management of Event bus for host {0}, queue {1}", Program.RabbitMqHost, model.extraction.name));
            }

            if (!isRecQueueReady)
            {
                string errMsg = string.Format("Receiving queue {0} not ready", ReceivingClient.QueueName);
                _logger.LogError(errMsg);
                throw new Exception(errMsg);
            }

            consumer = new EventingBasicConsumer(ReceivingClient.Channel);
            consumer.Registered += Consumer_Registered;
            consumer.Received += ReceivedHandler;

            ReceivingClient.Channel.BasicConsume(Program.SourceDeclaration, false, Program.SourceDeclaration + "-consumer", false, true, null, consumer);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            executionToken = stoppingToken;
            try
            {
                //JUST STAY ALIVE
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        Thread.Sleep(10000);
                        _logger.LogInformation("Worker {0} running at: {time}", Thread.CurrentThread.Name, DateTimeOffset.Now);
                        _logger.LogInformation("Queue {0} containing {1} messages", Program.SourceDeclaration, ReceivingClient.Channel.MessageCount(Program.SourceDeclaration));
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error during check of messages!");
                    }
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Cancellation requested, delaying 5 sec...");
                    await Task.Delay(5000);
                }

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in ExecuteAsync");
            }
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Received STOP Signal...");
            try
            {
                foreach (string consumerTag in consumer.ConsumerTags)
                {
                    _logger.LogWarning("Canceling consumer {0}...", consumerTag);
                    ReceivingClient.Channel.BasicCancel(consumerTag);
                }

                _logger.LogWarning("Detaching from queue {0}...", ReceivingClient.QueueName);
                ReceivingClient.CloseQueueClient();
                _logger.LogWarning("Detaching from queue {0}...", SendingClient.QueueName);
                SendingClient.CloseQueueClient();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception during queues detach");
            }

            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _logger.LogWarning("Disposing...");
            base.Dispose();
        }

        private void Consumer_Registered(object? sender, ConsumerEventArgs e)
        {
            _logger.LogInformation(Process.GetCurrentProcess().Id + " - Consumer successfully registered to queue ");
        }

        private void ReceivedHandler(object? sender, BasicDeliverEventArgs e)
        {
            Thread.CurrentThread.Name = "Scraper-" + Program.SourceDeclaration;
            _logger.LogInformation("Starting extraction for {0}... ", model.extraction.name);
            try
            {
                AMQPUrlMessage message = ReceivingClient.ParseMessage<AMQPUrlMessage>(e.Body);

                Program.CurrentMessage = new PropertyEnricher("MESSAGE", message.url);
                if (message != null) //While elements in queue
                {
                    HtmlNode? container = null;
                    if (string.IsNullOrEmpty(message.url))
                        _logger.LogWarning("Got empty url here!");

                    try
                    {
                        //Fetch source url
                        string urlSuffix = new Uri(message.url).LocalPath;
                        string pageContent = string.Empty;

                        try
                        {
                            pageContent = Surfer.FetchPage(message.url, int.Parse(Program.config["GentleScrapTime"])).Result;
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(5000);
                            //NACK --> Requeue
                            _logger.LogError("Page {0} not fetched", message.url);
                            _logger.LogError("Sending KO...");
                            ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, true);
                            return;
                        }

                        _logger.LogDebug(message.url + " fetched");

                        Scraper currScraper = new Scraper(Worker._logger);
                        //Scrap page
                        List<Dictionary<string, string>> entities = currScraper.ScrapPage(pageContent, model.extraction/*, ref container*/);

                        if (entities != null && entities.Count > 0)
                        {
                            _logger.LogInformation("Scraping succeded for {0}", message.url);

                            foreach (Dictionary<string, string> entity in entities)
                            {
                                AMQPEntityMessage sendMsg = new AMQPEntityMessage();
                                sendMsg.SetMessageBody(/*model.extraction.name, */entity);
                                SendingClient.Send<AMQPEntityMessage>(sendMsg, model.extraction.target.classType);
                            }

                            //ACK
                            _logger.LogInformation("Sending OK...");
                            ReceivingClient.Channel.BasicAck(e.DeliveryTag, false);
                        }
                        else
                        {
                            _logger.LogError("Scraping not succeded for {0}", message.url);
                            //NACK --> XDQ
                            _logger.LogError("Sending KO...");
                            ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, true);
                        }

                        currScraper.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception during url extraction");
                        //NACK --> XDQ
                        _logger.LogError("Sending KO...");
                        ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, true);
                    }
                }
                else
                {
                    _logger.LogError("Message empty");
                    //NACK --> XDQ
                    _logger.LogError("Sending KO...");
                    ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, true);
                }
            }
            catch (AlreadyClosedException ace)
            {
                _logger.LogError("Connection to receiving queue lost: {0}", ace.ShutdownReason);
                SetupCrawlerQueue();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Exception while handling receive of {0}", model.extraction.name));
                _logger.LogError("Sending KO...");
                ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, true);
            }
        }
    }
}

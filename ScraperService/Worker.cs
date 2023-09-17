using Common;
using Common.AMQP;
using Common.Serializer;
using Docker.DotNet.Models;
using HtmlAgilityPack;
using RabbitMQ.Client.Events;
using Serilog.Core.Enrichers;
using System.Diagnostics;
using System.Net.Mail;

namespace ScraperService
{
    public class Worker : BackgroundService
    {
        public static ILogger<Worker> _logger;
        static AMQPQueueClient ReceivingClient;
        internal static AMQPQueueClient SendingClient;
        EventingBasicConsumer consumer;
        AbstractModel? model;

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
            //Receiving queue

            bool isRecQueueReady = false;
            try
            {
                ReceivingClient = new AMQPQueueClient(_logger);

                if (!ReceivingClient.Connect(Program.RabbitMqHost))
                    _logger.LogError("Error during connection to {0}", Program.RabbitMqHost);

                isRecQueueReady = ReceivingClient.DeclareQueue(Program.SourceDeclaration, false);
                //#endif
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

            //Sending Queue
            try
            {
                SendingClient = new AMQPQueueClient(_logger);

                if (!SendingClient.Connect(Program.RabbitMqHost))
                    _logger.LogError("Error during connection to {0}", Program.RabbitMqHost);

                SendingClient.DeclareQueue(model.extraction.name, true);
                //#endif
            }
            catch (Exception e)
            {
                _logger.LogError(e, string.Format("Error during management of Event bus for host {0}, queue {1}", Program.RabbitMqHost, model.extraction.name));
            }

            //Registering to receiving queue
            consumer = new EventingBasicConsumer(ReceivingClient.Channel);
            consumer.Registered += Consumer_Registered;
            consumer.Received += ReceivedHandler;
            
            ReceivingClient.Channel.BasicConsume(Program.SourceDeclaration, false, Program.SourceDeclaration + "-consumer", false, true, null, consumer);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            //JUST STAY ALIVE
            while (!stoppingToken.IsCancellationRequested)
            {
                Thread.Sleep(10000);
                _logger.LogInformation("Worker {0} running at: {time}", Thread.CurrentThread.Name, DateTimeOffset.Now);
                _logger.LogInformation("Queue {0} containing {1} messages", Program.SourceDeclaration, ReceivingClient.Channel.MessageCount(Program.SourceDeclaration));
            }
            _logger.LogInformation("Extraction ended for " + model.extraction.name);
               
            await Task.Delay(1000, stoppingToken);
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
                            _logger.LogError("Page {0} not fetched, sending KO...", message.url);
                            ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, false);
                            return;
                        }

                        _logger.LogDebug(message.url + " fetched");

                        Scraper currScraper = new Scraper();
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
                            _logger.LogError("Scraping not succeded for {0}, sending KO...", message.url);
                            //NACK --> XDQ
                            ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, false);
                        }

                        currScraper.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception during url extraction, sending KO...");
                        //NACK --> XDQ
                        ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, false);
                    }
                }
                else
                {
                    _logger.LogError("Message empty, sending KO...");
                    //NACK --> XDQ
                    ReceivingClient.Channel.BasicNack(e.DeliveryTag, false, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Exception while handling receive of {0}, sending KO...", model.extraction.name));
            }
        }
    }
}

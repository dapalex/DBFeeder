using Common;
using Common.AMQP;
using Common.Serializer;
using Docker.DotNet;
using Docker.DotNet.Models;
using HtmlAgilityPack;
using Serilog.Core.Enrichers;
using System.IO;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CrawlerService
{
    public class Worker : BackgroundService
    {
        TaskFactory tf;
        internal static ILogger _logger;
        List<AbstractModel?> models = new List<AbstractModel?>();
        [ThreadStatic]
        AMQPQueueClient amqpClient;

        //Lock for unsafe method in multithreading
        public object nextFinder = new object();
        public object sendLock = new object();

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            Thread.CurrentThread.Name = "Crawler";

            Persistence.CreateConnection();
#if DEBUG
            if (Program.ConfigFileTest != null)
            {
                models.Add(Serializer.DeserializeConfig(Program.ConfigFileTest));
                return;
            }
#endif
            foreach (string configName in Directory.GetFiles(Path.GetFullPath(Program.config["ConfigFolder"])))
            {
                try
                {
                    if (Path.GetExtension(configName) != ".json")
                    {
                        _logger.LogWarning("No valid json file {0}!", configName);
                    }
                    else
                    {
                        _logger.LogDebug("Deserializing {0}...", configName);
                        //TO TEST
                        AbstractModel currentModel = Serializer.DeserializeConfig(configName);
                        currentModel.fileNameSource = configName;
                        //TO TEST
                        models.Add(Serializer.DeserializeConfig(configName));
                    }

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Deserialization for {0} failed!", configName);

                }
            }

            _logger.LogInformation("Deserialized following models");
            foreach(Extraction extr in models.Select(m => m.extraction))
                _logger.LogInformation(extr.name);

            if (models.Count == 0)
            {
                string msg = "Config not parsed";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            else
                logger.LogDebug("Config parsed");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Initial Execution running tasks for each config
            List<Task> workerTasks = new List<Task>();
            // Store the task we're executing
            foreach (AbstractModel model in models)
            {
                _logger.LogDebug("Preparing model information for {0}", model.extraction.name);

                CrawlProgress crawlProgress = Persistence.ReadCrawlProgress(model.extraction.name);
                workerTasks.Add(new Task((object? state) =>
                {
                    var crawlProgress = (dynamic)state;
                    DoWork(model.extraction, SetQueueClient(model, stoppingToken), (CrawlProgress)crawlProgress, stoppingToken);
                }, (object?)crawlProgress, stoppingToken));

                _logger.LogInformation("Starting {0}...", model.extraction.name);
                workerTasks.Last().Start();

                Thread.Sleep(1000);
            }

            Task.WaitAll(workerTasks.ToArray());
            _logger.LogInformation("Worker tasks done");
            await Task.CompletedTask;
        }
        //TO TEST

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Cancellation done");
            await base.StopAsync(cancellationToken);
        }


        /// <summary>
        /// Worker thread job
        /// </summary>
        /// <param name="token"></param>
        /// <param name="extraction"></param>
        /// <param name="queue"></param>
        /// <param name="crawlProgress"></param>
        /// <param name="_logger"></param>
        private void DoWork(Extraction extraction, AMQPQueueClient queueClient, CrawlProgress crawlProgress, CancellationToken executeAsyncToken)
        {
            Thread.CurrentThread.Name = string.Join("-", "Crawler", "Worker", extraction.name);
            _logger.LogInformation("Worker for " + extraction.name + " running at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Starting extraction of " + extraction.name);
            amqpClient = queueClient;
            Crawler crawler = new Crawler(extraction, _logger);

            ///-----------DIRECT URLS MANAGEMENT-----------///
            if (extraction.directUrls != null && extraction.directUrls.Count > 0 && !executeAsyncToken.IsCancellationRequested)
                foreach(string directUrl in extraction.directUrls)
                {
                    Program.CurrentMessage = new PropertyEnricher("MESSAGE", directUrl);
                    try
                    {
                        if (string.IsNullOrEmpty(directUrl)) throw new Exception("Got empty url here!");
                        if (crawlProgress.crawled.Contains(directUrl))
                        {
                            _logger.LogWarning("URL {0} already crawled", directUrl);
                            continue;
                        }

                        _logger.LogDebug("Enqueuing direct url {0}", directUrl);
                        //Create new Url message for AMQP
                        AMQPUrlMessage message = new AMQPUrlMessage();
                        message.SetMessageBody(directUrl);

                        lock (sendLock)
                        {
                            queueClient.Send<AMQPUrlMessage>(message, extraction.target.classType);
                        }

                        _logger.LogDebug("Updating progress DB...");
                        crawlProgress.UpdateProgress(directUrl);
                    }catch (Exception ex)
                    {
                        _logger.LogError(ex, string.Format("Exception during extraction of {0}", directUrl));
                    }
                }
            ///-----------DIRECT URLS MANAGEMENT-----------///

            if (extraction.urlBase == null)
            {
                _logger.LogWarning("No URLs to crawl");
                _logger.LogWarning("Extraction ended for " + extraction.name);
                return;
            }

            string urlToCrawl = null;
            if (crawlProgress.crawled == null || crawlProgress.crawled.Count == 0)
                urlToCrawl = Utils.GetWellformedUrlString(extraction.urlBase, extraction.urlSuffix);
            else
                urlToCrawl = crawlProgress.crawled.Last();

            HtmlNode container = null;
            do
            {
                try
                {
                    Program.CurrentMessage = new Serilog.Core.Enrichers.PropertyEnricher("MESSAGE", urlToCrawl);

                    if (urlToCrawl == null) throw new Exception("Got empty url here!");

                    _logger.LogDebug("Starting fetch of " + urlToCrawl);
                    string pageContent = Surfer.FetchPage(urlToCrawl, int.Parse(Program.config["GentleCrawlTime"])).Result;
                    _logger.LogDebug("{0} fetched", urlToCrawl);

                    container = crawler.NavigateHtmlPage(pageContent, extraction);

                    if (crawlProgress.crawled.Contains(urlToCrawl))
                        _logger.LogWarning("URL {0} already crawled", urlToCrawl);
                    else
                    {
                        //Extract urls to scrap
                        _logger.LogDebug("Crawling urls to scrap...");
                        List<string> urls = crawler.ExtractUrls(pageContent);

                    //ENQUEUE URLS
                    foreach (string url in urls)
                    {
                        _logger.LogInformation("Enqueuing {0}...", url);
                        AMQPUrlMessage message = new AMQPUrlMessage();
                        message.SetMessageBody(url);

                            lock (sendLock)
                            {
                                queueClient.Send<AMQPUrlMessage>(message, extraction.target.classType);
                            }
                        }

                        _logger.LogDebug("Updating progress DB...");
                        crawlProgress.UpdateProgress(urlToCrawl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, string.Format("Exception during extraction of {0}", urlToCrawl));
                }

                lock (nextFinder)
                {
                    urlToCrawl = crawler.FindNext(urlToCrawl, extraction.next, ref container, crawlProgress, extraction.urlBase);
                }

                _logger.LogDebug("urlToCrawl is {1}", urlToCrawl);
            }
            //Open Next PAGE
            while (urlToCrawl != null && !executeAsyncToken.IsCancellationRequested);

            if (executeAsyncToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation for {0}: detaching from queue {1}...", Thread.CurrentThread.Name, queueClient.QueueName);
                try
                {
                    queueClient.CloseQueueClient();
                    _logger.LogWarning("Disposing Crawler...");
                    crawler.Dispose();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception during closure of crawler");
                }
            }

            _logger.LogInformation("Extraction ended for " + extraction.name);
        }

        private AMQPQueueClient SetQueueClient(AbstractModel model, CancellationToken executeAsyncToken)
        {
            AMQPQueueClient amqpClient = null;
            try
            {
                amqpClient = new AMQPQueueClient(_logger, executeAsyncToken);

                if (!amqpClient.Connect(Program.RabbitMqHost))
                    _logger.LogError("Error during connection to {0}", Program.RabbitMqHost);

                amqpClient.DeclareQueue(model.extraction.name, true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, string.Format("Error during management of Event bus for host {0}, queue {1}", Program.RabbitMqHost, model.extraction.name));
            }

            return amqpClient;
        }
        
    }
}

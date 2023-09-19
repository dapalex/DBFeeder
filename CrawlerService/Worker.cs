using Common;
using Common.AMQP;
using Common.Serializer;
using Docker.DotNet;
using Docker.DotNet.Models;
using HtmlAgilityPack;
using Serilog.Core.Enrichers;
using System.IO;

namespace CrawlerService
{
    public class Worker : IHostedService, IAsyncDisposable
    {
        private readonly Task _completedTask = Task.CompletedTask;
        internal static ILogger _logger;
        List<AbstractModel?> models = new List<AbstractModel?>();

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
                        models.Add(Serializer.DeserializeConfig(configName)); //DESERIALIZATION GOES INTO THE WORKERS!!!!
                    }

                }
                catch (Exception e)
                {
                    _logger.LogError("Deserialization for {0} failed!", configName);

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

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker started running...");
            // Create linked token to allow cancelling executing tasks from provided token
            CancellationTokenSource _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            List<Task> bgWorkers = new List<Task>();

            // Store the task we're executing
            foreach (AbstractModel model in models)
            {
                _logger.LogDebug("Preparing model information for {0}", model.extraction.name);
                //#if RELEASE
                AMQPQueueClient amqpClient = null;
                try
                {
                    amqpClient = new AMQPQueueClient(_logger);
                    
                    if (!amqpClient.Connect(Program.RabbitMqHost))
                        _logger.LogError("Error during connection to {0}", Program.RabbitMqHost);

                    amqpClient.DeclareQueue(model.extraction.name, true);
                    //#endif
                }
                catch(Exception e)
                {
                    _logger.LogError(e, string.Format("Error during management of Event bus for host {0}, queue {1}", Program.RabbitMqHost, model.extraction.name));
                }

                CrawlProgress crawlProgress = Persistence.ReadCrawlProgress(model.extraction.name);
                TaskFactory tf = new TaskFactory(cancellationToken);

                //Instantiate background worker crawling urls
                //Naming them?
                bgWorkers.Add(tf.StartNew((object? state) => {
                    var crawlProgress = (dynamic) state;
                    DoWork(model.extraction, amqpClient, (CrawlProgress)crawlProgress); 
                }, (object?) crawlProgress, _stoppingCts.Token));
                
            }

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            if (Task.WhenAll(bgWorkers).IsCompleted)
            {
                return _completedTask;
            }

            // Otherwise it's running
            return _completedTask;
        }

        /// <summary>
        /// Worker thread job
        /// </summary>
        /// <param name="token"></param>
        /// <param name="extraction"></param>
        /// <param name="queue"></param>
        /// <param name="crawlProgress"></param>
        /// <param name="_logger"></param>
        private void DoWork(Extraction extraction, AMQPQueueClient queue, CrawlProgress crawlProgress)
        {
            Thread.CurrentThread.Name = string.Join("-", "Crawler", "Worker", extraction.name);
            _logger.LogInformation("Worker for " + extraction.name + " running at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Starting extraction of " + extraction.name);
            
            Crawler crawler = new Crawler(extraction, _logger);

            ///-----------DIRECT URLS MANAGEMENT-----------///
            if (extraction.directUrls != null && extraction.directUrls.Count > 0)
                foreach(string directUrl in extraction.directUrls)
                {
                    Program.CurrentMessage = new PropertyEnricher("MESSAGE", directUrl);
                    try
                    {
                        if (directUrl == null) throw new Exception("Got empty url here!");
                        if (crawlProgress.fetched.Contains(directUrl))
                        {
                            _logger.LogWarning("URL {0} already crawled", directUrl);
                            continue;
                        }

                        _logger.LogDebug("Enqueuing direct url {0}", directUrl);
                        //Create new Url message for AMQP
                        AMQPUrlMessage message = new AMQPUrlMessage();
                        message.SetMessageBody(directUrl);

                        queue.Send<AMQPUrlMessage>(message, extraction.target.classType);

                        _logger.LogDebug("Updating progress DB...");
                        crawlProgress.UpdateProgress(directUrl);
                    }catch (Exception ex)
                    {
                        _logger.LogError(ex, string.Format("Exception during extraction of {0}", directUrl));
                    }
                }
            ///-----------DIRECT URLS MANAGEMENT-----------///

            if (extraction.urlBase == null && extraction.urlSuffix == null)
            {
                _logger.LogWarning("No URLs to crawl");
                return;
            }
            string urlToCrawl = Utils.GetWellformedUrlString(extraction.urlBase, extraction.urlSuffix);

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

                    if (crawlProgress.fetched.Contains(urlToCrawl))
                    {
                        _logger.LogWarning("URL {0} already crawled", urlToCrawl);
                        continue;
                    }

                    //Extract urls to scrap
                    _logger.LogDebug("Crawling urls to scrap...");
                    List<string> urls = crawler.ExtractUrls(pageContent);

                    //ENQUEUE URLS
                    foreach (string url in urls)
                    {
                        _logger.LogInformation("Enqueuing {0}...", url);
                        AMQPUrlMessage message = new AMQPUrlMessage();
                        message.SetMessageBody(url);

                        queue.Send<AMQPUrlMessage>(message, extraction.target.classType);
                    }

                    _logger.LogDebug("Updating progress DB...");
                    crawlProgress.UpdateProgress(urlToCrawl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, string.Format("Exception during extraction of {0}", urlToCrawl));
                }
            }
            //Open Next PAGE
            while ((urlToCrawl = crawler.FindNext(extraction.next, ref container, crawlProgress.fetched, extraction.urlBase)) != null);

            crawler.Dispose();
            _logger.LogInformation("Extraction ended for " + extraction.name);
        }

        /// <summary>
        /// Seems useless in docker container
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Service} is stopping.", nameof(CrawlerService));

            //Persistence.SaveData(persist.Progress);
            return _completedTask;
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}

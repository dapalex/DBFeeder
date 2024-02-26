using Common;
using Serilog;
using Serilog.Core.Enrichers;

namespace CrawlerService
{
    public class Program
    {
        public static IConfigurationRoot config;
        internal static string RabbitMqHost { get; private set; }
        internal static int RabbitMqPort { get; private set; }
#if DEBUG
        internal static string UrlToCrawl { get; private set; }
        internal static string ConfigFileTest { get; private set; }
#endif

        [ThreadStatic]

        internal static PropertyEnricher CurrentMessage = new PropertyEnricher("MESSAGE", new object());

        public static void Main(string[] args)
        {
#if DEBUG
            if (args.Length > 1)
            {
                RabbitMqHost = args[0];
                ConfigFileTest = args[1];
            }
            else
                RabbitMqHost = args[0];
#else
            try
            {
                if (args.Length == 1)
                {
                    RabbitMqHost = args[0];

                }
                else
                    throw new Exception("Event bus info not received!");
            }
            catch (Exception e)
            {
                throw new Exception("Event bus info not valid!" + args);
            }
#endif
            Console.WriteLine("Event bus host name received: {0}", RabbitMqHost);

            var builder = new ConfigurationBuilder()
                            .SetBasePath(AppContext.BaseDirectory)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            config = builder.Build();

            Console.WriteLine("Configuration Build succeeded");

            IHost host = CreateHostBuilder(args)
                            .UseConsoleLifetime()
                            .Build();

            Console.WriteLine("Running host...");
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();

            }).ConfigureLogging((hostContext, builder) =>
            {
                builder.ConfigureSerilog(hostContext.Configuration, new PropertyEnricher[] { CurrentMessage });
            }).UseSerilog();
        }

    }
}
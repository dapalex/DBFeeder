using Common;
using Serilog;
using Serilog.Core.Enrichers;

namespace ScraperService
{
    public class Program
    {
        public static IConfigurationRoot config;
        internal static string RabbitMqHost { get; private set; }
        internal static string SourceDeclaration { get; private set; }
        internal static PropertyEnricher CurrentMessage = new PropertyEnricher("MESSAGE", "");
#if DEBUG
        internal static string DebugURL { get; private set; }
#endif
        public static void Main(string[] args)
        {
            try
            {
                if (args != null && args.Length == 2)
                {
                    RabbitMqHost = args[0];
                    SourceDeclaration = args[1];
                    Thread.CurrentThread.Name = string.Join("-", "Scraper", SourceDeclaration);
                }
                else
                    throw new Exception("Event bus info not received!");
            }
            catch (Exception e)
            {
                throw new Exception("Event bus info not valid!" + args);
            }

            Console.WriteLine("Event bus host name received: {0}", RabbitMqHost);
            Console.WriteLine("Source declaration received: {0}", SourceDeclaration);

            var builder = new ConfigurationBuilder()
                            .SetBasePath(AppContext.BaseDirectory)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            config = builder.Build();

            IHost host = CreateHostBuilder(args).Build();

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
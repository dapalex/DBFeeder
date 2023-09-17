using Common;
using Docker.DotNet.Models;
using Serilog;
using Serilog.Core.Enrichers;

namespace DACService
{
    public class Program
    {
        public static IConfigurationRoot config;
        internal static string RabbitMqHost { get; private set; }
        internal static string SourceDeclaration { get; private set; }
        //Initial empty enricher
        internal static PropertyEnricher CurrentEnricher = new PropertyEnricher("MESSAGE", new object());

        public static void Main(string[] args)
        {
            try
            {
                if (args != null && args.Length == 2)
                {
                    RabbitMqHost = args[0];
                    SourceDeclaration = args[1];
                    Thread.CurrentThread.Name = string.Join("-", "DAC", SourceDeclaration);
                }
                else
                    throw new Exception("Event bus info not received!");

                Console.WriteLine("Event bus host name received: {0}", RabbitMqHost);
                Console.WriteLine("Source declaration received: {0}", SourceDeclaration);
            }
            catch (Exception e)
            {
                throw e;
            }

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
                builder.ConfigureSerilog(hostContext.Configuration, new PropertyEnricher[] { CurrentEnricher });
            }).UseSerilog();
        }
    }
}
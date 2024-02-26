using Common.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;

namespace Common
{
    public static class ServiceWorkerLogger
    {
        public static Serilog.ILogger CreateLogger(PropertyEnricher[] enrichProps)
        {
            LoggerConfiguration loggerConfig = new LoggerConfiguration()
                                .MinimumLevel.Debug()
                                //.WriteTo.Logger(lg =>
                                //    lg
                                //    //.WriteTo.File(".", restrictedToMinimumLevel: LogEventLevel.Debug)
                                //    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {ThreadName}] {Message:lj}{NewLine}{Exception}", restrictedToMinimumLevel: LogEventLevel.Information)
                                // )
                                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {ThreadName}] {Message:lj}{NewLine}{Exception}", restrictedToMinimumLevel: LogEventLevel.Warning)
                                .WriteTo.SQLite(Resources.SQLite_path, "LOG_ERRORS", restrictedToMinimumLevel: LogEventLevel.Error)
                                .Enrich.WithThreadName();

            loggerConfig.Enrich.With(enrichProps);

            return loggerConfig.CreateLogger(); 
        }
    }
    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder ConfigureSerilog(this ILoggingBuilder loggingBuilder, IConfiguration configuration, PropertyEnricher[] enrichProps)
        {

            Log.Logger = ServiceWorkerLogger.CreateLogger(enrichProps);
            return loggingBuilder;
        }
    }
}

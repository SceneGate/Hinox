namespace SceneGate.Hinox.Utils;

using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

internal static class AppLoggerFactory
{
    private static Microsoft.Extensions.Logging.LogLevel minimumLevel = Microsoft.Extensions.Logging.LogLevel.Warning;

    private static readonly Lazy<ILoggerFactory> factory = new(CreateFactory);

    public static Microsoft.Extensions.Logging.LogLevel MinimumLevel {
        get => minimumLevel;
        set {
            if (factory.IsValueCreated) {
                throw new InvalidOperationException("Cannot change verbosity after factory initialization");
            }

            minimumLevel = value;
        }
    }

    public static ILogger<T> CreateLogger<T>()
    {
        return factory.Value.CreateLogger<T>();
    }

    private static ILoggerFactory CreateFactory()
    {
        LogManager.ThrowConfigExceptions = true;
        var nlogConfig = CreateNLogConfiguration();
        var nlogProviderConfig = CreateNLogProviderConfiguration();

        return LoggerFactory.Create(builder =>
            builder.AddNLog(nlogConfig, nlogProviderConfig)
            .SetMinimumLevel(MinimumLevel));
    }

    private static LoggingConfiguration CreateNLogConfiguration()
    {
        var config = new LoggingConfiguration();

        var coloredConsole = new ColoredConsoleTarget {
            Layout = "${level:uppercase=true}: ${logger:shortName=true} => ${message:withException=true}",
            AutoFlush = true,
        };
        config.AddRuleForAllLevels(coloredConsole);

        return config;
    }

    private static NLogProviderOptions CreateNLogProviderConfiguration()
    {
        return new NLogProviderOptions() {
            RemoveLoggerFactoryFilter = false,
        };
    }
}

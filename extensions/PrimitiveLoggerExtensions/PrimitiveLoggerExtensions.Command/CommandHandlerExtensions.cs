using System;
using System.CommandLine;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using log4net;
using log4net.Config;
using log4net.Repository;

namespace PrimitiveLoggerExtensions.Command
{
    [PublicAPI]
    public static class CommandHandlerExtensions
    {
        public static void InvokeWithPrimitiveLogger(this RootCommand command, string[] args)
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            using (PrimitiveLogger.Logger.Instance())
            {
                try
                {
                    command.Invoke(args);
                }
                catch (Exception e)
                {
                    PrimitiveLogger.Logger.Instance().Error("Failed to run the app", e);
                }
            }
        }
    }
}

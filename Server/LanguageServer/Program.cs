using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.LanguageServer.Server;
using Reductech.EDR.Core.Internal;

namespace LanguageServer
{
    /// <summary>
    /// Basically an OptionsMonitor, but working I hope
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityChangeSync<T> where T : class
    {
        public EntityChangeSync(IConfiguration configuration)
        {
            Latest = configuration.Get<T>();
        }

        public T Latest { get; private set; }

        // Declare the delegate (if using non-generic pattern).
        public delegate void ChangeEventHandler(object sender, T entity);

        // Declare the event.
        public event ChangeEventHandler OnChange;

        // Wrap the event in a protected virtual method
        // to enable derived classes to raise the event.
        public virtual void EntityHasChanged(T entity)
        {
            Latest = entity;
            // Raise the event in a thread-safe manner using the ?. operator.
            OnChange?.Invoke(this, entity);
        }

        /// <summary>
        /// Returns whether the entity has changed
        /// </summary>
        public bool TryUpdate(T newEntity)
        {
            if (newEntity.Equals(Latest))
                return false;
            EntityHasChanged(newEntity);
            return true;
        }
    }


    internal class Program
    {
        public const string AppSettingsPath = "appsettings.json";

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        private static void Main(string[] args) => MainAsync(args).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        private static async Task MainAsync(string[] args)
        {
            var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(
                options =>
                {
                    options.ConfigurationBuilder.AddJsonFile(AppSettingsPath, true, true);


                    options
                        .WithInput(Console.OpenStandardInput())
                        .WithOutput(Console.OpenStandardOutput())
                        .ConfigureLogging(x => x.AddLanguageProtocolLogging().SetMinimumLevel(LogLevel.Debug))
                        .WithServices(x =>
                            x.AddSingleton<IFileSystem>(new FileSystem())
                                .AddSingleton<DocumentManager>()
                                //.AddSingleton(typeof(IOptionsMonitor<>), typeof(OptionsMonitor<>))
                                .AddSingleton<IAsyncFactory<StepFactoryStore>, StepFactoryStoreFactory>()
                                .AddSingleton(typeof(EntityChangeSync<>))
                        )
                        .WithHandler<DidChangeConfigurationHandler>()
                        .WithHandler<CompletionHandler>()
                        .WithHandler<TextDocumentSyncHandler>()
                        .WithHandler<HoverHandler>()
                        .WithHandler<RenameHandler>()
                        .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug)))
                        .OnStarted((ls, token) =>
                        {
                            //Debugger.Launch();

                            var logger = ls.GetRequiredService<ILogger<Program>>();

                            logger.LogInformation("Language Server started");
                            var changeSync = ls.GetRequiredService<EntityChangeSync<SCLLanguageServerConfiguration>>();

                            if (changeSync.Latest.LaunchDebugger)
                            {
                                Debugger.Launch();
                            }

                            
                            changeSync.OnChange += (_, x) =>
                            {
                                if (x.LaunchDebugger)
                                {
                                    Debugger.Launch();
                                }
                            };
                            return Task.CompletedTask;
                        })
                        ;
                });

            await server.WaitForExit;
        }
    }
}
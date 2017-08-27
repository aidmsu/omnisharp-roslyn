using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Eventing;
using OmniSharp.Plugins;
using OmniSharp.Services;
using OmniSharp.Stdio;
using OmniSharp.Stdio.Services;
using OmniSharp.Utilities;

namespace OmniSharp.Http
{
    public class Program
    {
        public static int Main(string[] args) => OmniSharp.Start(() =>
        {
            var application = new OmniSharpHttpCommandLineApplication();
            application.OnExecute(() =>
            {
                var environment = application.CreateEnvironment();
                var writer = new SharedConsoleWriter();
                var plugins = application.CreatePluginAssemblies();

                var program = new Program(environment, writer, plugins, application.Port, application.Interface);
                program.Start();

                return 0;
            });
            return application.Execute(args);
        });

        internal readonly IOmniSharpEnvironment _environment;
        internal readonly ISharedTextWriter _sharedTextWriter;
        internal readonly PluginAssemblies _pluginAssemblies;
        internal readonly int _serverPort;
        internal readonly string _serverInterface;
        internal static Program Instance;

        public Program(
            IOmniSharpEnvironment environment,
            ISharedTextWriter sharedTextWriter,
            PluginAssemblies pluginAssemblies,
            int serverPort,
            string serverInterface)
        {
            _environment = environment;
            _sharedTextWriter = sharedTextWriter;
            _pluginAssemblies = pluginAssemblies;
            _serverPort = serverPort;
            _serverInterface = serverInterface;
            Instance = this;
        }

        public void Start()
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(new[] { "--server.urls", $"http://{_serverInterface}:{_serverPort}" });

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(config.Build())
                .UseEnvironment("OmniSharp")
                .UseStartup(typeof(Startup));

            using (var app = builder.Build())
            {
                app.Start();

                var appLifeTime = app.Services.GetRequiredService<IApplicationLifetime>();

                Console.CancelKeyPress += (sender, e) =>
                {
                    appLifeTime.StopApplication();
                    e.Cancel = true;
                };

                if (_environment.HostProcessId != -1)
                {
                    try
                    {
                        var hostProcess = Process.GetProcessById(_environment.HostProcessId);
                        hostProcess.EnableRaisingEvents = true;
                        hostProcess.OnExit(() => appLifeTime.StopApplication());
                    }
                    catch
                    {
                        // If the process dies before we get here then request shutdown
                        // immediately
                        appLifeTime.StopApplication();
                    }
                }

                appLifeTime.ApplicationStopping.WaitHandle.WaitOne();
            }
        }
    }

    public class OmniSharpHttpEnvironment
    {
        public int Port { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Swall.Server;

namespace Swall.Tasks
{
    internal class ServerTask : SwallTask
    {
        private readonly int port;
        private readonly string path;

        private readonly CancellationToken waitToken;

        public override string Name => "server";

        public ServerTask(IReadOnlyDictionary<string, object> config, CancellationToken waitToken) : base(config)
        {
            port = int.TryParse(Config["port"]?.ToString(), out var p) ? p : 3000;
            path = Config["path"]?.ToString() ?? "\\";

            this.waitToken = waitToken;
        }

        /// <summary>
        /// Starts Kestrel server to serve static files from the specified directory.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override async Task Execute(string subTask = null, string parameters = null)
        {
            var serverPath = Path.GetFullPath(path, Environment.CurrentDirectory);

            var nullLoggerFactory = NullLoggerFactory.Instance;

            var socketTransportFactory = new SocketTransportFactory(Options.Create(new SocketTransportOptions()), nullLoggerFactory);
            var kestrelServerOptions = new KestrelServerOptions();

            kestrelServerOptions.ListenLocalhost(port);

            using var kestrelServer = new KestrelServer(Options.Create(kestrelServerOptions), socketTransportFactory, nullLoggerFactory);

            await kestrelServer.StartAsync(new HttpApplication(serverPath, nullLoggerFactory), CancellationToken.None);

            WriteToConsole($"Started on http://localhost:{port}");

            while (!waitToken.IsCancellationRequested)
            {
                await Task.Delay(500);
            }

            WriteToConsole("Stopping...");

            await kestrelServer.StopAsync(CancellationToken.None);
        }
    }
}

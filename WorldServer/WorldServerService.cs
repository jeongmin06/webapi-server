using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorldServer
{
    public class WorldServerService : BackgroundService
    {
        private readonly ILogger<WorldServerService> _logger;
        private readonly TcpListener _listener;
        private readonly int _port = 5100;

        public WorldServerService(ILogger<WorldServerService> logger)
        {
            _logger = logger;

            _listener = new TcpListener(IPAddress.Any, _port);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _listener.Start();
            _logger.LogInformation("Start TCP Server: {Port}", _port);
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _logger.LogInformation("Client Connected : {EndPoint}", client.Client.RemoteEndPoint);

                    var session = new SessionActor(client, _logger);
                    session.Start();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WorldServerService Cancel graceful.");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
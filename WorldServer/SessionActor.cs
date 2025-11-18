using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace WorldServer
{
    public class SessionActor
    {
        private readonly TcpClient _client;
        private readonly ILogger _logger;

        private readonly NetworkStream _stream;
        private readonly Channel<string> _channel;
        private readonly CancellationTokenSource _cts = new();

        private Task _receiveLoopTask;
        private Task _actorLoopTask;

        public SessionActor(TcpClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;

            _stream = client.GetStream();
            _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
        }

        public void Start()
        {
            _receiveLoopTask = Task.Run(ReceiveLoopAsync);
            _actorLoopTask = Task.Run(ActorLoopAsync);
        }

        private async Task ReceiveLoopAsync()
        {
            using var reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
            
            try
            {
                while(!_cts.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        _logger.LogInformation("line is null! Stop Receive");
                        break;
                    }

                    await _channel.Writer.WriteAsync(line, _cts.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReceiveLoop Error!");
            }
            finally
            {
                _channel.Writer.TryComplete();
                _cts.Cancel();
                _client.Close();
            }
        }

        private async Task ActorLoopAsync()
        {
            try
            {
                await foreach (var msg in _channel.Reader.ReadAllAsync(_cts.Token))
                {
                    await HandleMessageAsync(msg);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ActorLoop Error!");
            }
        }

        private async Task HandleMessageAsync(string msg)
        {
            _logger.LogInformation("RECV > {Msg}", msg);

            if (msg == "/ping")
            {
                await SendAsync("/pong");
            }
        }

        private async Task SendAsync(string message)
        {
            var buf = Encoding.UTF8.GetBytes(message + "\n");
            try
            {
                await _stream.WriteAsync(buf, 0, buf.Length, _cts.Token);
            }
            catch
            {
                _cts.Cancel();
            }
        }
    }
}
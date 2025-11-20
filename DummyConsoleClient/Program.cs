using System;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyConsoleClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string host = "server";
            int port = 5100;

            Console.WriteLine($"[CLIENT] Target : {host}:{port}");

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(host, port);

                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                    using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true)
                    {
                        AutoFlush = true
                    };

                    while (!cts.IsCancellationRequested)
                    {
                        await writer.WriteLineAsync("/ping");
                        Console.WriteLine("[CLIENT] SENT: /ping");

                        var response = await reader.ReadLineAsync();
                        if (response == null)
                        {
                            Console.WriteLine("[CLIENT] Server closed connection.");
                            break;
                        }

                        Console.WriteLine($"[CLIENT] RECV : {response}");

                        await Task.Delay(2000, cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CLIENT] Error : {ex.Message}");
                    Console.WriteLine("[CLIENT] 3sec later Try Reconnect...");
                    try
                    {
                        await Task.Delay(3000, cts.Token);
                    }
                    catch { }
                }
            }
            Console.WriteLine("[CLIENT] DisConnected.");
        }
    }
}
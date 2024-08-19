using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;


class Server(ServerOptions o)
{
    ServerOptions options = o;

    readonly ConcurrentQueue<(uint, uint)> openPorts = [];

    public async Task Run()
    {
        Console.Error.WriteLine("Starting listeners...");
        for (var port = options.StartPort; port <= options.EndPort; port++)
            _ = StartListening(port, loop: options.ReusePorts);

        Console.Error.WriteLine("Waiting for client...");
        while (true)
        {
            if (!openPorts.TryDequeue(out var port)) await Task.Delay(250);
            else
            {
                var status = port.Item2 == port.Item1 ? PortStatus.open : PortStatus.mapped;
                switch (options.Format)
                {
                    case Format.json: FormatJson(port.Item2, port.Item1, status); break;
                    case Format.csv: FormatCsv(port.Item2, port.Item1, status); break;
                    case Format.fancy: FormatFancy(port.Item2, port.Item1, status); break;
                    default: break;
                }
            }
        }
    }

    static void FormatFancy(uint localPort, uint dstPort, PortStatus status)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("dst:");
        Console.ResetColor();
        Console.Write("{0} --", dstPort);
        Console.ForegroundColor = status == PortStatus.open ? ConsoleColor.Green : ConsoleColor.Blue;
        Console.Write("{0}", status);
        Console.ResetColor();
        Console.Write("--> ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("local:");
        Console.ResetColor();
        Console.WriteLine("{0}", localPort);
    }

    static void FormatJson(uint localPort, uint dstPort, PortStatus status)
    {
        Console.WriteLine("{");
        Console.Write("\"status\": \"{0}\", \"dst\": {1}, \"local\": {2}", status, dstPort, localPort);
        Console.WriteLine("}");
    }

    static void FormatCsv(uint localPort, uint dstPort, PortStatus status)
    {
        Console.WriteLine("{0},{1},{2}", status, dstPort, localPort);
    }

    async Task StartListening(uint port, bool loop)
    {
        TcpListener listener = new(IPAddress.Any, (int)port);
        listener.Start();
        if (options.Verbose) Console.Error.WriteLine("Listening on port {0}", port);

        // only handle client once
        do
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Console.Error.WriteLine("Got client on port {0}", port);
            if (options.Verbose) Console.Error.WriteLine("Conenction from {0} -> {1}", client.Client.RemoteEndPoint, port);
            _ = HandleClient(client, port);
        } while (loop);

    }

    async Task HandleClient(TcpClient client, uint port)
    {
        using (client)
        {
            NetworkStream stream = client.GetStream();

            int bytesRead = 0;
            byte[] buffer = new byte[5];
            do bytesRead += await stream.ReadAsync(buffer);
            while (bytesRead < 5);

            if (uint.TryParse(Encoding.UTF8.GetString(buffer), out uint dstPort))
                openPorts.Enqueue((port, dstPort));
        }
    }
}
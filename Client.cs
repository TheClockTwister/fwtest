using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

class Client(ClientOptions o)
{
    readonly ClientOptions options = o;
    readonly ConcurrentQueue<uint> portsToScan = [];

    public async Task Run()
    {
        // Loop through each port in the specified range
        if (options.StartPort > options.EndPort)
        {
            Console.Error.WriteLine("Start port cannot be larger than end port!");
            Environment.Exit(1);
            return;
        }
        var total = options.EndPort - options.StartPort;
        for (var port = options.StartPort; port <= options.EndPort; port++) portsToScan.Enqueue(port);

        // Create a list of tasks to run in parallel
        var tasks = new Task[options.Parallel];
        for (int i = 0; i < tasks.Length; i++) tasks[i] = Task.Run(ProcessPortsAsync);

        while (!portsToScan.IsEmpty)
        {
            await Task.Delay(1000);
            Console.Error.WriteLine("Progress: {0}/{1} scanned.", total - portsToScan.Count, total);
        }
    }

    async Task ProcessPortsAsync()
    {
        while (portsToScan.TryDequeue(out uint port))
            await TryConnectAsync(options.Target, (int)port);
    }

    async Task TryConnectAsync(string serverIp, int port)
    {
        try
        {
            using TcpClient client = new();
            // Attempt to connect to the server on the given port
            if (!client.ConnectAsync(serverIp, port).Wait((int)options.Timeout)) return;
            NetworkStream stream = client.GetStream();

            // Send the port number as a string to the server
            string messageToSend = port.ToString("D5");
            byte[] dataToSend = Encoding.ASCII.GetBytes(messageToSend);
            await stream.WriteAsync(dataToSend);

            if (options.Verbose) Console.Error.WriteLine($"Connected to port {port}");

            // Close the connection
            stream.Close();
            client.Close();
        }
        catch (SocketException)
        {
            // If the connection fails (e.g., the port is closed), handle the exception
            if (options.Verbose) Console.Error.WriteLine($"Connection to port {port} failed.");
        }
    }

}
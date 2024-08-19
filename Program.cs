using CommandLine;
using CommandLine.Text;

enum Format { fancy, json, csv }
enum Mode { server, client }
enum PortStatus { closed, open, mapped }


// Base options (common for both server and client)
class BaseOptions
{
    [Option('v', "verbose", Default = false, HelpText = "Set output to verbose.")]
    public bool Verbose { get; set; }
}

// Server-specific options
[Verb("server", HelpText = "Run the tool in server mode.")]
class ServerOptions : BaseOptions
{
    [Option("format", HelpText = "Format to print open ports", Default = Format.fancy)]
    public Format Format { get; set; }

    [Option('h', "host", Default = "0.0.0.0", HelpText = "Host address to bind to.")]
    public required string Port { get; set; }

    [Option('r', "reuse-ports", HelpText = "If specified, conenctions to the same port can be made multiple times (server mode only).")]
    public bool ReusePorts { get; set; }

    [Value(0, MetaName = "startPort", HelpText = "Start port", Required = true)]
    public uint StartPort { get; set; }

    [Value(1, MetaName = "endPort", HelpText = "End port", Required = true)]
    public uint EndPort { get; set; }
}

// Client-specific options
[Verb("client", HelpText = "Run the tool in client mode.")]
class ClientOptions : BaseOptions
{
    // [Option('g', "grid", HelpText = "Show a live grid of port numbers and their status")]
    // public bool Grid { get; set; }
    [Option('p', "parallel", HelpText = "How many ports to check at the same time.", Default = (uint)8)]
    public uint Parallel { get; set; }

    [Option('t', "timeout", HelpText = "Connection timeout in ms.", Default = (uint)1000)]
    public uint Timeout { get; set; }

    [Value(0, MetaName = "target", HelpText = "Target host (IP/FQDN)", Required = true)]
    public required string Target { get; set; }

    [Value(1, MetaName = "startPort", HelpText = "Start port", Required = true)]
    public uint StartPort { get; set; }

    [Value(2, MetaName = "endPort", HelpText = "End port", Required = true)]
    public uint EndPort { get; set; }
}


public static class Program
{
    static void Main(string[] args)
    {
        var parser = new Parser(with => with.HelpWriter = null); // Disable the default help writer
        var parserResult = parser.ParseArguments<ServerOptions, ClientOptions>(args);
        parserResult
            .WithParsed<ServerOptions>(o => new Server(o).Run().Wait())
            .WithParsed<ClientOptions>(o => new Client(o).Run().Wait())
            .WithNotParsed(errs => DisplayHelp(parserResult));
    }

    static void DisplayHelp<T>(ParserResult<T> result)
    {
        var helpText = HelpText.AutoBuild(result, h =>
        {
            h.AddEnumValuesToHelpText = true;
            h.AdditionalNewLineAfterOption = false;
            h.Heading = "fwtest version 0.1.0"; // Custom heading
            h.Copyright = "by TheClockTwister | Distributed under GNU GPLv3"; // Custom copyright info
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, e => e);
        Console.WriteLine(helpText);
    }
}

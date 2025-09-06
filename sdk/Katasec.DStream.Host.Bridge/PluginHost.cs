using Grpc.Core;
using HCLog.Net;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK;
using System.Net;
using Proto = Katasec.DStream.Proto;

namespace Katasec.DStream.Host.Bridge;

public static class PluginHost
{
    public static async Task Run<TProvider, TConfig>()
        where TProvider : ProviderBase<TConfig>, IProvider, new()
        where TConfig : class, new()
    {
        var log = new HCLogger(typeof(TProvider).Name);
        var port = GetFreeTcpPort();

        var server = new Server
        {
            Services =
            {
                Proto.Plugin.BindService(new PluginServiceImpl<TProvider, TConfig>(log))
            },
            Ports = { new ServerPort("127.0.0.1", port, ServerCredentials.Insecure) }
        };
        server.Start();

        Console.Out.WriteLine($"1|1|tcp|127.0.0.1:{port}|grpc");
        Console.Out.Flush();
        log.Info($"grpc_server_started port={port}");

        AppDomain.CurrentDomain.ProcessExit += async (_, __) => await server.ShutdownAsync();
        await Task.Delay(-1);
    }

    private static int GetFreeTcpPort()
    {
        var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var p = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return p;
    }
}

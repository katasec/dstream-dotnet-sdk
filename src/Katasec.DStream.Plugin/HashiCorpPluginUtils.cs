using HCLog.Net;
using System.Net;
using System.Net.Sockets;

namespace Katasec.DStream.Plugin;


/// <summary>
/// Utility functions for implementing HashiCorp go-plugin protocol in .NET
/// </summary>
public static class HashiCorpPluginUtils
{
    /// <summary>
    /// Finds an available TCP port for the gRPC server
    /// </summary>
    /// <returns>An available port number</returns>
    public static int FindAvailablePort()
    {
        // Create a new socket
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Bind to port 0, which tells the OS to assign an available port
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        // Get the assigned port
        var endpoint = socket.LocalEndPoint as IPEndPoint;
        if (endpoint == null)
        {
            throw new InvalidOperationException("Failed to retrieve the local endpoint.");
        }
        return endpoint.Port;
    }

    /// <summary>
    /// Outputs the HashiCorp go-plugin handshake string to stdout
    /// </summary>
    /// <param name="port">The port number the gRPC server is listening on</param>
    /// <param name="logger">Optional logger to log the handshake</param>
    public static void SendHandshakeString(int port, HCLogger? logger = null)
    {
        // Format: CORE-PROTOCOL-VERSION|APP-PROTOCOL-VERSION|NETWORK-TYPE|NETWORK-ADDR|PROTOCOL
        // Example: 1|1|tcp|127.0.0.1:1234|grpc
        string handshakeString = $"1|1|tcp|127.0.0.1:{port}|grpc";

        // We need to temporarily restore stdout for the handshake string
        var originalOut = new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(originalOut);

        // Write the handshake string to stdout
        Console.WriteLine(handshakeString);
        Console.Out.Flush();

        // Suppress further stdout output
        Console.SetOut(System.IO.TextWriter.Null);

        // Log the handshake if a logger is provided
        logger?.Info("Handshake string sent to host");
    }

    /// <summary>
    /// Determines if the plugin is running in standalone mode
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>True if running in standalone mode, false otherwise</returns>
    public static bool IsStandaloneMode(string[] args)
    {
        // Check if --standalone is in the arguments
        if (args != null && Array.Exists(args, arg => arg.Equals("--standalone", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Configures console output for plugin mode (suppresses stdout)
    /// </summary>
    /// <param name="isStandalone">Whether the application is running in standalone mode</param>
    /// <returns>The original stdout writer (for restoration if needed)</returns>
    public static TextWriter ConfigureConsoleOutput(bool isStandalone)
    {
        // Save the original stdout writer
        var originalOut = Console.Out;

        // In plugin mode, redirect stdout to null to suppress all output except the handshake string
        if (!isStandalone)
        {
            Console.SetOut(TextWriter.Null);
        }

        return originalOut;
    }
}

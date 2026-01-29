// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
// Group Project
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
// (Continue enhancing in Sprints 2 & 3)
//

using SecureMessenger.Core;
using SecureMessenger.Network;
using SecureMessenger.Security;
using SecureMessenger.UI;

namespace SecureMessenger;

/// <summary>
/// Main entry point for the Secure Distributed Messenger.
///
/// Architecture Overview:
/// This application uses multiple threads to handle concurrent operations:
///
/// 1. Main Thread (UI Thread)
///    - Reads user input from console
///    - Parses commands using ConsoleUI
///    - Dispatches commands to appropriate handlers
///
/// 2. Accept Thread (Server)
///    - Runs Server to accept incoming connections
///    - Each accepted connection spawns a receive task
///
/// 3. Receive Task(s)
///    - One per connected client
///    - Reads messages from network
///    - Invokes OnMessageReceived event
///
/// 4. Client Receive Task
///    - Reads messages from server we connected to
///    - Invokes OnMessageReceived event
///
/// Thread Communication:
/// - Use events for connection/disconnection/message notifications
/// - Use CancellationToken for graceful shutdown
/// - (Optional) Use MessageQueue for more complex processing pipelines
///
/// Sprint Progression:
/// - Sprint 1: Basic threading and networking (connect, send, receive)
///             Uses simple Client/Server model
/// - Sprint 2: Add encryption (key exchange, AES encryption, signing)
/// - Sprint 3: Upgrade to peer-to-peer model with Peer class,
///             add peer discovery, heartbeat, and reconnection
/// </summary>
class Program
{
    // TODO: Declare your components as fields for access across methods
    // Sprint 1-2 components:
    // private static Server? _server;
    // private static Client? _client;
    // private static ConsoleUI? _ui;
    // private static string _username = "User";
    //
    // Sprint 3 additions:
    // private static PeerDiscovery? _peerDiscovery;
    // private static HeartbeatMonitor? _heartbeatMonitor;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Secure Distributed Messenger");
        Console.WriteLine("============================");

        // TODO: Initialize components
        // 1. Create Server for incoming connections
        // 2. Create Client for outgoing connection
        // 3. Create ConsoleUI for user interface
        // 4. (Optional) Create MessageQueue if using producer/consumer pattern

        // TODO: Subscribe to events
        // Server events:
        // - _server.OnClientConnected += endpoint => { ... };
        // - _server.OnClientDisconnected += endpoint => { ... };
        // - _server.OnMessageReceived += message => { ... };
        //
        // Client events:
        // - _client.OnConnected += endpoint => { ... };
        // - _client.OnDisconnected += endpoint => { ... };
        // - _client.OnMessageReceived += message => { ... };

        Console.WriteLine("Type /help for available commands");
        Console.WriteLine();

        // Main loop - handle user input
        bool running = true;
        while (running)
        {
            // TODO: Implement the main input loop
            // 1. Read a line from the console
            // 2. Skip empty input
            // 3. Parse the input using ConsoleUI.ParseCommand()
            // 4. Handle the command based on CommandType:
            //    - Connect: Call await _client.ConnectAsync(host, port)
            //    - Listen: Call _server.Start(port)
            //    - ListPeers: Display connection status
            //    - History: Show message history (Sprint 3)
            //    - Quit: Set running = false
            //    - Not a command: Send as a message

            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            // Temporary basic command handling - replace with full implementation
            switch (input.ToLower())
            {
                case "/quit":
                case "/exit":
                    running = false;
                    break;
                case "/help":
                    ShowHelp();
                    break;
                default:
                    Console.WriteLine("Command not yet implemented. See TODO comments.");
                    break;
            }
        }

        // TODO: Implement graceful shutdown
        // 1. Stop the server
        // 2. Disconnect the client
        // 3. (Sprint 3) Stop peer discovery and heartbeat monitor

        Console.WriteLine("Goodbye!");
    }

    /// <summary>
    /// Display help information.
    /// Replace this with ConsoleUI.ShowHelp() once implemented.
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine("\nAvailable Commands:");
        Console.WriteLine("  /connect <ip> <port>  - Connect to another messenger");
        Console.WriteLine("  /listen <port>        - Start listening for connections");
        Console.WriteLine("  /peers                - Show connection status");
        Console.WriteLine("  /history              - View message history (Sprint 3)");
        Console.WriteLine("  /quit                 - Exit the application");
        Console.WriteLine();
        Console.WriteLine("Sprint Progression:");
        Console.WriteLine("  Sprint 1: Basic /connect and /listen with message sending");
        Console.WriteLine("  Sprint 2: Messages are encrypted end-to-end");
        Console.WriteLine("  Sprint 3: Automatic peer discovery and reconnection");
        Console.WriteLine();
    }

    // TODO: Add helper methods as needed
    // Examples:
    // - HandleListen(string[] args) - start the server
    // - HandleConnect(string[] args) - connect to a server
    // - HandlePeers() - show connection status
    // - SendMessage(string content) - send to all connections
}

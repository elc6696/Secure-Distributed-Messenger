// Donald Tsang
// CSCI 251 - Secure Distributed Messenger
// Group Project
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
// (Continue enhancing in Sprints 2 & 3)
//

using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualBasic;
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
    private static Server? _server;
    private static Client? _client;
    private static ConsoleUI? _ui;
    private static string _username = "User";
    private static MessageQueue _queue = new();
    
    private static CancellationTokenSource _cts = new();

    //Sprint 2 additions:
    private static KeyExchange _keyExchange = new();
    private static byte[]? _myPublicKey;

    // Sprint 3 additions:
    private static PeerDiscovery? _peerDiscovery;
    private static HeartbeatMonitor? _heartbeatMonitor;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Secure Distributed Messenger");
        Console.WriteLine("============================");

        _server = new Server();
        _client = new Client();
        _ui = new ConsoleUI();

        _server.OnMessageReceived += message =>
        {
            _server?.Broadcast(message);
            if (message.Type == MessageType.Text)
                _queue.EnqueueIncoming(message);
        };

        _client.OnMessageReceived += message =>
        {
            if (message.Type == MessageType.KeyExchange && message.PublicKey != null
                && !message.PublicKey.SequenceEqual(_myPublicKey ?? Array.Empty<byte>()))
            {
                _keyExchange.ReceivePublicKey(message.PublicKey);
                byte[] encryptedKey = _keyExchange.CreateEncryptedSessionKey();
                var sessionMessage = new Message
                {
                    Type = MessageType.SessionKey,
                    Sender = _username,
                    PublicKey = encryptedKey
                };
                _client?.Send(sessionMessage);
                _keyExchange.Complete();
                _client.SessionKey = new AesEncryption(_keyExchange.SessionKey!);
                _ui?.DisplaySystem("Key exchange complete, session key established.");
    
            }
            else if (message.Type == MessageType.SessionKey && message.PublicKey != null
                && !_keyExchange.IsEstablished)
            {
                _keyExchange.ReceiveEncryptedSessionKey(message.PublicKey);
                _client.SessionKey = new AesEncryption(_keyExchange.SessionKey!);
                _ui?.DisplaySystem("Session key received and decrypted, secure communication established.");
            }
            else if (message.Type == MessageType.Text)
            {
                _queue.EnqueueIncoming(message);
            }
        };

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

        Thread receiveThread = new Thread(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    Message message = _queue.DequeueIncoming(_cts.Token);
                    await _ui.DisplayMessage(message);
                }
            } catch (OperationCanceledException) {}
        });
        receiveThread!.IsBackground = true;
        receiveThread!.Name = "ReceiveThread";
        receiveThread!.Start();

        Thread sendThread = new Thread(() =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    Message message = _queue.DequeueOutgoing(_cts.Token);
                    _server?.Broadcast(message);
                    if (_client?.IsConnected == true) {
                        _client?.Send(message);
                    }
                }
            } catch (OperationCanceledException) {}
        });
        sendThread!.IsBackground = true;
        sendThread!.Name = "SendThread";
        sendThread!.Start();

        // Main loop - handle user input
        bool running = true;
        while (running)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            CommandResult commandResult = _ui.ParseCommand(input);

            switch(commandResult.CommandType)
            {
                case CommandType.Connect:
                    if(commandResult.Args[0] == "local") {
                        await _client.ConnectAsync("127.0.0.1", 5001);
                    }else {
                        await _client.ConnectAsync(commandResult.Args[0], int.Parse(commandResult.Args[1]));
                    }
                    _client.setClientID(Random.Shared.Next(1, 1000)); // Assign a random client ID for demonstration
                    // Sprint 2 Addition with Key Exchange:
                    _keyExchange = new KeyExchange();
                    _myPublicKey = _keyExchange.GetPublicKey();
                    var publicKeyMessage = new Message
                    {
                        Type = MessageType.KeyExchange,
                        Sender = _username,
                        PublicKey = _myPublicKey
                    };
                    _client.Send(publicKeyMessage);
                    break;
                case CommandType.Listen:
                    if(commandResult.Args[0] == "local") {
                        await _server.Start(5001);
                    } else {
                        await _server.Start(int.Parse(commandResult.Args[0]));
                    }
                    _ui.DisplaySystem($"Listening on port {commandResult.Args[0]}");  // add this
                    break;
                case CommandType.Quit:
                    running = false;
                    break;
                case CommandType.Help:  
                    _ui.ShowHelp();
                    break;
                case CommandType.Create:
                    // await _server.CreateRoom(int.Parse(commandResult.Args[0]));
                    if (_client?.IsConnected == true) 
                    {
                        var command = new Message { Sender = _username + await _client.getClientID(), Content = "/create " + commandResult.Args[0] };
                        _client.Send(command);
                    }
                    else
                    {
                        await _server.CreateRoom(commandResult.Args[0]);
                        _ui.DisplaySystem($"Room {commandResult.Args[0]} created.");
                    }
                    break;
                case CommandType.Rooms:
                    if (_client?.IsConnected == true) 
                    {
                        var command = new Message { Sender = _username + await _client.getClientID(), Content = "/rooms" };
                        _client.Send(command);
                    }
                    else
                    {
                        List<string> _rooms = _server.GetRooms();
                        foreach (string room in _rooms)
                        {
                            Console.WriteLine(room);
                        }
                    }
                    break;
                case CommandType.Join:
                    if (_client?.IsConnected == true)
                    {
                        var command = new Message { Sender = _username + await _client.getClientID(), Content = "/join " + commandResult.Args[0] };
                        _client.Send(command);
                    }
                    else
                    {
                        _ui.DisplaySystem($"Not connected to a server");
                    }
                    break;
                case CommandType.Leave:
                    if (_client?.IsConnected == true)
                    {
                        var command = new Message { Sender = _username + await _client.getClientID(), Content = "/leave " + commandResult.Args[0] };
                        _client.Send(command);
                    }
                    break;
                case CommandType.Message:
                    if (_client?.IsConnected == true) {
                        var command = new Message { Sender = _username + await _client.getClientID(), Content = "/msg " + string.Join(" ", commandResult.Args[0..]) };
                        _client.Send(command);
                    }

                    // var message = new Message { Sender = _username + await _client.getClientID(), Content = String.Join(", ", commandResult.Args[1..]) };
                    // _server.BroadcastToRoom(message, int.Parse(commandResult.Args[0]));
                    break;
                default:
                    // Only send if connected to a server; otherwise this node is a pure relay
                    if (_client?.IsConnected == true)
                    {
                        var msg = new Message { Sender = _username + await _client.getClientID(), Content = commandResult.Message! };
                        _queue.EnqueueOutgoing(msg);
                    }
                    break;
            }
        }
        _cts.Cancel();
        _queue.CompleteAdding();
        receiveThread.Join();
        sendThread.Join();
        _server?.Stop();
        _client?.Disconnect();

        Console.WriteLine("Goodbye!");

    }

    // TODO: Add helper methods as needed
    // Examples:
    // - HandleListen(string[] args) - start the server
    // - HandleConnect(string[] args) - connect to a server
    // - HandlePeers() - show connection status
    // - SendMessage(string content) - send to all connections
}
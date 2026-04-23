// Donald Tsang
// CSCI 251 - Secure Distributed Messenger
// Group Project
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
// (Continue enhancing in Sprints 2 & 3)
//

using System.Collections.Concurrent;
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
    private static PeerDiscovery _peerDiscovery = new();
    private static HeartbeatMonitor _heartbeatMonitor = new();
    private static MessageHistory _history = new();

    // P2P: one outbound Client per remote peer
    private static ConcurrentDictionary<string, Client> _peerClients = new();
    private static ConcurrentDictionary<string, Peer> _activePeers = new();
    private static ConcurrentDictionary<string, KeyExchange> _peerKeyExchanges = new();
    private static ConcurrentDictionary<string, ReconnectionPolicy> _reconnectPolicies = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Secure Distributed Messenger");
        Console.WriteLine("============================");

        _server = new Server();
        _client = new Client();
        _ui = new ConsoleUI();

        _server.OnMessageReceived += message =>
        {
            if (message.Type == MessageType.Heartbeat)
            {
                _heartbeatMonitor.RecordHeartbeat(message.Sender);
                return;
            }
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

        _server.OnClientConnected += endpoint => _ui?.DisplaySystem($"Peer connected from {endpoint}");
        _server.OnClientDisconnected += endpoint => _ui?.DisplaySystem($"Peer disconnected: {endpoint}");

        _peerDiscovery.OnPeerDiscovered += async peer =>
        {
            _ui?.DisplaySystem($"Peer discovered: {peer.Id} at {peer.Address}:{peer.Port}");
            if (!_peerClients.ContainsKey(peer.Id) &&
                string.Compare(_peerDiscovery.LocalPeerId, peer.Id, StringComparison.Ordinal) < 0)
                await ConnectToPeer(peer);
        };
        _peerDiscovery.OnPeerLost += peer =>
        {
            _ui?.DisplaySystem($"Peer {peer.Id} lost (30s with no broadcast)");
            _heartbeatMonitor.StopMonitoring(peer.Id);
            if (_peerClients.TryRemove(peer.Id, out var lostClient)) lostClient.Disconnect();
            _activePeers.TryRemove(peer.Id, out _);
        };

        _heartbeatMonitor.OnConnectionFailed += HandlePeerConnectionFailed;
        _heartbeatMonitor.OnHeartbeatReceived += peerId =>
        {
            if (_reconnectPolicies.TryGetValue(peerId, out var p)) p.ResetAttempts(peerId);
        };

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
                    _history.SaveMessage(message);
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

        Thread heartbeatThread = new Thread(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var hb = new Message { Type = MessageType.Heartbeat, Sender = _peerDiscovery.LocalPeerId };
                    foreach (var kvp in _peerClients)
                        try { kvp.Value.Send(hb); } catch { }
                    await Task.Delay((int)_heartbeatMonitor.HeartbeatInterval.TotalMilliseconds, _cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        });
        heartbeatThread.IsBackground = true;
        heartbeatThread.Name = "HeartbeatThread";
        heartbeatThread.Start();

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
                    int listenPort = commandResult.Args[0] == "local" ? 5001 : int.Parse(commandResult.Args[0]);
                    await _server.Start(listenPort);
                    _peerDiscovery.Start(listenPort);
                    _heartbeatMonitor.Start();
                    _ui?.DisplaySystem($"Listening on port {listenPort}, peer discovery active");
                    break;
                case CommandType.Quit:
                    _peerDiscovery.Stop();
                    _heartbeatMonitor.Stop();
                    foreach (var c in _peerClients.Values) c.Disconnect();
                    running = false;
                    break;
                case CommandType.Peers:
                    var knownPeers = _peerDiscovery.GetKnownPeers().ToList();
                    if (knownPeers.Count == 0)
                    {
                        _ui?.DisplaySystem("No peers discovered yet.");
                        break;
                    }
                    Console.WriteLine($"--- Known Peers ({knownPeers.Count}) ---");
                    foreach (var p in knownPeers)
                    {
                        bool connected = _peerClients.ContainsKey(p.Id);
                        bool alive = connected && _heartbeatMonitor.IsAlive(p.Id);
                        string status = connected ? (alive ? "Connected" : "Degraded") : "Discovered";
                        Console.WriteLine($"  {p.Id} @ {p.Address}:{p.Port} [{status}] last seen {p.LastSeen:HH:mm:ss}");
                    }
                    Console.WriteLine("--- End of Peers ---");
                    break;
                case CommandType.Help:
                    _ui.ShowHelp();
                    break;
                case CommandType.History:
                    _history.ShowHistory();
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
                    string msgTarget = commandResult.Args![0];
                    string msgContent = string.Join(" ", commandResult.Args[1..]);
                    if (msgTarget.StartsWith('@'))
                    {
                        string targetPeerId = msgTarget[1..];
                        if (_peerClients.TryGetValue(targetPeerId, out Client? targetClient))
                            targetClient.Send(new Message { Sender = _username, Content = msgContent, TargetPeerId = targetPeerId });
                        else
                            _ui?.DisplaySystem($"Peer '{targetPeerId}' not connected. Use /peers to see available peers.");
                    }
                    else if (_client?.IsConnected == true)
                    {
                        var command = new Message { Sender = _username + await _client.getClientID(), Content = "/msg " + string.Join(" ", commandResult.Args[0..]) };
                        _client.Send(command);
                    }
                    break;
                default:
                    // Only send if connected to a server; otherwise this node is a pure relay
                    if (_client?.IsConnected == true)
                    {
                        var msg = new Message { Sender = _username + await _client.getClientID(), Content = commandResult.Message! };
                        _queue.EnqueueOutgoing(msg);
                        await BroadcastToAllPeersParallel(msg);
                    }
                    break;
            }
        }
        _cts.Cancel();
        _queue.CompleteAdding();
        receiveThread.Join();
        sendThread.Join();
        heartbeatThread.Join();
        _server?.Stop();
        _client?.Disconnect();

        Console.WriteLine("Goodbye!");
    }

    private static void HandlePeerConnectionFailed(string peerId)
    {
        _ui?.DisplaySystem($"Peer {peerId} lost connection — reconnecting");
        _heartbeatMonitor.StopMonitoring(peerId);
        if (_activePeers.TryGetValue(peerId, out Peer? peer))
        {
            var freshClient = new Client();
            WireClientEvents(freshClient, peerId);
            var policy = new ReconnectionPolicy(freshClient);
            WireReconnectEvents(policy, peerId);
            _reconnectPolicies[peerId] = policy;
            _ = Task.Run(() => policy.TryReconnect(peer));
        }
    }

    private static async Task ConnectToPeer(Peer peer)
    {
        var client = new Client();
        WireClientEvents(client, peer.Id);
        bool ok = await client.ConnectAsync(peer.Address!.ToString(), peer.Port);
        if (!ok) return;

        _peerClients[peer.Id] = client;
        _activePeers[peer.Id] = peer;
        peer.IsConnected = true;
        _heartbeatMonitor.StartMonitoring(peer.Id);

        var kx = new KeyExchange();
        _peerKeyExchanges[peer.Id] = kx;
        client.Send(new Message
        {
            Type = MessageType.KeyExchange,
            Sender = _peerDiscovery.LocalPeerId,
            PublicKey = kx.GetPublicKey()
        });

        var policy = new ReconnectionPolicy(client);
        WireReconnectEvents(policy, peer.Id);
        _reconnectPolicies[peer.Id] = policy;
        _ui?.DisplaySystem($"Connected to peer {peer.Id}");
    }

    private static void WireClientEvents(Client client, string peerId)
    {
        client.OnMessageReceived += message =>
        {
            if (message.Type == MessageType.Heartbeat)
            {
                _heartbeatMonitor.RecordHeartbeat(peerId);
            }
            else if (message.Type == MessageType.KeyExchange && message.PublicKey != null
                && _peerKeyExchanges.TryGetValue(peerId, out KeyExchange? kx))
            {
                kx.ReceivePublicKey(message.PublicKey);
                byte[] encryptedKey = kx.CreateEncryptedSessionKey();
                client.Send(new Message { Type = MessageType.SessionKey, Sender = _peerDiscovery.LocalPeerId, PublicKey = encryptedKey });
                kx.Complete();
                client.SessionKey = new AesEncryption(kx.SessionKey!);
                _ui?.DisplaySystem($"Key exchange complete with peer {peerId}");
            }
            else if (message.Type == MessageType.SessionKey && message.PublicKey != null
                && _peerKeyExchanges.TryGetValue(peerId, out KeyExchange? kx2)
                && !kx2.IsEstablished)
            {
                kx2.ReceiveEncryptedSessionKey(message.PublicKey);
                client.SessionKey = new AesEncryption(kx2.SessionKey!);
                _ui?.DisplaySystem($"Session key established with peer {peerId}");
            }
            else if (message.Type == MessageType.Text)
            {
                _queue.EnqueueIncoming(message);
            }
        };

        client.OnDisconnected += _ => HandlePeerConnectionFailed(peerId);
    }

    private static void WireReconnectEvents(ReconnectionPolicy policy, string peerId)
    {
        policy.OnReconnectAttempt += (id, attempt) =>
            _ui?.DisplaySystem($"Reconnecting to {id} (attempt {attempt}/5)");
        policy.OnReconnectSuccess += id =>
        {
            _heartbeatMonitor.StartMonitoring(id);
            _ui?.DisplaySystem($"Reconnected to {id}");
        };
        policy.OnReconnectFailed += id =>
        {
            _ui?.DisplaySystem($"Gave up reconnecting to {id} after 5 attempts");
            _peerClients.TryRemove(id, out _);
            _activePeers.TryRemove(id, out _);
        };
    }

    private static async Task BroadcastToAllPeersParallel(Message msg)
    {
        var snapshot = _peerClients.Values.ToList();
        if (snapshot.Count == 0) return;
        // TPL: parallel AES encryption + network send to each connected peer
        await Task.WhenAll(snapshot.Select(c => Task.Run(() => c.Send(msg))));
    }
}
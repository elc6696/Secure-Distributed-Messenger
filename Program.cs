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
using System.Net.Sockets;
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

    private static ConcurrentDictionary<string, Peer> _peers = new();

    private static ConcurrentDictionary<string, HashSet<string>> _roomMembers = new();
    private static HashSet<string> _myRooms = new();

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
            if (message.Type == MessageType.Text)
            {
                _queue.EnqueueIncoming(message);
            }
            if (message.Type == MessageType.RoomJoin && message.Room != null)
            {
                var members = _roomMembers.GetOrAdd(message.Room, _ => new HashSet<string>());
                lock (members) members.Add(message.Sender);
                _ui?.DisplaySystem($"{message.Sender} joined room #{message.Room}");
                return;
            }
            if (message.Type == MessageType.RoomLeave && message.Room != null)
            {
                if (_roomMembers.TryGetValue(message.Room, out var members))
                    lock (members) members.Remove(message.Sender);
                _ui?.DisplaySystem($"{message.Sender} left room #{message.Room}");
                return;
            }
            if (message.Type == MessageType.RoomMessage && message.Room != null)
            {
                if (_myRooms.Contains(message.Room))
                    _queue.EnqueueIncoming(message);
                return;
            }
        };

        _client.OnMessageReceived += message =>
        {
            if (message.Type == MessageType.Text)
                _queue.EnqueueIncoming(message);
        };

        _server.OnPeerConnected += peer => _ui?.DisplaySystem($"Peer connected from {peer.Address}:{peer.Port}");
        _server.OnPeerDisconnected += peer => _ui?.DisplaySystem($"Peer disconnected: {peer.Address}:{peer.Port}");

        _peerDiscovery.OnPeerLost += peer =>
        {
            _ui?.DisplaySystem($"Peer {peer.Id} lost (30s with no broadcast)");
            _heartbeatMonitor.StopMonitoring(peer.Id);
            if (_peers.TryRemove(peer.Id, out var removed))
            {
                removed.Outbound?.Disconnect();
                removed.Dispose();
            }
        };

        _heartbeatMonitor.OnConnectionFailed += peerId =>
        {
            if (_peers.TryGetValue(peerId, out var failedPeer)) HandlePeerConnectionFailed(failedPeer);
        };
        _heartbeatMonitor.OnHeartbeatReceived += peerId =>
        {
            if (_peers.TryGetValue(peerId, out var p) && p.ReconnectPolicy != null)
                p.ReconnectPolicy.ResetAttempts(peerId);
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
                    foreach (var peer in _peers.Values)
                        try { peer.Outbound?.Send(hb); } catch { }
                    try { _server?.Broadcast(hb); } catch { }
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
                    string targetIp;
                    int targetPort;
                    if(commandResult.Args[0] == "local") {
                        targetIp = "127.0.0.1";
                        targetPort = 5001;
                        await _client.ConnectAsync(targetIp, targetPort);
                    }else {
                        targetIp = commandResult.Args[0];
                        targetPort = int.Parse(commandResult.Args[1]);
                        await _client.ConnectAsync(targetIp, targetPort);
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
                    if (_client.IsConnected)
                    {
                        var connected = new Peer
                        {
                            Address = IPAddress.TryParse(targetIp, out var parsed) ? parsed : IPAddress.None,
                            Port = targetPort,
                            Outbound = _client,
                            KeyExchange = _keyExchange,
                            IsConnected = true,
                        };
                        WireClientEvents(connected);
                        _peers[connected.Id] = connected;
                        _heartbeatMonitor.StartMonitoring(connected.Id);
                        _ui?.DisplaySystem($"Manually connected as peer {connected.Id}");
                    }
                    break;
                case CommandType.Listen:
                    int listenPort = commandResult.Args[0] == "local" ? 5001 : int.Parse(commandResult.Args[0]);
                    await _server.Start(listenPort);
                    try
                    {
                        _peerDiscovery.Start(listenPort);
                        _ui?.DisplaySystem($"Listening on port {listenPort}, peer discovery active");
                    }
                    catch (SocketException ex)
                    {
                        _ui?.DisplaySystem($"Listening on port {listenPort}. Peer discovery disabled on this instance ({ex.Message}). Use /connect to populate peers manually.");
                    }
                    _heartbeatMonitor.Start();
                    break;
                case CommandType.Quit:
                    _peerDiscovery.Stop();
                    _heartbeatMonitor.Stop();
                    foreach (var p in _peers.Values) p.Outbound?.Disconnect();
                    running = false;
                    break;
                case CommandType.Peers:
                    var knownPeers = _peerDiscovery.GetKnownPeers().ToList();
                    var allPeerIds = knownPeers.Select(p => p.Id).Concat(_peers.Keys).Distinct().ToList();
                    if (allPeerIds.Count == 0)
                    {
                        _ui?.DisplaySystem("No peers discovered yet.");
                        break;
                    }
                    Console.WriteLine($"--- Known Peers ({allPeerIds.Count}) ---");
                    foreach (var id in allPeerIds)
                    {
                        var discovered = knownPeers.FirstOrDefault(p => p.Id == id);
                        _peers.TryGetValue(id, out var tracked);
                        var effective = tracked ?? discovered!;
                        bool connected = tracked?.Outbound != null;
                        bool alive = connected && _heartbeatMonitor.IsAlive(id);
                        string status = connected ? (alive ? "Connected" : "Degraded") : "Discovered";
                        Console.WriteLine($"  {effective.Id} @ {effective.Address}:{effective.Port} [{status}] last seen {effective.LastSeen:HH:mm:ss}");
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
                {
                    string roomName = commandResult.Args[0];
                    _roomMembers.GetOrAdd(roomName, _ => new HashSet<string>());
                    var members = _roomMembers[roomName];
                    lock (members) members.Add(_peerDiscovery.LocalPeerId);
                    _myRooms.Add(roomName);

                    var announce = new Message
                    {
                        Type = MessageType.RoomJoin,
                        Sender = _peerDiscovery.LocalPeerId,
                        Room = roomName
                    };
                    await BroadcastToAllPeersParallel(announce);
                    _ui?.DisplaySystem($"Room #{roomName} created and joined.");
                    break;
                }
                case CommandType.Rooms:
                {
                    if (_roomMembers.IsEmpty)
                    {
                        _ui?.DisplaySystem("No rooms known. Use /create #<name> to make one.");
                        break;
                    }
                    Console.WriteLine("--- Known Rooms ---");
                    foreach (var (name, members) in _roomMembers)
                    {
                        string tag = _myRooms.Contains(name) ? " [joined]" : "";
                        Console.WriteLine($"  #{name} ({members.Count} members){tag}");
                    }
                    Console.WriteLine("--- End of Rooms ---");
                    break;
                }
                case CommandType.Join:
                {
                    string roomName = commandResult.Args[0];
                    if (!_roomMembers.ContainsKey(roomName))
                    {
                        _ui?.DisplaySystem($"Room #{roomName} doesn't exist. Use /create {roomName} first.");
                        break;
                    }
                    var roomMembers = _roomMembers[roomName];
                    lock (roomMembers) roomMembers.Add(_peerDiscovery.LocalPeerId);
                    _myRooms.Add(roomName);  // <-- this is also missing from your current /join!

                    var announce = new Message
                    {
                        Type = MessageType.RoomJoin,
                        Sender = _peerDiscovery.LocalPeerId,
                        Room = roomName
                    };
                    _ui?.DisplaySystem($"Broadcasting to {_peers.Count} peers...");
                    await BroadcastToAllPeersParallel(announce);
                    _ui?.DisplaySystem($"Joined #{roomName}");
                    break;
                }
                case CommandType.Leave:
                {
                    string roomName = commandResult.Args[0];
                    if (!_myRooms.Contains(roomName))
                    {
                        _ui?.DisplaySystem($"You haven't joined #{roomName}.");
                        break;
                    }
                    _myRooms.Remove(roomName);
                    if (_roomMembers.TryGetValue(roomName, out var members))
                        lock (members) members.Remove(_peerDiscovery.LocalPeerId);

                    var announce = new Message
                    {
                        Type = MessageType.RoomLeave,
                        Sender = _peerDiscovery.LocalPeerId,
                        Room = roomName
                    };
                    await BroadcastToAllPeersParallel(announce);
                    _ui?.DisplaySystem($"Left #{roomName}");
                    break;
                }
                case CommandType.Message:
                {
                    string msgTarget = commandResult.Args![0];
                    string msgContent = string.Join(" ", commandResult.Args[1..]);
                    
                    if (msgTarget.StartsWith('#'))
                    {
                        string roomName = msgTarget[1..];
                        if (!_myRooms.Contains(roomName))
                        {
                            _ui?.DisplaySystem($"You haven't joined #{roomName}. Use /join {roomName} first.");
                            break;
                        }
                        var roomMsg = new Message
                        {
                            Type = MessageType.RoomMessage,
                            Sender = _username + await _client.getClientID(),
                            Room = roomName,
                            Content = msgContent
                        };
                        _queue.EnqueueIncoming(roomMsg);  // show locally too
                        await BroadcastToRoomPeers(roomName, roomMsg);
                    }
                    else if (msgTarget.StartsWith('@'))
                    {
                        string targetPeerId = msgTarget[1..];
                        if (_peers.TryGetValue(targetPeerId, out Peer? targetPeer) && targetPeer.Outbound != null)
                            targetPeer.Outbound.Send(new Message { Type = MessageType.Text, Sender = _username + await _client.getClientID(), Content = msgContent, TargetPeerId = targetPeerId });
                        else
                            _ui?.DisplaySystem($"Peer '{targetPeerId}' not connected. Use /peers to see available peers.");
                    }
                    else if (_client?.IsConnected == true)
                    {
                        var command = new Message { Sender = _username + await _client.getClientID(), Content = "/msg " + string.Join(" ", commandResult.Args[0..]) };
                        _client.Send(command);
                    }
                    break;
                }
                default:
                    {
                        var msg = new Message { Sender = _username + (_client?.IsConnected == true ? await _client.getClientID() : 0), Content = commandResult.Message! };
                        _queue.EnqueueOutgoing(msg);
                        _queue.EnqueueIncoming(msg);
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

    private static void HandlePeerConnectionFailed(Peer peer)
    {
        _ui?.DisplaySystem($"Peer {peer.Id} lost connection — reconnecting");
        _heartbeatMonitor.StopMonitoring(peer.Id);
        var freshClient = new Client();
        peer.Outbound = freshClient;
        WireClientEvents(peer);
        var policy = new ReconnectionPolicy(freshClient);
        peer.ReconnectPolicy = policy;
        WireReconnectEvents(policy, peer.Id);
        _ = Task.Run(() => policy.TryReconnect(peer));
    }

    private static async Task ConnectToPeer(Peer peer)
    {
        var client = new Client();
        peer.Outbound = client;
        WireClientEvents(peer);
        bool ok = await client.ConnectAsync(peer.Address!.ToString(), peer.Port);
        if (!ok) return;

        peer.IsConnected = true;
        _peers[peer.Id] = peer;
        _heartbeatMonitor.StartMonitoring(peer.Id);

        var kx = new KeyExchange();
        peer.KeyExchange = kx;
        client.Send(new Message
        {
            Type = MessageType.KeyExchange,
            Sender = _peerDiscovery.LocalPeerId,
            PublicKey = kx.GetPublicKey()
        });

        var policy = new ReconnectionPolicy(client);
        peer.ReconnectPolicy = policy;
        WireReconnectEvents(policy, peer.Id);
        _ui?.DisplaySystem($"Connected to peer {peer.Id}");
    }

    private static void WireClientEvents(Peer peer)
    {
        Client client = peer.Outbound!;
        client.OnMessageReceived += message =>
        {
            if (message.Type == MessageType.Heartbeat)
            {
                _heartbeatMonitor.RecordHeartbeat(peer.Id);
            }
            else if (message.Type == MessageType.KeyExchange && message.PublicKey != null
                && peer.KeyExchange != null)
            {
                KeyExchange kx = peer.KeyExchange;
                kx.ReceivePublicKey(message.PublicKey);
                byte[] encryptedKey = kx.CreateEncryptedSessionKey();
                client.Send(new Message { Type = MessageType.SessionKey, Sender = _peerDiscovery.LocalPeerId, PublicKey = encryptedKey });
                kx.Complete();
                client.SessionKey = new AesEncryption(kx.SessionKey!);
                _ui?.DisplaySystem($"Key exchange complete with peer {peer.Id}");
            }
            else if (message.Type == MessageType.SessionKey && message.PublicKey != null
                && peer.KeyExchange != null
                && !peer.KeyExchange.IsEstablished)
            {
                KeyExchange kx2 = peer.KeyExchange;
                kx2.ReceiveEncryptedSessionKey(message.PublicKey);
                client.SessionKey = new AesEncryption(kx2.SessionKey!);
                _ui?.DisplaySystem($"Session key established with peer {peer.Id}");
            }
            else if (message.Type == MessageType.Text)
            {
                _queue.EnqueueIncoming(message);
            }
            else if (message.Type == MessageType.RoomJoin && message.Room != null)
            {
                var members = _roomMembers.GetOrAdd(message.Room, _ => new HashSet<string>());
                lock (members) members.Add(message.Sender);
                _ui?.DisplaySystem($"{message.Sender} joined room #{message.Room}");
            }
            else if (message.Type == MessageType.RoomLeave && message.Room != null)
            {
                if (_roomMembers.TryGetValue(message.Room, out var members))
                    lock (members) members.Remove(message.Sender);
                _ui?.DisplaySystem($"{message.Sender} left room #{message.Room}");
            }
            else if (message.Type == MessageType.RoomMessage && message.Room != null)
            {
                // only display it if we're in that room
                if (_myRooms.Contains(message.Room))
                    _queue.EnqueueIncoming(message);
            }
        };

        client.OnDisconnected += _ => HandlePeerConnectionFailed(peer);
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
            _peers.TryRemove(id, out _);
        };
    }

    private static async Task BroadcastToAllPeersParallel(Message msg)
    {
        var snapshot = _peers.Values.Select(p => p.Outbound).Where(c => c != null).Cast<Client>().ToList();
        if (snapshot.Count == 0) return;
        // TPL: parallel AES encryption + network send to each connected peer
        await Task.WhenAll(snapshot.Select(c => Task.Run(() => c.Send(msg))));
    }

    private static async Task BroadcastToRoomPeers(string roomName, Message msg)
    {
        if (!_roomMembers.ContainsKey(roomName)) return;

        var targets = _peers.Values
            .Where(p => p.Outbound != null)
            .Select(p => p.Outbound!)
            .ToList();

        await Task.WhenAll(targets.Select(c => Task.Run(() => c.Send(msg))));
    }

}
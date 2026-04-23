// Ethan Chang
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
//
// KEY CONCEPTS USED IN THIS FILE:
//   - TcpListener: accepts incoming connections (see HINTS.md)
//   - Threads/Tasks: accept loop runs on background thread
//   - Events (Action<T>): notify Program.cs when things happen
//   - Locking: protect _clients list from concurrent access
//
// SPRINT PROGRESSION:
//   - Sprint 1: Basic server with client connections (this file)
//   - Sprint 2: Add encryption to message sending/receiving
//   - Sprint 3: Refactor to use Peer class for richer connection tracking,
//               add heartbeat monitoring and reconnection support
//

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SecureMessenger.Core;
using SecureMessenger.Security;

namespace SecureMessenger.Network;

/// <summary>
/// TCP server that listens for incoming connections.
///
/// In Sprint 1-2, we use simple client/server terminology:
/// - Server listens for incoming connections
/// - Connected parties are tracked as "clients"
///
/// In Sprint 3, this evolves to peer-to-peer:
/// - Connections become "peers" with richer state (see Peer.cs)
/// - Add peer discovery, heartbeats, and reconnection
/// </summary>
public class Server
{
    private TcpListener? _listener;
    private readonly ConcurrentDictionary<string, Peer> _peers = new();
    private static readonly List<string> _rooms = new();
    private readonly Dictionary<string, string> _roomToPeer = new();
    private static readonly object _clientsLock = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public event Action<Peer>? OnPeerConnected;
    public event Action<Peer>? OnPeerDisconnected;
    public event Action<Message>? OnMessageReceived;

    public int Port { get; private set; }
    public bool IsListening { get; private set; }

    private MessageSigner? MessageSigner { get; set; }
    public byte[] PublicKey { get; }
    public Server()
    {
        RSA rsa = RSA.Create(2048);
        MessageSigner = new MessageSigner(rsa);
        PublicKey = rsa.ExportSubjectPublicKeyInfo();
    }

    /// <summary>
    /// Start listening for incoming connections on the specified port.
    ///
    /// TODO: Implement the following:
    /// 1. Store the port number in the Port property
    /// 2. Create a new CancellationTokenSource
    /// 3. Create a TcpListener on IPAddress.Any and the specified port
    /// 4. Call Start() on the listener
    /// 5. Set IsListening to true
    /// 6. Start AcceptClientsAsync on a background Task
    /// 7. Print a message indicating the server is listening
    /// </summary>
    public async Task Start(int port)
    {
        Port = port; // Stores the port number to Port property

        _cancellationTokenSource = new CancellationTokenSource(); // Creates a new CancellationTokenSource for managing cancellation

        _listener = new TcpListener(IPAddress.Any, port); // Creates a TcpListener that listens on all network interfaces and the specified port

        _listener.Start(); // Starts the TcpListener to begin accepting incoming connection requests

        IsListening = true; // Sets the IsListening property to true, indicating that the server is now listening for connections

        _ = Task.Run(() => AcceptClientsAsync()); // Starts the AcceptClientsAsync method on a background Task to handle incoming connections asynchronously    

        Console.WriteLine($"Client listening on: {port}"); // Prints a message to the console indicating that a client has connected, along with the client's endpoint information

    }

    /// <summary>
    /// Main loop that accepts incoming connections.
    ///
    /// TODO: Implement the following:
    /// 1. Loop while cancellation is not requested
    /// 2. Use await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token)
    /// 3. Get the endpoint string from client.Client.RemoteEndPoint
    /// 4. Add the client to _clients (with proper locking)
    /// 5. Invoke OnClientConnected event with the endpoint
    /// 6. Start ReceiveFromClientAsync for this client on a background Task
    /// 7. Catch OperationCanceledException (normal shutdown - just break)
    /// 8. Catch other exceptions and log them
    /// </summary>
    private async Task AcceptClientsAsync()
    {
        while (!_cancellationTokenSource!.Token.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener!.AcceptTcpClientAsync(_cancellationTokenSource.Token);

                IPEndPoint? remoteEp = client.Client.RemoteEndPoint as IPEndPoint;
                Peer peer = new Peer
                {
                    Address = remoteEp?.Address ?? IPAddress.None,
                    Port = remoteEp?.Port ?? 0,
                    Client = client,
                    Stream = client.GetStream(),
                    IsConnected = true,
                };

                _peers[peer.Id] = peer;

                OnPeerConnected?.Invoke(peer);

                _ = Task.Run(() => ReceiveFromPeerAsync(peer));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Receive loop for a specific client - reads messages until disconnection.
    ///
    /// TODO: Implement the following:
    /// 1. Get the NetworkStream from the client
    /// 2. Create a 4-byte buffer for reading message length
    /// 3. Loop while not cancelled and client is connected:
    ///    a. Read 4 bytes for the message length (length-prefix framing)
    ///    b. If bytesRead == 0, client disconnected - break
    ///    c. Convert bytes to int using BitConverter.ToInt32
    ///    d. Validate length (> 0 and < 1,000,000)
    ///    e. Create a buffer for the message payload
    ///    f. Read the full payload (may require multiple reads)
    ///    g. Convert to string using Encoding.UTF8.GetString
    ///    h. Deserialize JSON to Message using JsonSerializer.Deserialize
    ///    i. Invoke OnMessageReceived event
    /// 4. Catch OperationCanceledException (normal shutdown)
    /// 5. Catch other exceptions and log them
    /// 6. In finally block, call DisconnectClient
    ///
    /// Sprint 3: This method will be enhanced to work with Peer objects
    /// instead of raw TcpClient, enabling richer connection state tracking.
    /// </summary>
    private async Task ReceiveFromPeerAsync(Peer peer)
    {
        NetworkStream stream = peer.Stream!;
        TcpClient client = peer.Client!;
        string endpoint = $"{peer.Address}:{peer.Port}";

        byte[] buffer = new byte[4];

        try
        {
            while (!_cancellationTokenSource!.Token.IsCancellationRequested && client.Connected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, 4, _cancellationTokenSource.Token);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    int messageLength = BitConverter.ToInt32(buffer, 0);

                    if (messageLength <= 0 || messageLength >= 1000000)
                    {
                        Console.WriteLine($"Invalid message length: {messageLength} from {endpoint}");
                        break;
                    }

                    byte[] payloadBuffer = new byte[messageLength];

                    int totalBytesRead = 0;

                    while (totalBytesRead < messageLength)
                    {
                        int read = await stream.ReadAsync(payloadBuffer, totalBytesRead, messageLength - totalBytesRead, _cancellationTokenSource.Token);

                        if (read == 0)
                        {
                            break;
                        }

                        totalBytesRead += read;
                    }

                    string jsonString = Encoding.UTF8.GetString(payloadBuffer);

                    Message? message = JsonSerializer.Deserialize<Message>(jsonString);

                    if (message != null)
                    {
                        peer.LastSeen = DateTime.Now;
                        if (peer.SessionAes != null && message.EncryptedContent != null)
                        {
                            message.Content = peer.SessionAes.Decrypt(message.EncryptedContent);
                        }

                        if (message.Signature != null && message.PublicKey != null && !string.IsNullOrEmpty(message.Content))
                        {
                            byte[] data = Encoding.UTF8.GetBytes(message.Content);

                            if (!MessageSigner!.VerifyData(data, message.Signature, message.PublicKey))
                            {
                                Console.WriteLine("Invalid signature from client — message dropped.");
                                continue;
                            }
                        }

                        if (!string.IsNullOrEmpty(message.Content) && message.Content.StartsWith("/create"))
                        {
                            string[] messagesplit = message.Content.Split(' ');
                            if (messagesplit.Length == 2 && !string.IsNullOrWhiteSpace(messagesplit[1]))
                            {
                                string roomName = messagesplit[1].Trim();
                                await CreateRoom(roomName);
                            }
                            else
                            {
                                var response = new Message { Sender = "Server", Content = "Usage: /create #room" };
                                SendToPeer(peer, response);
                            }
                        }
                        else if (!string.IsNullOrEmpty(message.Content) && message.Content.StartsWith("/rooms"))
                        {
                            List<string> rooms = GetRooms();
                            if (rooms.Count == 0)
                            {
                                var response = new Message { Sender = "Server", Content = "No rooms" };
                                SendToPeer(peer, response);
                            }
                            else
                            {
                                string roomlist = string.Join(", ", rooms);
                                var response = new Message { Sender = "Server", Content = roomlist };
                                SendToPeer(peer, response);
                            }
                        }
                        else if (!string.IsNullOrEmpty(message.Content) && message.Content.StartsWith("/join"))
                        {
                            string[] messagesplit = message.Content.Split(' ');
                            if (messagesplit.Length == 2 && !string.IsNullOrWhiteSpace(messagesplit[1]))
                            {
                                await AddToRoom(peer, messagesplit[1].Trim());
                            }
                            else
                            {
                                var response = new Message { Sender = "Server", Content = "Usage: /join #room" };
                                SendToPeer(peer, response);
                            }
                        }
                        else if (!string.IsNullOrEmpty(message.Content) && message.Content.StartsWith("/leave"))
                        {
                            string[] messagesplit = message.Content.Split(' ');
                            if (messagesplit.Length == 2 && !string.IsNullOrWhiteSpace(messagesplit[1]))
                            {
                                await RemoveFromRoom(peer, messagesplit[1].Trim());
                            }
                            else
                            {
                                var response = new Message { Sender = "Server", Content = "Usage: /leave #room" };
                                SendToPeer(peer, response);
                            }
                        }
                        else if (!string.IsNullOrEmpty(message.Content) && message.Content.StartsWith("/msg"))
                        {
                            string[] messagesplit = message.Content.Split(' ');
                            var tosend = new Message { Sender = message.Sender, Content = string.Join(" ", messagesplit[2..]) };
                            BroadcastToRoom(tosend, messagesplit[1]);
                            OnMessageReceived?.Invoke(tosend);
                        }
                        else
                        {
                            OnMessageReceived?.Invoke(message);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving from client {endpoint}: {ex.Message}");
                }
            }
        }
        finally
        {
            DisconnectPeer(peer);
        }
    }

    /// <summary>
    /// Send a message to a single specific client.
    /// </summary>
    private void SendToPeer(Peer peer, Message message)
    {
        try
        {
            if (!string.IsNullOrEmpty(message.Content) && MessageSigner != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(message.Content);
                message.Signature = MessageSigner.SignData(data);
                message.PublicKey = PublicKey;
            }

            if (peer.SessionAes != null && !string.IsNullOrEmpty(message.Content))
            {
                message.EncryptedContent = peer.SessionAes.Encrypt(message.Content);
                message.Content = string.Empty;
            }
            string json = JsonSerializer.Serialize(message);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] lengthPrefix = BitConverter.GetBytes(bytes.Length);

            NetworkStream stream = peer.Stream ?? peer.Client!.GetStream();
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending to client: {ex.Message}");
        }
    }


    public Task CreateRoom(string roomName)
    {
        lock(_clientsLock)
        {
            if (!_rooms.Contains(roomName))
            {
                _rooms.Add(roomName);
                Console.WriteLine($"Room {roomName} created.");
            }
            else
            {
                Console.WriteLine($"Room {roomName} already exists.");
            }
        }
        return Task.CompletedTask;
    }


    public List<string> GetRooms()
    {
        lock (_clientsLock)
        {
            return new List<string>(_rooms);
        }
    }

    private Task AddToRoom(Peer peer, string roomName)
    {
        lock(_clientsLock)
        {
            if (_roomToPeer.ContainsKey(peer.Id))
            {
                Message response = new() { Sender = "Server", Content = "You are already in a room" };
                SendToPeer(peer, response);
            }
            else if (_rooms.Contains(roomName))
            {
                _roomToPeer.TryAdd(peer.Id, roomName);
                Message response = new() { Sender = "Server", Content = $"Successfully added to room {roomName}" };
                SendToPeer(peer, response);
            }
            else
            {
                Message response = new() { Sender = "Server", Content = "That room does not exist" };
                SendToPeer(peer, response);
            }
        }
        return Task.CompletedTask;
    }

    private Task RemoveFromRoom(Peer peer, string roomName)
    {
        lock(_clientsLock)
        {
            _roomToPeer.TryGetValue(peer.Id, out string? room);
            if (room == roomName)
            {
                _roomToPeer.Remove(peer.Id);
                Message response = new() { Sender = "Server", Content = $"Successfully removed from room {roomName}" };
                SendToPeer(peer, response);
            }
            else
            {
                Message response = new() { Sender = "Server", Content = "You are not in this room" };
                SendToPeer(peer, response);
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clean up a disconnected client.
    ///
    /// TODO: Implement the following:
    /// 1. Remove the client from _clients (with proper locking)
    /// 2. Close the client connection
    /// 3. Invoke OnClientDisconnected event
    ///
    /// Sprint 3: This will be refactored to DisconnectPeer(Peer peer)
    /// to handle richer peer state and trigger reconnection attempts.
    /// </summary>
    private void DisconnectPeer(Peer peer)
    {
        _peers.TryRemove(peer.Id, out _);
        lock (_clientsLock)
        {
            _roomToPeer.Remove(peer.Id);
        }
        peer.Dispose();
        OnPeerDisconnected?.Invoke(peer);
    }

    /// <summary>
    /// Send a message to all connected clients (broadcast).
    ///
    /// TODO: Implement the following:
    /// 1. Serialize the message to JSON using JsonSerializer.Serialize
    /// 2. Convert to bytes using Encoding.UTF8.GetBytes
    /// 3. Create a 4-byte length prefix using BitConverter.GetBytes
    /// 4. Get a copy of _clients (with proper locking)
    /// 5. For each connected client:
    ///    a. Get the NetworkStream
    ///    b. Write the length prefix (4 bytes)
    ///    c. Write the payload
    /// 6. Handle exceptions for individual clients (don't stop broadcast)
    /// </summary>
    public void Broadcast(Message message)
    {
        foreach (var peer in _peers.Values)
        {
            SendToPeer(peer, message);
        }
    }


    /// <summary>
    /// Send a message to all connected clients in the given room.
    /// </summary>
    public void BroadcastToRoom(Message message, string roomName)
    {
        foreach (var peer in _peers.Values)
        {
            if (_roomToPeer.TryGetValue(peer.Id, out string? room) && room == roomName)
            {
                SendToPeer(peer, message);
            }
        }
    }

    /// <summary>
    /// Stop the server and close all connections.
    ///
    /// TODO: Implement the following:
    /// 1. Cancel the cancellation token
    /// 2. Stop the listener
    /// 3. Set IsListening to false
    /// 4. Close all clients (with proper locking)
    /// 5. Clear the _clients list
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _listener?.Stop();
        IsListening = false;
        foreach (var peer in _peers.Values)
        {
            peer.Dispose();
        }
        _peers.Clear();
    }

    /// <summary>
    /// Get the count of currently connected clients.
    /// </summary>
    public int PeerCount
    {
        get
        {
            return _peers.Count;
        }
    }
}
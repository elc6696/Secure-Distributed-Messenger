// Ethan Chang
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
//
// KEY CONCEPTS USED IN THIS FILE:
//   - TcpClient: initiates outgoing connections (see HINTS.md)
//   - async/await: ConnectAsync, ReadAsync, WriteAsync
//   - NetworkStream: read/write bytes over network
//   - Length-prefix framing: 4-byte length + JSON payload
//
// CLIENT vs SERVER:
//   - Server (Server.cs) waits for others to connect TO it
//   - Client (this file) connects TO other servers
//   - Test: Terminal 1 runs /listen, Terminal 2 runs /connect
//
// SPRINT PROGRESSION:
//   - Sprint 1: Basic client for outgoing connections (this file)
//   - Sprint 2: Add encryption to message sending/receiving
//   - Sprint 3: Refactor to track connections as Peer objects,
//               integrate with PeerDiscovery for automatic connections
//

using System.Linq.Expressions;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SecureMessenger.Core;
using SecureMessenger.Security;
namespace SecureMessenger.Network;

/// <summary>
/// TCP client that connects to a server and handles message sending/receiving.
///
/// In Sprint 1-2, this handles a single outgoing connection.
///
/// In Sprint 3, connections are upgraded to "peers" with:
/// - Richer state tracking (see Peer.cs)
/// - Automatic reconnection on disconnect
/// - Integration with PeerDiscovery
/// </summary>
public class Client
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cancellationTokenSource;
    private string _serverEndpoint = "";

    public event Action<string>? OnConnected;
    public event Action<string>? OnDisconnected;
    public event Action<Message>? OnMessageReceived;

    private int _clientID;
    public AesEncryption? SessionKey{ get; set;}
    public bool IsConnected => _client?.Connected ?? false;

    private MessageSigner? MessageSigner { get; set; }
    public byte[] PublicKey { get; }

    public Client()
    {
        RSA rsa = RSA.Create(2048);
        MessageSigner = new MessageSigner(rsa);
        PublicKey = rsa.ExportSubjectPublicKeyInfo();
    }

    /// <summary>
    /// Connect to a server at the specified address and port.
    ///
    /// TODO: Implement the following:
    /// 1. Create a new CancellationTokenSource
    /// 2. Create a new TcpClient
    /// 3. Connect asynchronously using await _client.ConnectAsync(host, port)
    /// 4. Get the NetworkStream from the client
    /// 5. Store the endpoint string (e.g., "192.168.1.5:5000")
    /// 6. Invoke OnConnected event
    /// 7. Start ReceiveAsync on a background Task
    /// 8. Return true on success
    /// 9. Catch exceptions, log error, and return false
    ///
    /// Sprint 3: This will be enhanced to create a Peer object and
    /// register it with the connection manager for reconnection support.
    /// </summary>
    public async Task<bool> ConnectAsync(string host, int port)
    {
        _cancellationTokenSource = new CancellationTokenSource(); // Create a new CancellationTokenSource

        _client = new TcpClient(); // Create a new TcpClient

        try
        {
            await _client.ConnectAsync(host, port, _cancellationTokenSource.Token); // Asynchronously connect to the specified host and port
            _stream = _client.GetStream(); // Get the NetworkStream from the client
            _serverEndpoint = $"{host}:{port}"; // Store the endpoint string (e.g
            OnConnected?.Invoke(_serverEndpoint); // Invoke the OnConnected event with the server endpoint
            _ = Task.Run(ReceiveAsync); // Start the ReceiveAsync method on a background Task
            return true; // Return true on successful connection
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to server: {ex.Message}");
            return false;
        }
    }
    /// <summary>
    /// Receive loop - runs on background thread.
    /// Uses length-prefix framing: 4 bytes for length, then JSON payload.
    ///
    /// TODO: Implement the following:
    /// 1. Create a 4-byte buffer for reading message length
    /// 2. Loop while not cancelled and client is connected:
    ///    a. Read 4 bytes for the message length
    ///    b. If bytesRead == 0, server disconnected - break
    ///    c. Convert bytes to int using BitConverter.ToInt32
    ///    d. Validate length (> 0 and < 1,000,000)
    ///    e. Create a buffer for the message payload
    ///    f. Read the full payload (may require multiple reads)
    ///    g. Convert to string using Encoding.UTF8.GetString
    ///    h. Deserialize JSON to Message using JsonSerializer.Deserialize
    ///    i. Invoke OnMessageReceived event
    /// 3. Catch OperationCanceledException (normal shutdown)
    /// 4. Catch other exceptions and log them
    /// 5. In finally block, invoke OnDisconnected event
    ///
    /// Sprint 3: Will be enhanced to update Peer.LastSeen and
    /// trigger reconnection attempts on unexpected disconnect.
    /// </summary>
    private async Task ReceiveAsync()
    {
        try
        {
            byte[] lengthBuffer = new byte[4]; // Buffer for reading message length
            while (!_cancellationTokenSource!.Token.IsCancellationRequested && _client?.Connected == true)
            {
                int totalBytesRead = 0;

                while (totalBytesRead < 4)
                {
                    int bytesRead = await _stream!.ReadAsync(lengthBuffer, totalBytesRead, 4 - totalBytesRead, _cancellationTokenSource.Token);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server disconnected.");
                        return; // Server disconnected
                    }
                    totalBytesRead += bytesRead;
                }

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0); // Convert bytes to int for message length

                if (messageLength <= 0 || messageLength > 1_000_000)
                {
                    Console.WriteLine($"Invalid message length: {messageLength}");
                    return; // Invalid length, close connection
                }

                byte[] payloadBuffer = new byte[messageLength];

                totalBytesRead = 0;
                while (totalBytesRead < messageLength)
                {
                    int bytesRead = await _stream!.ReadAsync(payloadBuffer, totalBytesRead, messageLength - totalBytesRead, _cancellationTokenSource.Token);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server disconnected during payload read.");
                        return; // Server disconnected
                    }
                    totalBytesRead += bytesRead;
                }

                string payload = Encoding.UTF8.GetString(payloadBuffer);

                Message? message = JsonSerializer.Deserialize<Message>(payload);

                if (message != null)
                {
                    if (SessionKey != null && message.EncryptedContent != null)
                    {
                        Console.WriteLine($"[DEBUG] Encrypted bytes: {BitConverter.ToString(message.EncryptedContent)}");
                        message.Content = SessionKey.Decrypt(message.EncryptedContent);
                        Console.WriteLine($"[DEBUG] Decrypted: \"{message.Content}\"");
                    }

                    if (message.Signature != null && message.PublicKey != null) // Signature verification
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message.Content);

                        if (!MessageSigner!.VerifyData(data, message.Signature, message.PublicKey))
                        {
                            Console.WriteLine("Invalid signature");
                            continue;
                        }
                    }

                    OnMessageReceived?.Invoke(message); // Invoke the OnMessageReceived event with the deserialized message

                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, do nothing
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in receive loop: {ex.Message}");
        }
        finally
        {
            OnDisconnected?.Invoke(_serverEndpoint); // Invoke the OnDisconnected event with the server endpoint
        }
    }

    /// <summary>
    /// Send a message to the server.
    ///
    /// TODO: Implement the following:
    /// 1. Check if connected - if not, log error and return
    /// 2. Serialize the message to JSON using JsonSerializer.Serialize
    /// 3. Convert to bytes using Encoding.UTF8.GetBytes
    /// 4. Create a 4-byte length prefix using BitConverter.GetBytes
    /// 5. Write the length prefix to the stream
    /// 6. Write the payload to the stream
    /// 7. Handle exceptions
    ///
    /// Sprint 2: Add encryption before serialization
    /// Sprint 3: Will send to Peer instead of raw stream
    /// </summary>
    public void Send(Message message)
    {

        if (!IsConnected)
        {
            Console.WriteLine("Cannot send message: not connected to server.");
            return;
        }

        try
        {

            if (!string.IsNullOrEmpty(message.Content) && MessageSigner != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(message.Content);
                message.Signature = MessageSigner.SignData(data);
                message.PublicKey = PublicKey;

                Console.WriteLine($"[DEBUG] Signed message: \"{message.Content}\"");
            }

                if (SessionKey != null && !string.IsNullOrEmpty(message.Content)) // For encryption
            {
                message.EncryptedContent = SessionKey.Encrypt(message.Content);
                message.Content = string.Empty;

            }
            string json = JsonSerializer.Serialize(message); // Serialize the message to JSON
            byte[] payloadBytes = Encoding.UTF8.GetBytes(json); // Convert JSON string to bytes
            byte[] lengthPrefix = BitConverter.GetBytes(payloadBytes.Length); // Create a 4-byte length prefix

            _stream!.Write(lengthPrefix, 0, lengthPrefix.Length); // Write the length prefix to the stream
            _stream.Write(payloadBytes, 0, payloadBytes.Length); // Write the payload to the stream
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");

        }

    }

    /// <summary>
    /// Disconnect from the server.
    ///
    /// TODO: Implement the following:
    /// 1. Cancel the cancellation token
    /// 2. Close the stream
    /// 3. Close the client
    /// </summary>
    public void Disconnect()
    {
        _cancellationTokenSource?.Cancel(); // Cancel the cancellation token
        _stream?.Close(); // Close the stream
        _client?.Close(); // Close the client

    }
    public async Task<int> getClientID()
    {
        return await Task.FromResult(this._clientID);
    }

    public async void setClientID(int id)
    {
        this._clientID = id;
    }
}
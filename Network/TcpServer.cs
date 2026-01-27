// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
//
// KEY CONCEPTS USED IN THIS FILE:
//   - TcpListener: accepts incoming connections (see HINTS.md)
//   - Threads: listen loop runs on background thread
//   - Events (Action<T>): notify Program.cs when things happen
//   - Locking: protect _connectedPeers from concurrent access
//

using System.Net;
using System.Net.Sockets;
using SecureMessenger.Core;

namespace SecureMessenger.Network;

/// <summary>
/// TCP server that listens for incoming peer connections.
/// Each peer runs both a server (to accept connections) and client (to initiate connections).
/// </summary>
public class TcpServer
{
    private TcpListener? _listener;
    private readonly List<Peer> _connectedPeers = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Thread? _listenThread;

    // Events: invoke these with OnXxx?.Invoke(...) when something happens
    // Program.cs subscribes with: server.OnXxx += (args) => { ... };
    public event Action<Peer>? OnPeerConnected;
    public event Action<Peer>? OnPeerDisconnected;
    public event Action<Peer, Message>? OnMessageReceived;

    public int Port { get; private set; }
    public bool IsListening { get; private set; }

    /// <summary>
    /// Start listening for incoming connections on the specified port.
    ///
    /// TODO: Implement the following:
    /// 1. Store the port number
    /// 2. Create a new CancellationTokenSource
    /// 3. Create and start a TcpListener on IPAddress.Any and the specified port
    /// 4. Set IsListening to true
    /// 5. Create and start a new Thread running ListenLoop
    /// 6. Print a message indicating the server is listening
    /// </summary>
    public void Start(int port)
    {
        throw new NotImplementedException("Implement Start() - see TODO in comments above");
    }

    /// <summary>
    /// Main loop that accepts incoming connections.
    ///
    /// TODO: Implement the following:
    /// 1. Loop while cancellation is not requested
    /// 2. Check if a connection is pending using _listener.Pending()
    /// 3. If pending, accept the connection with AcceptTcpClient()
    /// 4. Call HandleNewConnection with the new client
    /// 5. If not pending, sleep briefly (e.g., 100ms) to avoid busy-waiting
    /// 6. Handle SocketException and IOException appropriately
    /// </summary>
    private void ListenLoop()
    {
        throw new NotImplementedException("Implement ListenLoop() - see TODO in comments above");
    }

    /// <summary>
    /// Handle a new incoming connection by creating a Peer and starting its receive thread.
    ///
    /// TODO: Implement the following:
    /// 1. Create a new Peer object with:
    ///    - Client = the TcpClient
    ///    - Stream = client.GetStream()
    ///    - Address = extracted from client.Client.RemoteEndPoint
    ///    - Port = extracted from client.Client.RemoteEndPoint
    ///    - IsConnected = true
    /// 2. Add the peer to _connectedPeers (with proper locking)
    /// 3. Invoke OnPeerConnected event
    /// 4. Create and start a new Thread running ReceiveLoop for this peer
    /// </summary>
    private void HandleNewConnection(TcpClient client)
    {
        throw new NotImplementedException("Implement HandleNewConnection() - see TODO in comments above");
    }

    /// <summary>
    /// Receive loop for a specific peer - reads messages until disconnection.
    ///
    /// TODO: Implement the following:
    /// 1. Create a StreamReader from the peer's stream
    /// 2. Loop while peer is connected and cancellation not requested
    /// 3. Read a line from the stream (ReadLine blocks until data available)
    /// 4. If line is null, the connection was closed - break the loop
    /// 5. Create a Message object with the received content
    /// 6. Invoke OnMessageReceived event with the peer and message
    /// 7. Handle IOException (connection lost)
    /// 8. In finally block, call DisconnectPeer
    /// </summary>
    private void ReceiveLoop(Peer peer)
    {
        throw new NotImplementedException("Implement ReceiveLoop() - see TODO in comments above");
    }

    /// <summary>
    /// Clean up a disconnected peer.
    ///
    /// TODO: Implement the following:
    /// 1. Set peer.IsConnected to false
    /// 2. Dispose the peer's Client and Stream
    /// 3. Remove the peer from _connectedPeers (with proper locking)
    /// 4. Invoke OnPeerDisconnected event
    /// </summary>
    private void DisconnectPeer(Peer peer)
    {
        throw new NotImplementedException("Implement DisconnectPeer() - see TODO in comments above");
    }

    /// <summary>
    /// Stop the server and close all connections.
    ///
    /// TODO: Implement the following:
    /// 1. Cancel the cancellation token
    /// 2. Stop the listener
    /// 3. Set IsListening to false
    /// 4. Disconnect all connected peers (with proper locking)
    /// 5. Wait for the listen thread to finish (with timeout)
    /// </summary>
    public void Stop()
    {
        throw new NotImplementedException("Implement Stop() - see TODO in comments above");
    }

    /// <summary>
    /// Get a list of currently connected peers.
    /// Remember to use proper locking when accessing _connectedPeers.
    /// </summary>
    public IEnumerable<Peer> GetConnectedPeers()
    {
        lock (_connectedPeers)
        {
            return _connectedPeers.ToList();
        }
    }
}

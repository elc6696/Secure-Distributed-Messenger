// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
//
// KEY CONCEPTS USED IN THIS FILE:
//   - TcpClient: initiates outgoing connections (see HINTS.md)
//   - async/await: ConnectAsync, ReadLineAsync, WriteLineAsync
//   - StreamReader/StreamWriter: read/write text over network
//   - Locking: protect _connections dictionary
//
// CLIENT vs SERVER:
//   - TcpServer waits for others to connect TO it
//   - TcpClientHandler connects TO other servers
//   - Test: Terminal 1 runs /listen, Terminal 2 runs /connect
//

using System.Net;
using System.Net.Sockets;
using SecureMessenger.Core;

namespace SecureMessenger.Network;

/// <summary>
/// Handles outgoing TCP connections to other peers.
/// </summary>
public class TcpClientHandler
{
    private readonly Dictionary<string, Peer> _connections = new();
    private readonly object _lock = new();

    public event Action<Peer>? OnConnected;
    public event Action<Peer>? OnDisconnected;
    public event Action<Peer, Message>? OnMessageReceived;

    /// <summary>
    /// Connect to a peer at the specified address and port.
    ///
    /// TODO: Implement the following:
    /// 1. Create a new TcpClient
    /// 2. Connect asynchronously to the host and port
    /// 3. Create a Peer object with:
    ///    - Client = the TcpClient
    ///    - Stream = client.GetStream()
    ///    - Address = parsed from host string
    ///    - Port = the port parameter
    ///    - IsConnected = true
    /// 4. Add to _connections dictionary (with proper locking)
    /// 5. Invoke OnConnected event
    /// 6. Start a background task running ReceiveLoop for this peer
    /// 7. Return true on success
    /// 8. Handle SocketException - print error and return false
    /// </summary>
    public async Task<bool> ConnectAsync(string host, int port)
    {
        throw new NotImplementedException("Implement ConnectAsync() - see TODO in comments above");
    }

    /// <summary>
    /// Receive loop for a connected peer - reads messages until disconnection.
    ///
    /// TODO: Implement the following:
    /// 1. Create a StreamReader from the peer's stream
    /// 2. Loop while peer is connected
    /// 3. Read a line asynchronously (ReadLineAsync)
    /// 4. If line is null, connection was closed - break
    /// 5. Create a Message object with the received content
    /// 6. Invoke OnMessageReceived event
    /// 7. Handle IOException (connection lost)
    /// 8. In finally block, call Disconnect
    /// </summary>
    private async Task ReceiveLoop(Peer peer)
    {
        throw new NotImplementedException("Implement ReceiveLoop() - see TODO in comments above");
    }

    /// <summary>
    /// Send a message to a specific peer.
    ///
    /// TODO: Implement the following:
    /// 1. Look up the peer in _connections by peerId (with proper locking)
    /// 2. If peer exists and is connected with a valid stream:
    ///    - Create a StreamWriter (with leaveOpen: true)
    ///    - Write the message line asynchronously
    ///    - Flush the writer
    /// </summary>
    public async Task SendAsync(string peerId, string message)
    {
        throw new NotImplementedException("Implement SendAsync() - see TODO in comments above");
    }

    /// <summary>
    /// Broadcast a message to all connected peers.
    ///
    /// TODO: Implement the following:
    /// 1. Get a copy of all peers (with proper locking)
    /// 2. Loop through each peer and call SendAsync
    /// </summary>
    public async Task BroadcastAsync(string message)
    {
        throw new NotImplementedException("Implement BroadcastAsync() - see TODO in comments above");
    }

    /// <summary>
    /// Disconnect from a peer.
    ///
    /// TODO: Implement the following:
    /// 1. Remove the peer from _connections (with proper locking)
    /// 2. If peer was found:
    ///    - Set IsConnected to false
    ///    - Dispose the Client and Stream
    ///    - Invoke OnDisconnected event
    /// </summary>
    public void Disconnect(string peerId)
    {
        throw new NotImplementedException("Implement Disconnect() - see TODO in comments above");
    }

    /// <summary>
    /// Get all currently connected peers.
    /// Remember to use proper locking when accessing _connections.
    /// </summary>
    public IEnumerable<Peer> GetConnectedPeers()
    {
        lock (_lock)
        {
            return _connections.Values.ToList();
        }
    }
}

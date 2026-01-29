// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 3: P2P & Advanced Features
// Due: Week 14 | Work on: Weeks 11-13
//
// NOTE: This file is NOT used in Sprint 1 or Sprint 2!
//
// In Sprint 1-2, connections are tracked as simple TcpClient objects
// in Server.cs and Client.cs. The terminology is "client/server".
//
// In Sprint 3, you'll refactor to use this Peer class to enable:
// - Richer connection state (LastSeen, reconnection status)
// - Peer discovery via UDP broadcast
// - Heartbeat monitoring
// - Automatic reconnection
//
// When you reach Sprint 3, update Server.cs and Client.cs to use
// List<Peer> instead of List<TcpClient>, and update event signatures
// to pass Peer objects instead of endpoint strings.
//

using System.Net;
using System.Net.Sockets;

namespace SecureMessenger.Core;

/// <summary>
/// Represents a connected peer in the network.
///
/// Sprint 3 introduces the "peer" concept for true P2P networking:
/// - Each peer can both send and receive messages
/// - Peers are discovered automatically via UDP broadcast
/// - Connections are monitored with heartbeats
/// - Disconnected peers trigger automatic reconnection attempts
///
/// This replaces the simple client/server model from Sprint 1-2
/// with a more sophisticated peer-to-peer architecture.
/// </summary>
public class Peer
{
    /// <summary>Unique identifier for this peer (first 8 chars of GUID)</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString()[..8];

    /// <summary>Display name of the peer</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>IP address of the peer</summary>
    public IPAddress? Address { get; set; }

    /// <summary>TCP port the peer is listening on</summary>
    public int Port { get; set; }

    /// <summary>Last time we received data from this peer (for heartbeat timeout)</summary>
    public DateTime LastSeen { get; set; } = DateTime.Now;

    /// <summary>Whether we currently have an active connection to this peer</summary>
    public bool IsConnected { get; set; }

    // Network connection handles
    public TcpClient? Client { get; set; }
    public NetworkStream? Stream { get; set; }

    // Convenience wrappers for text-based messaging
    // These simplify reading/writing JSON lines over the network
    public StreamReader? Reader { get; set; }
    public StreamWriter? Writer { get; set; }

    // Sprint 2 security: Per-peer encryption keys
    // These are negotiated during the key exchange handshake
    public byte[]? AesKey { get; set; }
    public byte[]? PublicKey { get; set; }

    // Sprint 3: Reconnection tracking
    public int ReconnectAttempts { get; set; }
    public DateTime? LastReconnectAttempt { get; set; }

    public override string ToString()
    {
        var status = IsConnected ? "Connected" : "Disconnected";
        return $"{Name} ({Address}:{Port}) - {status}";
    }

    /// <summary>
    /// Clean up all resources associated with this peer connection.
    /// Call this when disconnecting a peer.
    /// </summary>
    public void Dispose()
    {
        IsConnected = false;
        try { Reader?.Dispose(); } catch { }
        try { Writer?.Dispose(); } catch { }
        try { Stream?.Dispose(); } catch { }
        try { Client?.Dispose(); } catch { }
    }
}

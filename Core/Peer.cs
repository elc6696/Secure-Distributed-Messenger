// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// PROVIDED - No implementation required
// This data model is complete. You may add properties if needed.
//

using System.Net;
using System.Net.Sockets;

namespace SecureMessenger.Core;

/// <summary>
/// Represents a connected peer in the network
/// </summary>
public class Peer
{
    public string Id { get; set; } = Guid.NewGuid().ToString()[..8];
    public string Name { get; set; } = string.Empty;
    public IPAddress? Address { get; set; }
    public int Port { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.Now;
    public bool IsConnected { get; set; }

    // Network connection
    public TcpClient? Client { get; set; }
    public NetworkStream? Stream { get; set; }

    // Sprint 2: Per-session encryption keys
    public byte[]? AesKey { get; set; }
    public byte[]? PublicKey { get; set; }

    public override string ToString()
    {
        var status = IsConnected ? "Connected" : "Disconnected";
        return $"{Name} ({Address}:{Port}) - {status}";
    }
}

// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 3: P2P & Advanced Features
// Due: Week 14 | Work on: Weeks 11-13
//

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using SecureMessenger.Core;

namespace SecureMessenger.Network;

/// <summary>
/// Sprint 3: UDP-based peer discovery using broadcast.
/// Broadcasts presence and listens for other peers on the local network.
///
/// Discovery Protocol:
/// - Message format: "PEER:{peerId}:{tcpPort}"
/// - Example: "PEER:abc12345:5000"
/// - Broadcast every 5 seconds
/// - Peers timeout after 30 seconds of no broadcasts
/// </summary>
public class PeerDiscovery
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ConcurrentDictionary<string, Peer> _knownPeers = new();
    private readonly int _broadcastPort = 5001;
    private Thread? _listenThread;
    private Thread? _broadcastThread;

    public event Action<Peer>? OnPeerDiscovered;
    public event Action<Peer>? OnPeerLost;

    public int TcpPort { get; private set; }
    public string LocalPeerId { get; } = Guid.NewGuid().ToString()[..8];

    /// <summary>
    /// Start broadcasting presence and listening for other peers.
    ///
    /// TODO: Implement the following:
    /// 1. Store the TCP port
    /// 2. Create a new CancellationTokenSource
    /// 3. Create a UdpClient on the broadcast port
    /// 4. Enable broadcast on the UDP client
    /// 5. Create and start a thread for ListenLoop
    /// 6. Create and start a thread for BroadcastLoop
    /// 7. Start a background task for TimeoutCheckLoop
    /// </summary>
    public void Start(int tcpPort)
    {
        throw new NotImplementedException("Implement Start() - see TODO in comments above");
    }

    /// <summary>
    /// Periodically broadcast our presence to the network.
    ///
    /// TODO: Implement the following:
    /// 1. Create an IPEndPoint for broadcast (IPAddress.Broadcast, _broadcastPort)
    /// 2. Loop while cancellation not requested:
    ///    a. Create discovery message: "PEER:{LocalPeerId}:{TcpPort}"
    ///    b. Convert to bytes using UTF8 encoding
    ///    c. Send via UDP client to the broadcast endpoint
    ///    d. Handle SocketException (ignore broadcast errors)
    ///    e. Sleep for 5 seconds between broadcasts
    /// </summary>
    private void BroadcastLoop()
    {
        throw new NotImplementedException("Implement BroadcastLoop() - see TODO in comments above");
    }

    /// <summary>
    /// Listen for peer broadcast messages.
    ///
    /// TODO: Implement the following:
    /// 1. Create an IPEndPoint for receiving (IPAddress.Any, _broadcastPort)
    /// 2. Loop while cancellation not requested:
    ///    a. Receive data from UDP client (blocks until data available)
    ///    b. Convert bytes to string using UTF8 encoding
    ///    c. If message starts with "PEER:", call ProcessDiscoveryMessage
    ///    d. Handle SocketException (ignore receive errors)
    /// </summary>
    private void ListenLoop()
    {
        throw new NotImplementedException("Implement ListenLoop() - see TODO in comments above");
    }

    /// <summary>
    /// Parse a discovery message and add/update the peer.
    ///
    /// TODO: Implement the following:
    /// 1. Split the message by ':' - format is "PEER:peerId:port"
    /// 2. Validate we have at least 3 parts
    /// 3. Extract peerId (parts[1]) and port (parts[2])
    /// 4. If peerId equals LocalPeerId, return (don't add ourselves)
    /// 5. Create a Peer object with the extracted info and current timestamp
    /// 6. Try to add to _knownPeers:
    ///    - If new peer, invoke OnPeerDiscovered
    ///    - If existing peer, update LastSeen timestamp
    /// </summary>
    private void ProcessDiscoveryMessage(string message, IPAddress senderAddress)
    {
        throw new NotImplementedException("Implement ProcessDiscoveryMessage() - see TODO in comments above");
    }

    /// <summary>
    /// Periodically check for peers that have timed out (no broadcast in 30 seconds).
    ///
    /// TODO: Implement the following:
    /// 1. Loop while cancellation not requested:
    ///    a. Define timeout as 30 seconds
    ///    b. Get current time
    ///    c. Iterate through _knownPeers
    ///    d. If (now - peer.LastSeen) > timeout:
    ///       - Remove from _knownPeers
    ///       - Invoke OnPeerLost
    ///    e. Delay 5 seconds between checks
    /// </summary>
    private async Task TimeoutCheckLoop()
    {
        throw new NotImplementedException("Implement TimeoutCheckLoop() - see TODO in comments above");
    }

    /// <summary>
    /// Get list of known peers.
    /// </summary>
    public IEnumerable<Peer> GetKnownPeers()
    {
        return _knownPeers.Values.ToList();
    }

    /// <summary>
    /// Stop discovery.
    ///
    /// TODO: Implement the following:
    /// 1. Cancel the cancellation token
    /// 2. Close the UDP client
    /// 3. Wait for threads to finish (with timeout)
    /// </summary>
    public void Stop()
    {
        throw new NotImplementedException("Implement Stop() - see TODO in comments above");
    }
}

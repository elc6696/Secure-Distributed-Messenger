// Ethan Chang
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 3: P2P & Advanced Features
// Due: Week 14 | Work on: Weeks 11-13
//
// NOTE: This file is NOT used in Sprint 1 or Sprint 2!
//
// In Sprint 1-2, users manually connect using /connect and /listen.
// In Sprint 3, this class enables automatic peer discovery:
// - Broadcasts presence on the local network via UDP
// - Discovers other messengers automatically
// - Triggers automatic connection attempts
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
        TcpPort = tcpPort; // storing TCP port

        new CancellationTokenSource().TryAddTo(ref _cancellationTokenSource); // create cancellation token source

        _udpClient = new UdpClient(_broadcastPort); // create UDP client on broadcast port
        _udpClient.EnableBroadcast = true; // enable broadcast

        _listenThread = new Thread(ListenLoop);
        _listenThread.Start(); // start listen thread

        _broadcastThread = new Thread(BroadcastLoop);
        _broadcastThread.Start(); // start broadcast thread

        Task.Run(TimeoutCheckLoop); // start timeout check loop



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

        new IPEndPoint(IPAddress.Broadcast, _broadcastPort); // create broadcast endpoint

        while (!_cancellationTokenSource!.IsCancellationRequested)
        {
            string message = $"PEER:{LocalPeerId}:{TcpPort}"; // create discovery message
            byte[] data = Encoding.UTF8.GetBytes(message); // convert to bytes

            try
            {
                _udpClient!.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, _broadcastPort)); // send via UDP client
            }
            catch (SocketException)
            {
                // ignore broadcast errors
            }

            Thread.Sleep(5000); // sleep for 5 seconds between broadcasts
        }

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

        new IPEndPoint(IPAddress.Any, _broadcastPort); // create receive endpoint

        while (!_cancellationTokenSource!.IsCancellationRequested)
        {
            try
            {
                var result = _udpClient!.Receive(ref new IPEndPoint(IPAddress.Any, _broadcastPort)); // receive data
                string message = Encoding.UTF8.GetString(result); // convert bytes to string

                if (message.StartsWith("PEER:"))
                {
                    ProcessDiscoveryMessage(message, ((IPEndPoint)result).Address); // process discovery message
                }
            }
            catch (SocketException)
            {
                // ignore receive errors
            }
        }

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
        var parts = message.Split(':'); // split message by ':'

        if (parts.Length < 3) 
        {
            return; // validate we have at least 3 parts
        }

        string peerId = parts[1]; // extract peerId

        if (peerId == LocalPeerId) 
        {
            return; // ignore ourselves
        }

        if (!int.TryParse(parts[2], out int port)) 
        {
            return; // extract port
        }

        var peer = new Peer(peerId, senderAddress, port, DateTime.UtcNow); // create Peer object


        if (_knownPeers.TryAdd(peerId, peer)) // try to add to known peers
        {
            OnPeerDiscovered?.Invoke(peer); // invoke OnPeerDiscovered if new peer
        }
        else
        {
            _knownPeers[peerId].LastSeen = DateTime.UtcNow; // update LastSeen timestamp for existing peer
        }


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

        while (!_cancellationTokenSource!.IsCancellationRequested)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(30); // define timeout
            DateTime now = DateTime.UtcNow; // get current time

            foreach (var peer in _knownPeers.Values.ToList()) // iterate through known peers
            {
                if (now - peer.LastSeen > timeout) // check for timeout
                {
                    if (_knownPeers.TryRemove(peer.Id, out _)) // remove from known peers
                    {
                        OnPeerLost?.Invoke(peer); // invoke OnPeerLost
                    }
                }
            }

            await Task.Delay(5000); // delay 5 seconds between checks
        }

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

        _cancellationTokenSource?.Cancel(); // cancel the cancellation token
        _udpClient?.Close(); // close the UDP client

        _listenThread?.Join(1000); // wait for listen thread to finish
        _broadcastThread?.Join(1000); // wait for broadcast thread to finish

    }
}

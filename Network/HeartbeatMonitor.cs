// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 3: P2P & Advanced Features
// Due: Week 14 | Work on: Weeks 11-13
//
// NOTE: This file is NOT used in Sprint 1 or Sprint 2!
//
// In Sprint 1-2, there's no heartbeat monitoring - if a connection drops,
// you only find out when a send/receive fails.
//
// In Sprint 3, this class enables proactive connection health monitoring:
// - Periodic heartbeat messages detect silent connection failures
// - Triggers OnConnectionFailed when a peer stops responding
// - Works with ReconnectionPolicy to attempt automatic reconnection
//

using System.Collections.Concurrent;
using System.Diagnostics;
using SecureMessenger.Core;

namespace SecureMessenger.Network;

/// <summary>
/// Sprint 3: Heartbeat monitoring for connection health.
/// Detects failed connections by tracking when heartbeats were last received.
///
/// Configuration:
/// - Heartbeat interval: 5 seconds (how often to send heartbeats)
/// - Timeout: 15 seconds (how long without heartbeat before considered failed)
/// </summary>
public class HeartbeatMonitor
{
    private readonly ConcurrentDictionary<string, DateTime> _lastHeartbeat = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);

    public event Action<string>? OnConnectionFailed;
    public event Action<string>? OnHeartbeatReceived;

    /// <summary>
    /// The interval at which heartbeats should be sent.
    /// Use this when implementing heartbeat sending in your main program.
    /// </summary>
    public TimeSpan HeartbeatInterval => _heartbeatInterval;

    /// <summary>
    /// Start the heartbeat monitoring loop.
    ///
    /// TODO: Implement the following:
    /// 1. Create a new CancellationTokenSource
    /// 2. Start MonitorLoop as a background task
    /// </summary>
    public void Start()
    {
        throw new NotImplementedException("Implement Start() - see TODO in comments above");
    }

    /// <summary>
    /// Start monitoring a specific peer.
    /// Call this when a peer connects.
    ///
    /// TODO: Implement the following:
    /// 1. Record current time as the peer's last heartbeat
    /// </summary>
    public void StartMonitoring(string peerId)
    {
        throw new NotImplementedException("Implement StartMonitoring() - see TODO in comments above");
    }

    /// <summary>
    /// Record that a heartbeat was received from a peer.
    /// Call this when you receive a heartbeat message.
    ///
    /// TODO: Implement the following:
    /// 1. Update the peer's last heartbeat time to now
    /// 2. Invoke OnHeartbeatReceived event
    /// </summary>
    public void RecordHeartbeat(string peerId)
    {
        throw new NotImplementedException("Implement RecordHeartbeat() - see TODO in comments above");
    }

    /// <summary>
    /// Stop monitoring a peer.
    /// Call this when a peer disconnects normally.
    ///
    /// TODO: Implement the following:
    /// 1. Remove the peer from _lastHeartbeat dictionary
    /// </summary>
    public void StopMonitoring(string peerId)
    {
        throw new NotImplementedException("Implement StopMonitoring() - see TODO in comments above");
    }

    /// <summary>
    /// Main monitoring loop - checks for timed out connections.
    ///
    /// TODO: Implement the following:
    /// 1. Loop while cancellation not requested:
    ///    a. Get current time
    ///    b. Iterate through all entries in _lastHeartbeat
    ///    c. Calculate elapsed time since last heartbeat
    ///    d. If elapsed > _timeout:
    ///       - Log timeout message
    ///       - Invoke OnConnectionFailed event
    ///       - Call StopMonitoring for that peer
    ///    e. Delay 1 second between checks
    /// </summary>
    private async Task MonitorLoop()
    {
        throw new NotImplementedException("Implement MonitorLoop() - see TODO in comments above");
    }

    /// <summary>
    /// Check if a peer is still alive (received heartbeat recently).
    ///
    /// TODO: Implement the following:
    /// 1. Try to get the peer's last heartbeat time
    /// 2. If found, return true if (now - lastSeen) < _timeout
    /// 3. If not found, return false
    /// </summary>
    public bool IsAlive(string peerId)
    {
        throw new NotImplementedException("Implement IsAlive() - see TODO in comments above");
    }

    /// <summary>
    /// Stop monitoring all peers.
    ///
    /// TODO: Implement the following:
    /// 1. Cancel the cancellation token
    /// </summary>
    public void Stop()
    {
        throw new NotImplementedException("Implement Stop() - see TODO in comments above");
    }
}

// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 3: P2P & Advanced Features
// Due: Week 14 | Work on: Weeks 11-13
//

using System.Collections.Concurrent;
using SecureMessenger.Core;

namespace SecureMessenger.Network;

/// <summary>
/// Sprint 3: Automatic reconnection with exponential backoff.
///
/// Exponential Backoff Strategy:
/// - Initial delay: 1 second
/// - Each retry doubles the delay: 1s -> 2s -> 4s -> 8s -> 16s
/// - Maximum delay capped at 30 seconds
/// - Maximum 5 attempts before giving up
///
/// Example retry sequence:
/// Attempt 1: immediate, then wait 1s
/// Attempt 2: retry, then wait 2s
/// Attempt 3: retry, then wait 4s
/// Attempt 4: retry, then wait 8s
/// Attempt 5: retry, then give up
/// </summary>
public class ReconnectionPolicy
{
    private readonly ConcurrentDictionary<string, int> _attemptCount = new();
    private readonly TcpClientHandler _clientHandler;

    private const int MaxAttempts = 5;
    private const int InitialDelayMs = 1000;
    private const int MaxDelayMs = 30000;

    public event Action<string, int>? OnReconnectAttempt;
    public event Action<string>? OnReconnectSuccess;
    public event Action<string>? OnReconnectFailed;

    public ReconnectionPolicy(TcpClientHandler clientHandler)
    {
        _clientHandler = clientHandler;
    }

    /// <summary>
    /// Attempt to reconnect to a peer with exponential backoff.
    ///
    /// TODO: Implement the following:
    /// 1. Get current attempt count for this peer (default 0)
    /// 2. Loop while attempt < MaxAttempts:
    ///    a. Increment attempt count and store in _attemptCount
    ///    b. Log reconnection attempt
    ///    c. Invoke OnReconnectAttempt event
    ///    d. Calculate delay using exponential backoff:
    ///       delay = min(InitialDelayMs * 2^(attempt-1), MaxDelayMs)
    ///    e. Try to connect using _clientHandler.ConnectAsync
    ///    f. If successful:
    ///       - Log success
    ///       - Call ResetAttempts
    ///       - Invoke OnReconnectSuccess
    ///       - Return true
    ///    g. If failed, log error and wait for calculated delay
    /// 3. After max attempts, log failure, invoke OnReconnectFailed, return false
    /// </summary>
    public async Task<bool> TryReconnect(Peer peer)
    {
        throw new NotImplementedException("Implement TryReconnect() - see TODO in comments above");
    }

    /// <summary>
    /// Reset attempt count for a peer.
    /// Call this after a successful connection.
    ///
    /// TODO: Implement the following:
    /// 1. Remove the peer's entry from _attemptCount
    /// </summary>
    public void ResetAttempts(string peerId)
    {
        throw new NotImplementedException("Implement ResetAttempts() - see TODO in comments above");
    }

    /// <summary>
    /// Get current attempt count for a peer.
    ///
    /// TODO: Implement the following:
    /// 1. Try to get value from _attemptCount
    /// 2. Return the count, or 0 if not found
    /// </summary>
    public int GetAttemptCount(string peerId)
    {
        throw new NotImplementedException("Implement GetAttemptCount() - see TODO in comments above");
    }
}

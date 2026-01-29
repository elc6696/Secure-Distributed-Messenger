// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
//
// KEY CONCEPTS USED IN THIS FILE:
//   - BlockingCollection<T>: thread-safe queue that blocks on Take() (see HINTS.md)
//   - Producer/Consumer pattern: network threads add, processing threads take
//   - CancellationToken: allows Take() to be interrupted during shutdown
//
// NOTE: This class is OPTIONAL for Sprint 1!
//
// The simplest approach (used in the reference solution) is to handle messages
// directly in event handlers:
//   - server.OnMessageReceived += message => ui.DisplayMessage(message);
//
// MessageQueue is useful if you want a more sophisticated architecture:
//   - Separate network I/O from message processing
//   - Buffer messages during high load
//   - Process messages on a dedicated thread
//
// If you're just getting started, skip MessageQueue and use direct events.
// You can always refactor to use MessageQueue later if needed.
//

using System.Collections.Concurrent;

namespace SecureMessenger.Core;

/// <summary>
/// Thread-safe message queue for incoming/outgoing messages.
///
/// This class implements the Producer/Consumer pattern:
/// - Producers add messages to the queue (network threads, UI thread)
/// - Consumers take messages from the queue (processing threads, send threads)
///
/// Thread Safety Options:
/// 1. BlockingCollection&lt;T&gt; - recommended, handles blocking and thread safety
/// 2. ConcurrentQueue&lt;T&gt; with manual synchronization
/// 3. Queue&lt;T&gt; with explicit locking
///
/// The blocking behavior is important:
/// - Take() should block when the queue is empty
/// - This allows consumer threads to wait efficiently without busy-waiting
/// </summary>
public class MessageQueue
{
    // TODO: Choose your thread-safe collection(s)
    // Option 1: BlockingCollection<Message> (recommended)
    // Option 2: ConcurrentQueue<Message>
    // Option 3: Queue<Message> with lock

    /// <summary>
    /// Enqueue an incoming message (received from network).
    ///
    /// TODO: Implement the following:
    /// 1. Add the message to your incoming queue
    /// 2. Ensure thread safety (depends on your collection choice)
    /// </summary>
    public void EnqueueIncoming(Message message)
    {
        throw new NotImplementedException("Implement EnqueueIncoming() - see TODO in comments above");
    }

    /// <summary>
    /// Dequeue an incoming message for processing.
    /// This method BLOCKS if the queue is empty - hence "Blocking" in the name.
    ///
    /// TODO: Implement the following:
    /// 1. Take a message from the incoming queue
    /// 2. Block if the queue is empty (don't busy-wait)
    /// 3. Support cancellation via the CancellationToken
    ///
    /// Hint: BlockingCollection.Take(cancellationToken) does this automatically
    /// </summary>
    public Message DequeueIncomingBlocking(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implement DequeueIncomingBlocking() - see TODO in comments above");
    }

    /// <summary>
    /// Try to dequeue an incoming message without blocking.
    ///
    /// TODO: Implement the following:
    /// 1. Attempt to take a message from the incoming queue
    /// 2. Return true if successful, false if queue is empty
    /// 3. Set 'message' to the dequeued message or null
    ///
    /// Hint: BlockingCollection.TryTake() does this
    /// </summary>
    public bool TryDequeueIncoming(out Message? message)
    {
        throw new NotImplementedException("Implement TryDequeueIncoming() - see TODO in comments above");
    }

    /// <summary>
    /// Enqueue an outgoing message (to be sent to network).
    ///
    /// TODO: Implement the following:
    /// 1. Add the message to your outgoing queue
    /// 2. Ensure thread safety
    /// </summary>
    public void EnqueueOutgoing(Message message)
    {
        throw new NotImplementedException("Implement EnqueueOutgoing() - see TODO in comments above");
    }

    /// <summary>
    /// Dequeue an outgoing message for sending.
    /// This method BLOCKS if the queue is empty - hence "Blocking" in the name.
    ///
    /// TODO: Implement the following:
    /// 1. Take a message from the outgoing queue
    /// 2. Block if the queue is empty
    /// 3. Support cancellation via the CancellationToken
    /// </summary>
    public Message DequeueOutgoingBlocking(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implement DequeueOutgoingBlocking() - see TODO in comments above");
    }

    /// <summary>
    /// Try to dequeue an outgoing message without blocking.
    ///
    /// TODO: Implement the following:
    /// 1. Attempt to take a message from the outgoing queue
    /// 2. Return true if successful, false if queue is empty
    /// 3. Set 'message' to the dequeued message or null
    /// </summary>
    public bool TryDequeueOutgoing(out Message? message)
    {
        throw new NotImplementedException("Implement TryDequeueOutgoing() - see TODO in comments above");
    }

    /// <summary>
    /// Get the count of incoming messages waiting to be processed.
    ///
    /// TODO: Return the count of your incoming queue
    /// </summary>
    public int IncomingCount => throw new NotImplementedException("Implement IncomingCount property");

    /// <summary>
    /// Get the count of outgoing messages waiting to be sent.
    ///
    /// TODO: Return the count of your outgoing queue
    /// </summary>
    public int OutgoingCount => throw new NotImplementedException("Implement OutgoingCount property");

    /// <summary>
    /// Signal that no more messages will be added.
    /// Call this during shutdown to unblock waiting consumers.
    ///
    /// TODO: Implement the following:
    /// 1. Mark both queues as complete (no more additions)
    ///
    /// Hint: BlockingCollection.CompleteAdding() does this
    /// </summary>
    public void CompleteAdding()
    {
        throw new NotImplementedException("Implement CompleteAdding() - see TODO in comments above");
    }
}

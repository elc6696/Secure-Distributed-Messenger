// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 3: P2P & Advanced Features
// Due: Week 14 | Work on: Weeks 11-13
//

using System.Text.Json;
using SecureMessenger.Core;

namespace SecureMessenger.UI;

/// <summary>
/// Sprint 3: Message history storage and retrieval.
/// Persists messages to a JSON file for retrieval across sessions.
///
/// Features:
/// - Thread-safe message storage
/// - JSON serialization/deserialization
/// - Automatic loading on startup
/// - Configurable history display limit
///
/// File Format: JSON array of Message objects
/// Default file: "message_history.json"
/// </summary>
public class MessageHistory
{
    private readonly string _historyFile;
    private readonly List<Message> _messages = new();
    private readonly object _lock = new();

    /// <summary>
    /// Create a MessageHistory with optional custom file path.
    /// Automatically loads existing history from file.
    ///
    /// TODO: Implement the following:
    /// 1. Store the history file path
    /// 2. Call Load() to load existing history
    /// </summary>
    public MessageHistory(string historyFile = "message_history.json")
    {
        throw new NotImplementedException("Implement constructor - see TODO in comments above");
    }

    /// <summary>
    /// Save a message to history and persist to file.
    ///
    /// TODO: Implement the following:
    /// 1. Lock on _lock for thread safety
    /// 2. Add the message to _messages list
    /// 3. Call PersistToFile() to save to disk
    /// </summary>
    public void SaveMessage(Message message)
    {
        throw new NotImplementedException("Implement SaveMessage() - see TODO in comments above");
    }

    /// <summary>
    /// Load history from file on startup.
    ///
    /// TODO: Implement the following:
    /// 1. Check if the history file exists
    /// 2. If it exists:
    ///    a. Read the file contents as a string
    ///    b. Deserialize from JSON to List<Message>
    ///    c. Lock on _lock and replace _messages with loaded data
    /// 3. Handle exceptions (file errors, JSON errors):
    ///    a. Print error message but don't crash
    ///    b. Start with empty history if load fails
    ///
    /// Hint: Use JsonSerializer.Deserialize<List<Message>>()
    /// </summary>
    public void Load()
    {
        throw new NotImplementedException("Implement Load() - see TODO in comments above");
    }

    /// <summary>
    /// Write the current messages to the history file.
    ///
    /// TODO: Implement the following:
    /// 1. Serialize _messages to JSON
    ///    - Use JsonSerializerOptions with WriteIndented = true for readability
    /// 2. Write the JSON string to the history file
    /// 3. Handle exceptions:
    ///    a. Print error message but don't crash
    ///
    /// Note: This is called while holding _lock, so don't lock again
    /// </summary>
    private void PersistToFile()
    {
        throw new NotImplementedException("Implement PersistToFile() - see TODO in comments above");
    }

    /// <summary>
    /// Get messages from history.
    ///
    /// TODO: Implement the following:
    /// 1. Lock on _lock for thread safety
    /// 2. Order messages by Timestamp descending (newest first)
    /// 3. If limit is specified, take only that many messages
    /// 4. Return as a new List (don't return the internal list)
    ///
    /// Hint: Use LINQ OrderByDescending, Take, and ToList
    /// </summary>
    public IEnumerable<Message> GetHistory(int? limit = null)
    {
        throw new NotImplementedException("Implement GetHistory() - see TODO in comments above");
    }

    /// <summary>
    /// Display history to console.
    ///
    /// TODO: Implement the following:
    /// 1. Print a header: "--- Message History (last N messages) ---"
    /// 2. Get history with the specified limit
    /// 3. Reverse the order (so oldest is first, newest is last)
    /// 4. Print each message using its ToString()
    /// 5. Print a footer: "--- End of History ---"
    /// </summary>
    public void ShowHistory(int limit = 50)
    {
        throw new NotImplementedException("Implement ShowHistory() - see TODO in comments above");
    }

    /// <summary>
    /// Clear all history from memory and disk.
    ///
    /// TODO: Implement the following:
    /// 1. Lock on _lock for thread safety
    /// 2. Clear the _messages list
    /// 3. Delete the history file if it exists
    /// </summary>
    public void Clear()
    {
        throw new NotImplementedException("Implement Clear() - see TODO in comments above");
    }
}

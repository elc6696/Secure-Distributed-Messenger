// Donald Tsang
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
    private readonly Lock _lock = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

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
        _historyFile = historyFile;
        Load();
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
        lock (_lock)
        {
            _messages.Add(message);
            PersistToFile();
        }
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
        try
        {
            if (!File.Exists(_historyFile)) return;

            string json = File.ReadAllText(_historyFile);
            var loaded = JsonSerializer.Deserialize<List<Message>>(json);
            if (loaded != null)
            {
                lock (_lock)
                {
                    _messages.Clear();
                    _messages.AddRange(loaded);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[System] Failed to load message history: {ex.Message}");
        }
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
        try
        {
            string json = JsonSerializer.Serialize(_messages, _jsonOptions);
            File.WriteAllText(_historyFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[System] Failed to save message history: {ex.Message}");
        }
    }

    /// <summary>
    /// Get messages from history, newest first, with optional count limit.
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
        lock (_lock)
        {
            var ordered = _messages.OrderByDescending(m => m.Timestamp);
            return (limit.HasValue ? ordered.Take(limit.Value) : ordered).ToList();
        }
    }

    /// <summary>
    /// Display history to console, oldest first.
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
        Console.WriteLine($"--- Message History (last {limit} messages) ---");
        var messages = GetHistory(limit).Reverse();
        foreach (var message in messages)
            Console.WriteLine(message.ToString());
        Console.WriteLine("--- End of History ---");
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
        lock (_lock)
        {
            _messages.Clear();
            if (File.Exists(_historyFile))
                File.Delete(_historyFile);
        }
    }
}

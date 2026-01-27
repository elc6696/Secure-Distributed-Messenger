// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 1: Threading & Basic Networking
// Due: Week 5 | Work on: Weeks 3-4
//

using SecureMessenger.Core;

namespace SecureMessenger.UI;

/// <summary>
/// Console-based user interface.
/// Handles user input parsing and message display.
///
/// Supported Commands:
/// - /connect <ip> <port>  - Connect to a peer
/// - /listen <port>        - Start listening for connections
/// - /peers                - List known peers
/// - /history              - View message history
/// - /quit or /exit        - Exit the application
/// - Any other text        - Send as a message
/// </summary>
public class ConsoleUI
{
    private readonly MessageQueue _messageQueue;

    public ConsoleUI(MessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
    }

    /// <summary>
    /// Display a received message to the console.
    ///
    /// TODO: Implement the following:
    /// 1. Format the timestamp as "HH:mm:ss"
    /// 2. Print in format: "[timestamp] sender: content"
    /// </summary>
    public void DisplayMessage(Message message)
    {
        throw new NotImplementedException("Implement DisplayMessage() - see TODO in comments above");
    }

    /// <summary>
    /// Display a system message to the console.
    ///
    /// TODO: Implement the following:
    /// 1. Print in format: "[System] message"
    /// </summary>
    public void DisplaySystem(string message)
    {
        throw new NotImplementedException("Implement DisplaySystem() - see TODO in comments above");
    }

    /// <summary>
    /// Show available commands to the user.
    ///
    /// TODO: Implement the following:
    /// 1. Print a formatted help message showing all available commands
    /// 2. Include: /connect, /listen, /peers, /history, /quit
    /// </summary>
    public void ShowHelp()
    {
        throw new NotImplementedException("Implement ShowHelp() - see TODO in comments above");
    }

    /// <summary>
    /// Parse user input and return a CommandResult.
    ///
    /// TODO: Implement the following:
    /// 1. Check if input starts with "/" - if not, it's a regular message:
    ///    - Return CommandResult with IsCommand = false, Message = input
    ///
    /// 2. If it's a command, split by spaces and parse:
    ///    - "/connect <ip> <port>" -> CommandType.Connect with Args = [ip, port]
    ///    - "/listen <port>" -> CommandType.Listen with Args = [port]
    ///    - "/peers" -> CommandType.ListPeers
    ///    - "/history" -> CommandType.History
    ///    - "/quit" or "/exit" -> CommandType.Quit
    ///    - Unknown command -> CommandType.Unknown with error message
    ///
    /// Hint: Use string.Split(' ', StringSplitOptions.RemoveEmptyEntries)
    /// Hint: Use a switch expression for clean command matching
    /// </summary>
    public CommandResult ParseCommand(string input)
    {
        throw new NotImplementedException("Implement ParseCommand() - see TODO in comments above");
    }
}

/// <summary>
/// Types of commands the user can enter
/// </summary>
public enum CommandType
{
    Unknown,
    Connect,
    Listen,
    ListPeers,
    History,
    Quit
}

/// <summary>
/// Result of parsing a user input line
/// </summary>
public class CommandResult
{
    /// <summary>True if the input was a command (started with /)</summary>
    public bool IsCommand { get; set; }

    /// <summary>The type of command parsed</summary>
    public CommandType CommandType { get; set; }

    /// <summary>Arguments for the command (e.g., IP and port for /connect)</summary>
    public string[]? Args { get; set; }

    /// <summary>The message content (for non-commands or error messages)</summary>
    public string? Message { get; set; }
}

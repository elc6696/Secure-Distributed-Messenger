
// Cooper Miles
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
/// - /connect host port  - Connect to another messenger
/// - /listen port        - Start listening for connections
/// - /peers              - Show connection status
/// - /history            - View message history (Sprint 3)
/// - /quit or /exit      - Exit the application
/// - Any other text      - Send as a message
/// </summary>
public class ConsoleUI
{
    /// <summary>
    /// Display a received message to the console.
    ///
    /// DONE:
    /// TODO: Implement the following:
    /// 1. Format the message nicely, e.g.: "[14:30:25] Alice: Hello!"
    /// 2. Use message.Timestamp.ToString("HH:mm:ss") for time format
    /// 3. Print to console
    /// </summary>
    public async Task DisplayMessage(Message message)
    {
        // string time = DateTime.Now.ToString("HH:mm:ss");
        // string response = "[" + time + "] " + message.Content.ToUpper();
        string response = $"[{message.Timestamp:HH:mm:ss}] {message.Sender}: {message.Content}";
        Console.WriteLine(response);
    }

    /// <summary>
    /// Display a system message to the console.
    ///
    /// DONE:
    /// TODO: Implement the following:
    /// 1. Print in a distinct format, e.g.: "[System] Server started on port 5000"
    /// </summary>
    public void DisplaySystem(string message)
    {
        string response = "[System] " + message;
        Console.WriteLine(response);
    }

    /// <summary>
    /// Show available commands to the user.
    ///
    /// DONE:
    /// TODO: Implement the following:
    /// 1. Print a formatted help message showing all available commands
    /// 2. Include: /connect, /listen, /peers, /history, /quit
    /// </summary>
    public void ShowHelp()
    {
        Console.WriteLine("\nAvailable Commands:");
        Console.WriteLine("  /connect <ip> <port>  - Connect to another messenger");
        Console.WriteLine("  /listen <port>        - Start listening for connections");
        Console.WriteLine("  /peers                - Show connection status");
        Console.WriteLine("  /history              - View message history (Sprint 3)");
        Console.WriteLine("  /quit                 - Exit the application");
        Console.WriteLine();
        Console.WriteLine("Sprint Progression:");
        Console.WriteLine("  Sprint 1: Basic /connect and /listen with message sending");
        Console.WriteLine("  Sprint 2: Messages are encrypted end-to-end");
        Console.WriteLine("  Sprint 3: Automatic peer discovery and reconnection");
        Console.WriteLine();
    }

    /// <summary>
    /// Parse user input and return a CommandResult.
    ///
    /// DONE:
    /// TODO: Implement the following:
    /// 1. Check if input starts with "/" - if not, it's a regular message:
    ///    - Return CommandResult with IsCommand = false, Message = input
    ///
    /// 2. If it's a command, split by spaces and parse:
    ///    - "/connect host port" -> CommandType.Connect with Args = [host, port]
    ///    - "/listen port" -> CommandType.Listen with Args = [port]
    ///    - "/peers" -> CommandType.Peers
    ///    - "/history" -> CommandType.History
    ///    - "/quit" or "/exit" -> CommandType.Quit
    ///    - "/help" -> CommandType.Help
    ///    - Unknown command -> CommandType.Unknown with error message
    ///
    /// 3. Validate arguments:
    ///    - /connect requires 2 args (host and port)
    ///    - /listen requires 1 arg (port)
    ///
    /// Hint: Use input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
    /// Hint: Use a switch expression for clean command matching
    /// </summary>
    public CommandResult ParseCommand(string input)
    {
        CommandResult commandResult = new(); 
        
        if (input.StartsWith("/"))
        {
            string[] inputsplit = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (inputsplit[0].ToLower())
            {
                case "/connect":
                    if (inputsplit.Length != 3)
                    {
                        commandResult.CommandType = CommandType.Unknown;
                        return commandResult;
                    } 
                    else
                    {
                        commandResult.CommandType = CommandType.Connect;
                        commandResult.Args = [inputsplit[1], inputsplit[2]];
                        return commandResult;
                    }
                case "/listen":
                    if (inputsplit.Length != 2)
                    {
                        commandResult.CommandType = CommandType.Unknown;
                        return commandResult;
                    } 
                    else
                    {
                        commandResult.CommandType = CommandType.Listen;
                        commandResult.Args = [inputsplit[1]];
                        return commandResult;
                    }
                case "/peers":
                    commandResult.CommandType = CommandType.Peers;
                    return commandResult;
                case "/history":
                    commandResult.CommandType = CommandType.History;
                    return commandResult;
                case "/quit":
                case "/exit":
                    commandResult.CommandType = CommandType.Quit;
                    return commandResult;
                case "/help":
                    commandResult.CommandType = CommandType.Help;
                    return commandResult;
                default:
                    commandResult.CommandType = CommandType.Unknown;
                    return commandResult;
            }
        }
        else
        {
            commandResult.IsCommand = false;
            commandResult.Message = input;
            return commandResult;
        }
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
    Peers,
    History,
    Help,
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

    /// <summary>Arguments for the command (e.g., host and port for /connect)</summary>
    public string[]? Args { get; set; }

    /// <summary>The message content (for non-commands or error messages)</summary>
    public string? Message { get; set; }
}

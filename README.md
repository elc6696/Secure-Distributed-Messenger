# Secure Distributed Messenger

CSCI 251: Concepts of Parallel and Distributed Systems

> **Note:** For sprint submissions, use the `sprint-X-documentation.md` templates provided in the templates folder. This README is for your GitHub repository reference only.

## Build Instructions

### Prerequisites
- .NET 9.0 SDK or later

### Building the Project
```bash
dotnet build
```

Or for a release build:
```bash
dotnet build -c Release
```

## Run Instructions

### Starting the Application
```bash
dotnet run
```

Or run the compiled executable:
```bash
dotnet run --project SecureMessenger.csproj
```

## Usage

### Available Commands
- `/connect <ip> <port>` - Connect to a peer at the specified address
- `/listen <port>` - Start listening for incoming connections
- `/peers` - List all known peers
- `/history` - View message history
- `/quit` - Exit the application

### Example Session
```
Secure Distributed Messenger
============================
Type /help for available commands

/listen 5000
Listening on port 5000...

/connect 192.168.1.100 5000
Connected to 192.168.1.100:5000

Hello, world!
[10:30:45] You: Hello, world!
[10:30:47] Peer1: Hi there!

/quit
Goodbye!
```

## Project Structure

```
SecureMessenger/
├── Program.cs                 # Entry point - implement main loop and threading
├── Core/
│   ├── Message.cs             # Message model (provided)
│   ├── MessageQueue.cs        # Thread-safe queue (implement)
│   └── Peer.cs                # Peer information (provided)
├── Network/
│   ├── TcpServer.cs           # Listens for connections (implement)
│   ├── TcpClientHandler.cs    # Handles outgoing connections (implement)
│   ├── PeerDiscovery.cs       # UDP broadcast discovery (implement)
│   ├── HeartbeatMonitor.cs    # Connection health monitoring (implement)
│   └── ReconnectionPolicy.cs  # Automatic reconnection (implement)
├── Security/
│   ├── AesEncryption.cs       # AES encrypt/decrypt (implement)
│   ├── RsaEncryption.cs       # RSA key management (implement)
│   ├── MessageSigner.cs       # Digital signatures (implement)
│   └── KeyExchange.cs         # Key exchange protocol (implement)
└── UI/
    ├── ConsoleUI.cs           # User interface (implement)
    └── MessageHistory.cs      # Message persistence (implement)
```

## What's Provided vs. What You Implement

### Provided (Do Not Modify)
- **Class structures**: All classes, fields, properties, and method signatures
- **Data models**: `Message.cs` and `Peer.cs` are complete
- **Events**: All event declarations for thread communication
- **Constants**: Configuration values (timeouts, intervals, key sizes)
- **Enums**: `CommandType`, `ConnectionState`, etc.

### You Must Implement
All methods marked with `throw new NotImplementedException()` - look for the detailed TODO comments in each method that explain exactly what to implement.

## Getting Started

**New to C# threading, events, or networking?** Start with `HINTS.md` - it contains:
- How to use `Action` and events (the notification pattern used throughout)
- How to use `BlockingCollection` for thread-safe queues
- How threads and cancellation tokens work
- TCP networking basics
- **Sprint progression guide** - how to approach each sprint

## Sprint Implementation Guide

### Sprint 1: Threading & Basic Networking (Week 5)

> **Tip:** Start by thinking of your app as having two separate roles: a **Server** (listens and accepts connections) and a **Client** (connects to servers). See HINTS.md for diagrams.

**Files to complete:**
- `Program.cs` - Main loop, thread creation, event handling
- `Core/MessageQueue.cs` - Thread-safe producer/consumer queue
- `Network/TcpServer.cs` - TCP listener, accept loop, receive threads
- `Network/TcpClientHandler.cs` - TCP client, connect, send/receive
- `UI/ConsoleUI.cs` - Command parsing and message display

**Key concepts:**
- Multi-threading with `Thread` and `Task`
- Thread synchronization with `lock` and `BlockingCollection`
- TCP sockets with `TcpListener` and `TcpClient`
- Event-driven programming with C# events

### Sprint 2: Security & Encryption (Week 10)

**Files to complete:**
- `Security/AesEncryption.cs` - AES-256-CBC encryption/decryption
- `Security/RsaEncryption.cs` - RSA-2048 key pair management
- `Security/MessageSigner.cs` - RSA-SHA256 digital signatures
- `Security/KeyExchange.cs` - Key exchange state machine

**Key concepts:**
- Symmetric encryption (AES)
- Asymmetric encryption (RSA)
- Digital signatures
- Key exchange protocols

### Sprint 3: P2P & Advanced Features (Week 14)

**Files to complete:**
- `Network/PeerDiscovery.cs` - UDP broadcast for peer discovery
- `Network/HeartbeatMonitor.cs` - Connection health monitoring
- `Network/ReconnectionPolicy.cs` - Exponential backoff reconnection
- `UI/MessageHistory.cs` - JSON-based message persistence

**Key concepts:**
- UDP broadcast
- Heartbeat/keepalive patterns
- Exponential backoff retry logic
- File I/O with JSON serialization

## Technical Specifications

### Wire Protocol
- Messages sent as newline-terminated strings
- Sprint 2+: JSON-serialized Message objects with encrypted content

### Encryption (Sprint 2)
- **AES-256-CBC**: 32-byte key, 16-byte IV prepended to ciphertext
- **RSA-2048**: OAEP-SHA256 padding for key exchange
- **Signatures**: RSA-SHA256 with PKCS#1 v1.5 padding

### Discovery Protocol (Sprint 3)
- UDP broadcast on port 5001
- Message format: `PEER:<peerId>:<tcpPort>`
- Broadcast interval: 5 seconds
- Peer timeout: 30 seconds

### Heartbeat (Sprint 3)
- Interval: 5 seconds
- Timeout: 15 seconds

### Reconnection (Sprint 3)
- Max attempts: 5
- Backoff: 1s → 2s → 4s → 8s → 16s (capped at 30s)

## Known Issues

[Document any known issues here]

## Testing

[Document testing procedures here]

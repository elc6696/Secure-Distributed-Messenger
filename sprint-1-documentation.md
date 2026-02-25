# Sprint 1 Documentation
## Secure Distributed Messenger

**Team Name:** Group 25

**Team Members:**
- Donald Tsang - Multi-Threaded Architecture
- Ethan Chang - Basic TCP Communication
- Cooper Miles - Console UI
- Teju - Management, documentation, debugging (helping all of us)

**Date:** 02/27/2026

---

## Build Instructions

### Prerequisites
- [List required software, e.g., .NET SDK version]
- [Any other dependencies]

### Building the Project
```
dotnet build
```

---

## Run Instructions

### Starting the Application
```
dotnet run
```

### Command Line Arguments (if any)
| Argument | Description | Example |
|----------|-------------|---------|
| | | |

---

## Application Commands

| Command | Description | Example |
|---------|-------------|---------|
| `/connect <ip> <port>` | Connect to a peer | `/connect 192.168.1.100 5000` |
| `/listen <port>` | Start listening for connections | `/listen 5000` |
| `/quit` | Exit the application | `/quit` |
| `/exit` | Exit the application | `/exit` |
| `/help` | Display the commands | `/help` |
| | | |

---

## Architecture Overview

### Threading Model
[Describe your threading approach - which threads exist and what each does]

- **Main Thread:** Runs the console input loop (`Console.ReadLine()`) and parses commands (`ConsoleUI.ParseCommand()`) and dispatches all command actions. Also manages startup and graceful shutdown.
- **Receive Thread:** A backround thread (`ReceiveThread`) that blocks on `_queue.DequeueIncoming(cancellationToken))`. For incoming messages, we wake it up via `_ui.DisplayMessage()`to print it. We exit cleanly when the cancellation token is triggered on shutdown. 
- **Send Thread:** A backround thread (`SendThread`) that blocks on `_queue.DequeueOutgoing(cancellationToken)`. When outgoing messages that are in queue, it wakes up and calls `_server?.BroadcastMessage(message)` if listening or `_client?.Send(message)` if connected. We exit cleanly on cancellation.

### Thread-Safe Message Queue
The `MessageQueue` class inside `Core/MessageQueue.cs`. implements a producer/consumer pattern using two separate `BlockingCollection<Message>` instances. `_incoming` for messages received from the network and `_outgoing` for messages to be sent. `BlockingCollection<Message>` is thread-safe, so we do not use manual locking required.

* `EnqueueIncoming()` / `EnqueueOutgoing()` 
    * Thread-safe adds to queue by network event handlers
* `DequeueIncoming(token)` / `DequeueOutgoing(token)` 
    * Block `Take()` calls that sleep the consumer thread until message is available
* `CancellationTokenSource.Cancel()`
    * Unblcok `Take()` by passing the cancellation token, and `CompleteAdding()` finalizes adding items. 

---

## Features Implemented

- [x] Multi-threaded architecture
- [x] Thread-safe message queue
- [x] TCP server (listen for connections)
- [x] TCP client (connect to peers)
- [x] Send/receive text messages
- [x] Graceful disconnection handling
- [x] Console UI with commands

---

## Testing Performed

### Test Cases
| Test | Expected Result | Actual Result | Pass/Fail |
|------|-----------------|---------------|-----------|
| Two instances can connect | Connection established | Connections are established | Pass |
| Messages sent and received | Message appears on other instance | Message does appear on other instance | Pass |
| Disconnection handled | No crash, appropriate message | No crash, says Goodbye | Pass |
| Thread safety under load | No race conditions | No race conditions | Pass |

---

## Known Issues

| Issue | Description | Workaround |

---

## Video Demo Checklist

Your demo video (3-5 minutes) should show:
- [x] Starting two instances of the application
- [x] Connecting the instances
- [x] Sending messages in both directions
- [x] Disconnecting gracefully
- [ ] (Optional) Showing thread-safe behavior under load

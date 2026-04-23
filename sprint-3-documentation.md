# Sprint 3 Documentation (Final)
## Secure Distributed Messenger

**Team Name:** [Your Team Name]

**Team Members:**
- Donald Tsang - Peer-To-Peer Architecture, Message History (File-Based)
- [Name 2] - [Role/Responsibilities]
- [Name 3] - [Role/Responsibilities]
- [Name 4] - [Role/Responsibilities]
- [Name 5] - [Role/Responsibilities]

**Date:** [Submission Date]

---

## Build & Run Instructions

### Prerequisites
- [List all required software]

### Building
```
[Commands to build]
```

### Running
```
[Commands to run]
```

### Command Line Arguments
| Argument | Description | Default |
|----------|-------------|---------|
| | | |

---

## Application Commands

| Command | Description | Example |
|---------|-------------|---------|
| `/connect <ip> <port>` | Connect to a peer | `/connect 192.168.1.100 5000` |
| `/listen <port>` | Start listening | `/listen 5000` |
| `/peers` | List known peers | `/peers` |
| `/history` | View message history | `/history` |
| `/quit` | Exit application | `/quit` |
| | | |

---

## Architecture Diagram

```
[Insert ASCII diagram of your system architecture]
[Show major components and how they interact]

+------------------+     +------------------+
|                  |     |                  |
|                  |<--->|                  |
|                  |     |                  |
+------------------+     +------------------+
```

### Component Descriptions

| Component | Responsibility |
|-----------|----------------|
| | |
| | |
| | |

---

## Protocol Specification

### Connection Establishment
[Describe the full connection handshake]

```
Peer A                          Peer B
  |                                |
  |-------- [Step 1] ------------->|
  |<------- [Step 2] --------------|
  |-------- [Step 3] ------------->|
  |                                |
```

### Message Flow
[Describe how messages flow through the system]

### Peer Discovery Protocol
[Describe UDP broadcast format and discovery process]

#### Broadcast Message Format
```
[Format of discovery broadcast]
```

#### Discovery Process
1. [Step 1]
2. [Step 2]
3. ...

### Heartbeat Protocol
[Describe heartbeat mechanism]

- **Interval:** [e.g., 5 seconds]
- **Timeout:** [e.g., 15 seconds]
- **Action on timeout:** [e.g., mark as disconnected, attempt reconnect]

---

## P2P Architecture

### Peer Management
Each peer is represented by a `Peer` object (Core/Peer.cs) containing its ID, IP address, TCP port, connection state, and AES session key. `Program.cs` maintains two concurrent dictionaries:
- `_peerClients` — one outbound `Client` instance per remote peer we connected to
- `_activePeers` — the corresponding `Peer` metadata for each active connection

Peers are identified by an 8-character GUID (`LocalPeerId`) generated at startup by `PeerDiscovery`. A separate `_peerKeyExchanges` dictionary tracks RSA-AES handshake for each peer, and `_reconnectPolicies` tracks the `ReconnectionPolicy` instance for each connection.

### Connection Strategy
Every instance runs both a `Server` (accepting inbound TCP connections from any peer) and zero or more outbound `Client` connections (one per discovered peer). When `PeerDiscovery.OnPeerDiscovered` fires:

1. To prevent both sides connecting to each other simultaneously, only the peer whose `LocalPeerId` is lexicographically smaller calls `ConnectToPeer()`. The other side accepts the inbound connection via its Server.
2. `ConnectToPeer()` creates a new `Client`, wires its message/disconnect events, calls `ConnectAsync`, then immediately initiates an RSA-AES key exchange by sending a `MessageType.KeyExchange` message.
3. On disconnect (TCP failure or heartbeat timeout), `HandlePeerConnectionFailed()` is called, which creates a fresh `Client` and spawns `ReconnectionPolicy.TryReconnect()` on a background task.

### Message Routing
- **Incoming:** Messages arrive either via the `Server` (from peers who connected to us) or via a per-peer `Client` (for connections we initiated). Both paths enqueue `MessageType.Text` messages to the shared `MessageQueue` for display and history.
- **Outgoing (broadcast):** Plain text messages are sent to all discovered peers in parallel via `BroadcastToAllPeersParallel()`, which uses `Task.WhenAll` + `Task.Run` to encrypt and send concurrently (TPL).
- **Outgoing (direct):** `/msg @{peerId} message` looks up the target peer's `Client` in `_peerClients` and sends directly to that peer only.

---

## Resilience Features

### Failure Detection
[Describe how connection failures are detected]

### Automatic Reconnection
[Describe your reconnection strategy]

- **Initial delay:** [e.g., 1 second]
- **Backoff strategy:** [e.g., exponential, max 30 seconds]
- **Max attempts:** [e.g., 5]

### Graceful Degradation
[Describe how the system behaves when peers are unavailable]

---

## Message History

### Storage Format
Messages are serialized as JSON array and written to disk after every received message. Each entry preserves the full `Message` object structure including sender, content, timestamp, and message type.

```json
[
  {
    "Id": "...",
    "Sender": "Donald",
    "Content": "Hello!",
    "Timestamp": "2026-04-21T14:32:05",
    "Type": 0
  },
  ...
]
```

### File Location
`message_history.json` in the application's working directory (alongside the executable). The file is created on first message and loaded on startup. If the file is missing or corrupted, the application initiates an empty history and logs a warning.

### History Commands
- `/history` — displays the last 50 messages oldest-to-newest in the console
- History is saved automatically on every received message; no manual save command needed
- Thread safety is ensured with a `Lock` object so concurrent messages never corrupts the file

---

## User Guide

### Getting Started
1. [Step 1: Start the application]
2. [Step 2: ...]
3. ...

### Connecting to Peers
[Instructions for connecting]

### Sending Messages
[Instructions for messaging]

### Viewing Peer Status
[Instructions for checking peer status]

### Troubleshooting
| Problem | Solution |
|---------|----------|
| Cannot connect to peer | [Check firewall, verify IP/port] |
| Messages not sending | [Check connection status] |
| | |

---

## Features Implemented

### Core Features
- [ ] P2P architecture (no central server)
- [ ] Peer discovery (UDP broadcast)
- [ ] Automatic peer connection
- [ ] Heartbeat monitoring
- [ ] Failure detection
- [ ] Automatic reconnection
- [ ] Message history (file-based)
- [ ] Parallel message processing

### Security Features (from Sprint 2)
- [ ] AES encryption
- [ ] RSA key exchange
- [ ] Message signing

### Bonus Features (if implemented)
- [ ] Message relay through intermediate peers
- [ ] Encrypted history storage
- [ ] Peer persistence (save/load known peers)

---

## Testing Performed

### P2P Tests
| Test | Expected Result | Actual Result | Pass/Fail |
|------|-----------------|---------------|-----------|
| 3+ peers can form mesh | All peers connected | | |
| Peer discovery works | New peer found automatically | | |
| Peer leaving detected | Removed from peer list | | |
| Reconnection after failure | Connection restored | | |

### Resilience Tests
| Test | Expected Result | Actual Result | Pass/Fail |
|------|-----------------|---------------|-----------|
| Kill peer process | Detected as failed | | |
| Network interruption | Reconnection attempted | | |
| Peer rejoins | Connection restored | | |

---

## Known Issues

| Issue | Description | Severity | Workaround |
|-------|-------------|----------|------------|
| | | | |

---

## Future Improvements

[What would you improve with more time?]

---

## Video Demo Checklist

Your demo video (8-10 minutes) should show:
- [ ] Starting 3+ peer instances
- [ ] Peer discovery in action
- [ ] Messages between multiple peers
- [ ] Killing a peer and showing failure detection
- [ ] Automatic reconnection when peer returns
- [ ] Message history feature
- [ ] `/peers` command showing connected peers

# Sprint 3 Documentation (Final)
## Secure Distributed Messenger

**Team Name:** Group 25

**Team Members:**
- Donald Tsang — Peer-To-Peer Architecture, Message History (File-Based)
- Cooper Miles — Decentralized Chat Rooms
- Ethan Chang — Peer Discovery, Resilient Connections
- Teju — Parallel Message Processing

**Date:** 4/24/2026

---

## Build & Run Instructions

See [Sprint 1 Documentation - Build Instructions](sprint-1-documentation.md#build-instructions).

Sprint 3 adds one runtime requirement: **UDP port 5001 must be reachable on the local subnet** for automatic peer discovery (firewall must allow UDP broadcast). If discovery is blocked, peers can still be reached via `/connect <ip> <port>`.

---

## Application Commands

| Command | Description | Example |
|---------|-------------|---------|
| `/listen <port>` | Start the TCP server on `<port>`, enable UDP discovery on 5001, and start the heartbeat loop | `/listen 5000` |
| `/connect <ip> <port>` | Manually connect to a peer and initiate the RSA-AES key exchange | `/connect 127.0.0.1 5001` |
| `/peers` | List known peers with ID, address:port, and status (`Connected` / `Degraded` / `Discovered`) | `/peers` |
| `/create #<room>` | Create a new chat room locally and announce it to all peers | `/create #general` |
| `/join #<room>` | Join an existing room and announce membership | `/join #general` |
| `/leave #<room>` | Leave a room and announce departure | `/leave #general` |
| `/rooms` | List all known rooms with member count and join status | `/rooms` |
| `/msg #<room> <text>` | Send a message to every peer in the room | `/msg #general Hello` |
| `/msg @<peerId> <text>` | Send a direct encrypted message to one specific peer | `/msg @abc12345 Hi` |
| `/history` | Display the last 50 messages from `message_history.json` (oldest → newest) | `/history` |
| `/help` | Show the command list | `/help` |
| `/quit` | Disconnect cleanly and exit | `/quit` |
| *(plain text)* | Any non-command line is broadcast to every connected peer | `Hello everyone` |

---

## Architecture Diagram

```
Three-node P2P mesh — every node is both server and client.

                +-------- Node A --------+
                |                        |
                |  Server  PeerDiscovery |
                |  (TCP)   (UDP 5001)    |
                |    ^         ^         |
                |    |         |         |
                |  Client    Client      |
                |  (to B)    (to C)      |
                +-----+---------+--------+
                      |         |
                      |         |
              TCP     |         |    TCP
                      v         v
       +----- Node B -----+   +----- Node C -----+
       | Server / Disc.   |---| Server / Disc.   |
       | Client(->A)      |TCP| Client(->A)      |
       | Client(->C)      |   | Client(->B)      |
       +------------------+   +------------------+

Each node runs, in parallel:
  - 1 Accept loop      (Server listens on its TCP port)
  - 1 Receive loop     per connected peer
  - 1 Heartbeat loop   (sends Heartbeat every 5s to all peers)
  - 1 Discovery loop   (UDP broadcast every 5s on port 5001)
  - 1 Timeout checker  (drops silent peers after 30s)
  - 1 Send thread      (drains outgoing MessageQueue)
  - 1 Receive thread   (drains incoming MessageQueue -> UI + history)

A single TCP connection is full-duplex: whoever accepted and whoever
connected both read/write on the same pipe (see sprint-3 P2P FAQ).
```

### Component Descriptions

| Component | Responsibility |
|-----------|----------------|
| `Program.cs` | Main entry point, wires every subsystem together, runs the UI loop and the three background threads (receive / send / heartbeat) |
| `Core/Message.cs` | Message model: `Id`, `Sender`, `Content`, `Timestamp`, `Type`, `Room`, `Signature`, `EncryptedContent`, `PublicKey`, `TargetPeerId`, plus `MessageType` enum |
| `Core/MessageQueue.cs` | Thread-safe incoming/outgoing `BlockingCollection<Message>` queues |
| `Core/Peer.cs` | Per-peer state: ID, address, port, last-seen timestamp, TCP handles, session AES key, `KeyExchange` state, `ReconnectionPolicy`, outbound `Client` |
| `Network/Server.cs` | TCP listener, accept loop, per-peer receive loop, broadcast to all accepted peers |
| `Network/Client.cs` | Outbound TCP client, receive loop for connections we initiated, send with optional AES encryption + RSA signing |
| `Network/PeerDiscovery.cs` | UDP broadcast (`PEER:{peerId}:{tcpPort}`) every 5 s, listener, and 30 s timeout check |
| `Network/HeartbeatMonitor.cs` | Tracks `LastHeartbeat` per peer; raises `OnConnectionFailed` after 15 s of silence |
| `Network/ReconnectionPolicy.cs` | Exponential-backoff reconnection: 1, 2, 4, 8, 16 s (capped 30 s), max 5 attempts |
| `Security/KeyExchange.cs` | State machine for the RSA-AES handshake; wraps `RsaEncryption` and the generated AES session key |
| `Security/AesEncryption.cs` | AES-256-CBC. Encrypt prepends a random 16-byte IV to the ciphertext |
| `Security/RsaEncryption.cs` | RSA-2048 with OAEP-SHA256 padding for wrapping the AES session key |
| `Security/MessageSigner.cs` | RSA-SHA256 (PKCS#1 v1.5) signing and verification of message content |
| `UI/ConsoleUI.cs` | Parses slash-commands into `CommandResult`, formats incoming messages for display |
| `UI/MessageHistory.cs` | Persists `message_history.json`, lock-guarded, supports `/history` display |

---

## Protocol Specification

### Connection Establishment
Every TCP connection carries a two-message key exchange before any text traffic flows. Whoever calls `ConnectAsync` is the *initiator*; whoever accepts is the *responder*. Both endpoints read AND write on the same full-duplex TCP connection — no second connection is opened back the other way.

```
Initiator (A)                            Responder (B)
  |                                         |
  |--- TCP ConnectAsync(B.ip, B.port) ----->|  (B: AcceptTcpClientAsync returns)
  |                                         |
  |  Both wrap the TcpClient in a Peer,     |
  |  start a receive loop on a Task         |
  |                                         |
  |--- Message{Type=KeyExchange,         -->|
  |            PublicKey = A.RSApub}        |  B: kx.ReceivePublicKey(A.RSApub)
  |                                         |     kx.CreateEncryptedSessionKey()
  |<-- Message{Type=SessionKey,            -|     (generates AES-256 key,
  |            PublicKey = RSA_B(AES_key)}  |      RSA-encrypts with A.RSApub)
  |                                         |     kx.Complete()
  |  A: kx.ReceiveEncryptedSessionKey(..)   |
  |     sets SessionKey = AesEncryption(k)  |
  |                                         |
  |=== All subsequent messages =============|
  |    AES-256-CBC encrypted + RSA-SHA256 signed
```

### Message Flow
1. User types a command or plain text; `ConsoleUI.ParseCommand` produces a `CommandResult`.
2. `Program.cs` dispatches on `CommandType` (or treats plain text as a broadcast).
3. Outgoing `Message` objects are encrypted with the per-peer AES session key, signed with this node's RSA private key, framed as `[4-byte big-endian length][JSON]`, and written to the peer's `NetworkStream`.
4. On the receiving side, the per-peer receive loop reads the length prefix, deserializes the JSON, verifies the signature, decrypts `EncryptedContent` into `Content`, and raises `OnMessageReceived`.
5. Program.cs's handler enqueues `Text` / `RoomMessage` into `MessageQueue`. The `ReceiveThread` drains the queue, displays via `ConsoleUI`, and persists via `MessageHistory.SaveMessage`.

### Peer Discovery Protocol
UDP broadcast on port 5001. Every node broadcasts its presence every 5 s; every node listens on the same port. Peers silent for 30 s are evicted and `OnPeerLost` fires.

#### Broadcast Message Format
```
PEER:{peerId}:{tcpPort}

Example: PEER:abc12345:5000

- peerId: first 8 chars of Guid.NewGuid() generated at startup
- tcpPort: the port passed to /listen
- Encoding: UTF-8, sent to IPAddress.Broadcast:5001
```

#### Discovery Process
1. `/listen <port>` calls `PeerDiscovery.Start(tcpPort)`, which binds `UdpClient(5001)` and starts:
   - a listen thread (`ListenLoop`)
   - a broadcast thread (`BroadcastLoop`)
   - a background timeout checker (`TimeoutCheckLoop`)
2. Broadcast thread sends `PEER:{LocalPeerId}:{TcpPort}` every 5 s to `255.255.255.255:5001`.
3. Listen thread receives broadcasts. If the peer is new, it adds to `_knownPeers` and raises `OnPeerDiscovered`. If already known, it refreshes `LastSeen`.
4. A node ignores its own broadcasts by comparing the incoming `peerId` to `LocalPeerId`.
5. `Program.cs`'s `OnPeerDiscovered` handler automatically opens a TCP `Client` to the new peer and drives the key exchange.
6. Timeout checker runs every 5 s; if `now - peer.LastSeen > 30 s`, the peer is removed and `OnPeerLost` fires.

### Heartbeat Protocol
Implemented in `Network/HeartbeatMonitor.cs` and the heartbeat thread in `Program.cs`.

- **Interval:** 5 seconds (send a `MessageType.Heartbeat` to every peer)
- **Timeout:** 15 seconds (no heartbeat from a peer → mark it failed)
- **Monitor cadence:** the failure checker runs every ~1 s
- **Action on timeout:** raise `OnConnectionFailed(peerId)` → `Program.HandlePeerConnectionFailed` creates a fresh `Client`, wires its events, and spawns `ReconnectionPolicy.TryReconnect(peer)` on a background task.

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
Two independent layers:
1. **Heartbeat** (`Network/HeartbeatMonitor.cs`) — every peer is expected to emit a `MessageType.Heartbeat` at least every 5 s. The monitor loop checks each peer's `LastHeartbeat`; if it's older than 15 s, `OnConnectionFailed(peerId)` fires. Program.cs catches this and transitions the peer to the reconnection path.
2. **Discovery timeout** (`Network/PeerDiscovery.cs`) — if no UDP broadcast has been seen from a peer for 30 s, `OnPeerLost` fires. Program.cs disposes the outbound `Client`, stops monitoring the peer, and removes it from `_peers`.

`/peers` reflects both states:
- `Connected` — TCP up and a recent heartbeat
- `Degraded` — TCP up but no recent heartbeat (reconnection is in flight)
- `Discovered` — UDP-seen but not yet connected (or just reconnecting)

### Automatic Reconnection
Triggered by `HeartbeatMonitor.OnConnectionFailed`. A fresh `Client` is created (old streams disposed first), wired with the same event handlers, then `ReconnectionPolicy.TryReconnect(peer)` is launched via `Task.Run`.

- **Initial delay:** 1 second
- **Backoff strategy:** exponential, `delay = min(1000 * 2^(attempt-1), 30000)` ms → 1 s, 2 s, 4 s, 8 s, 16 s (capped at 30 s)
- **Max attempts:** 5
- On success, `ResetAttempts` is called, `OnReconnectSuccess` fires, and the key exchange runs again to re-establish a fresh AES session key.
- On exhaustion, `OnReconnectFailed` fires and the peer is marked disconnected in the UI.

### Graceful Degradation
- Every send in `BroadcastToAllPeersParallel` is wrapped in try/catch per peer; one dead peer never blocks broadcasts to the others.
- The receive loop on each peer catches stream exceptions, disposes the peer cleanly, and fires the disconnect event so one peer's failure never brings the node down.
- A peer in `Degraded` state is still kept in `/peers` so the user can see reconnection progress; it is removed only when the 30 s discovery timeout expires without a broadcast.

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
`message_history.json` in the application's working directory (alongside the executable). The file is created on first message and loaded on startup. If the file is missing or corrupted, the application initiates an empty history and logs a warning. The file is stored as plaintext JSON; the optional encrypted-history bonus feature is **not** implemented.

### History Commands
- `/history` — displays the last 50 messages oldest-to-newest in the console
- History is saved automatically on every received message; no manual save command needed
- Thread safety is ensured with a `Lock` object so concurrent messages never corrupts the file

---

## User Guide

### Getting Started
1. Clone the repository and `cd` into it.
2. `dotnet build SecureMessenger.csproj`
3. Open two (or more) terminal windows. In each one, run `dotnet run`.
4. In the first terminal: `/listen 5000`. In the second: `/listen 5001`. The nodes should discover each other automatically within a few seconds on the same subnet.
5. Type a plain text line in any terminal; it broadcasts to every connected peer.

### Connecting to Peers
- **Automatic (preferred):** `/listen <port>` is enough. `PeerDiscovery` broadcasts on UDP 5001 and connects to every peer it hears from.
- **Manual:** `/connect <ip> <port>` opens a TCP connection directly, runs the RSA-AES key exchange, and adds the peer to `_peers`. Use this when discovery is blocked (firewall, different subnet, or a second same-host instance that can't bind UDP 5001).

### Sending Messages
- **Broadcast:** type any non-command line and press Enter — goes to every peer via `BroadcastToAllPeersParallel`.
- **Room:** `/create #general`, then `/join #general`, then `/msg #general Hi everyone`. Peers who have also joined `#general` will see it.
- **Direct:** `/msg @abc12345 Hello` sends an encrypted DM to the peer with ID `abc12345` only. Look up peer IDs with `/peers`.

### Viewing Peer Status
`/peers` prints each known peer as:
```
<peerId>  <ip>:<port>  <Status>   Last seen: <timestamp>
```
Status is one of `Connected` (TCP + recent heartbeat), `Degraded` (TCP up, heartbeat stale), or `Discovered` (UDP-seen only, not yet connected).

### Troubleshooting
| Problem | Solution |
|---------|----------|
| "Address already in use" | The old TCP port is still bound. Wait ~30 s or pick a different `/listen` port. |
| Peer not discovered | Firewall may be blocking UDP 5001. Allow inbound UDP on that port, or fall back to `/connect <ip> <port>`. |
| Only the first node on one machine discovers peers | Expected — `PeerDiscovery` binds UDP 5001 and there's only one of it per host. Second+ instances on the same machine must use `/connect` manually. See Known Issues. |
| Peer shown as `Degraded` | Heartbeat has stalled (>15 s since last one). The reconnection policy is already retrying — give it up to ~30 s. |
| "Padding is invalid" exception | The key exchange didn't complete before an encrypted message was sent. Reconnect with `/connect`; make sure both sides log the `SessionKey` handshake before any text traffic. |
| Messages not showing on `/history` | Check that `message_history.json` exists in the run directory and is not locked by another process. |

---

## Features Implemented

### Core Features
- [x] P2P architecture (no central server)
- [x] Peer discovery (UDP broadcast)
- [x] Automatic peer connection
- [x] Heartbeat monitoring
- [x] Failure detection
- [x] Automatic reconnection
- [x] Message history (file-based)
- [x] Parallel message processing

### Security Features (from Sprint 2)
- [x] AES encryption
- [x] RSA key exchange
- [x] Message signing

### Bonus Features (if implemented)
- [ ] Message relay through intermediate peers
- [ ] Encrypted history storage
- [x] Peer persistence (save/load known peers) — `Network/PeerStore.cs` writes `known_peers.json` on every discovery; on `/listen`, entries younger than 7 days are reconnected in parallel (max 8 at a time)

---

## Testing Performed

### P2P Tests
| Test | Expected Result | Actual Result | Pass/Fail |
|------|-----------------|---------------|-----------|
| 3+ peers can form mesh | All three peers appear in every node's `/peers` within ~5 s of `/listen` | Mesh forms as expected; every node lists both other peers as `Connected` | Pass |
| Peer discovery works | New peer auto-appears in `/peers` within one discovery interval | New peer discovered via UDP within ~5 s, TCP handshake completes immediately after | Pass |
| Peer leaving detected | Peer removed from `/peers` after `/quit` or 30 s silence | Peer transitions `Connected` → `Degraded` at 15 s, removed at 30 s | Pass |
| Reconnection after failure | Connection restored once peer returns | `ReconnectionPolicy` retries on 1, 2, 4, 8, 16 s schedule; succeeds when peer is back | Pass |

### Resilience Tests
| Test | Expected Result | Actual Result | Pass/Fail |
|------|-----------------|---------------|-----------|
| Kill peer process | `OnConnectionFailed` fires within 15 s; peer marked `Degraded` | Heartbeat timeout observed at ~15 s; peer marked `Degraded` and reconnection starts | Pass |
| Network interruption | Exponential-backoff reconnect attempts fire until peer is reachable again | 5 attempts observed with 1/2/4/8/16 s delays between them | Pass |
| Peer rejoins | Connection restored, key exchange re-runs, messages flow again | Reconnect succeeds; fresh `KeyExchange` completes; messages resume | Pass |

---

## Known Issues

| Issue | Description | Severity | Workaround |
|-------|-------------|----------|------------|
| UDP 5001 fixed-port bind | `PeerDiscovery` calls `new UdpClient(5001)` with no fallback, so only one node per host can bind the discovery socket. Additional same-host instances won't auto-discover. | Low — only affects multi-instance testing on one machine | Use `/connect 127.0.0.1 <port>` manually between same-host instances. On separate machines the issue doesn't apply. |

---

## Future Improvements

- Port auto-select for UDP discovery (try 5001, 5002, 5003... until one binds) so multiple nodes can run on the same machine without manual `/connect`.
- Simultaneous-discovery tie-break using the `myId > theirId` rule from the P2P FAQ — currently both sides may race to connect on a fresh discovery.
- Encrypted message history on disk (bonus feature).
- Message relay for partial-mesh topologies (bonus feature).
- Cross-platform GUI using Avalonia or .NET MAUI (bonus feature).

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
- [ ] Peer persistence: kill all nodes, restart, show saved peers reconnect on `/listen` before discovery would run

**Demo video link:** [TBD — fill in]

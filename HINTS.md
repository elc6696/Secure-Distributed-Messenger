# Concepts Reference Guide

This guide explains key concepts you'll need. See Microsoft docs for full API details.

---

## Sprint 1: Using the Starter Code

The starter code provides `Server` and `Client` classes - you implement the methods inside them.

```
┌─────────────────┐                    ┌─────────────────────┐
│     Server      │◄─── connection ────│       Client        │
│                 │                    │                     │
│  /listen 5000   │                    │  /connect host port │
│                 │                    │                     │
│  You implement: │                    │  You implement:     │
│  - Start()      │                    │  - ConnectAsync()   │
│  - AcceptClients│                    │  - ReceiveAsync()   │
│  - ReceiveFrom..│                    │  - Send()           │
└─────────────────┘                    └─────────────────────┘
```

**How the pieces connect in Program.cs:**

1. Create instances of both classes
2. Subscribe to their events to handle connections/messages
3. When user types `/listen` → call `_server.Start(port)`
4. When user types `/connect` → call `_client.ConnectAsync(host, port)`
5. Events fire automatically as things happen

**Test with two terminals:**
- Terminal 1: `/listen 5000` (waits for connections)
- Terminal 2: `/connect 127.0.0.1 5000` (connects to Terminal 1)

**Wiring up events in Program.cs:**
```csharp
// Create instances
_server = new Server();
_client = new Client();

// Subscribe to events - these fire when things happen
_server.OnClientConnected += (endpoint) => { /* new connection */ };
_server.OnClientDisconnected += (endpoint) => { /* client left */ };
_server.OnMessageReceived += (message) => { /* got message */ };

_client.OnConnected += (endpoint) => { /* connected to server */ };
_client.OnDisconnected += (endpoint) => { /* disconnected */ };
_client.OnMessageReceived += (message) => { /* got message */ };
```

### Sprint 2: Add Encryption Layer

Your Sprint 1 networking code stays the same. You add encryption *around* it:

1. Before sending → encrypt the content
2. After receiving → decrypt the content
3. On connect → exchange keys first

### Sprint 3: Upgrade to Peer Model

In Sprint 3, you refactor to use the `Peer` class for richer connection tracking:

1. Replace `List<TcpClient>` with `List<Peer>`
2. Change events from `Action<string>` to `Action<Peer>`
3. Add PeerDiscovery for automatic peer finding
4. Add HeartbeatMonitor for connection health
5. Add ReconnectionPolicy for fault tolerance

Every instance runs both server AND client simultaneously. Your `/listen` starts the server, `/connect` uses the client, and both can be active.

---

## Events and Actions

An `Action<T>` is a delegate - a reference to a method. Events let one class notify others.

**The pattern:**
- Declare: `public event Action<string>? OnSomething;`
- Invoke (inside class): `OnSomething?.Invoke("data");`
- Subscribe (outside class): `obj.OnSomething += (data) => { /* handle */ };`

**Why `?.Invoke()`?** The event might have no subscribers (null). The `?.` safely checks first.

**Why `+=`?** Multiple handlers can subscribe. Each one gets called when the event fires.

---

## BlockingCollection<T>

A thread-safe queue where `Take()` **blocks** (waits) when empty. Perfect for producer/consumer.

**Key methods:**
- `Add(item)` - puts item in queue (never blocks)
- `Take()` - gets item, blocks if empty
- `Take(token)` - blocks until item OR token cancelled
- `TryTake(out item)` - non-blocking, returns true/false
- `CompleteAdding()` - signals shutdown, unblocks waiting Take() calls

**Why blocking matters:** Without it, your consumer thread would spin in a loop wasting CPU. With blocking, it efficiently waits.

**Note:** MessageQueue is optional for Sprint 1. The simplest approach is to handle messages directly in event handlers. MessageQueue is useful if you want a more sophisticated producer/consumer architecture.

---

## Threads and Tasks

**Starting work on a background thread:**
```csharp
var thread = new Thread(MethodName);
thread.IsBackground = true;  // Won't prevent app exit
thread.Start();
```

**Or with Task:**
```csharp
_ = Task.Run(() => DoWork());  // Fire and forget
```

**Cancellation pattern:**
```csharp
while (!_cancellationTokenSource.IsCancellationRequested)
{
    // do work
}
```

---

## Locking

Use `lock` when multiple threads access the same data.

**The pattern:**
```csharp
private readonly object _clientsLock = new();
private readonly List<TcpClient> _clients = new();

// In any method that touches _clients:
lock (_clientsLock)
{
    // safe to access _clients here
}
```

**Important:**
- Always use the SAME lock object for the same data
- Return copies, not the original: `return _clients.ToList();`
- Don't hold locks during slow operations (network I/O)

---

## TCP Basics

**Server side (TcpListener):**
1. Create listener on a port
2. Call `Start()` to begin listening
3. Call `AcceptTcpClientAsync()` to wait for a connection
4. Get `NetworkStream` from the client
5. Read/write bytes over the stream

**Client side (TcpClient):**
1. Create TcpClient
2. Call `ConnectAsync(host, port)`
3. Get `NetworkStream` with `GetStream()`
4. Read/write bytes over the stream

**Message Framing (Length-Prefix):**

The starter code uses length-prefix framing for messages:
```
┌─────────────┬────────────────────────────┐
│ 4 bytes     │ N bytes                    │
│ (length N)  │ (JSON payload)             │
└─────────────┴────────────────────────────┘
```

**Sending:**
```csharp
var json = JsonSerializer.Serialize(message);
var payload = Encoding.UTF8.GetBytes(json);
var lengthPrefix = BitConverter.GetBytes(payload.Length);

stream.Write(lengthPrefix, 0, 4);
stream.Write(payload, 0, payload.Length);
```

**Receiving:**
```csharp
var lengthBuffer = new byte[4];
await stream.ReadAsync(lengthBuffer, 0, 4);
var messageLength = BitConverter.ToInt32(lengthBuffer, 0);

var messageBuffer = new byte[messageLength];
// Read full payload (may require multiple reads)
await stream.ReadAsync(messageBuffer, 0, messageLength);

var json = Encoding.UTF8.GetString(messageBuffer);
var message = JsonSerializer.Deserialize<Message>(json);
```

---

## Common Pitfalls

1. **Forgetting null check on events** → Use `?.Invoke()` not just `Invoke()`

2. **Returning internal collection** → Return `.ToList()` copy instead

3. **Blocking UI thread** → Network code should run on background threads

4. **Not handling closed connections** → Check for `bytesRead == 0`

5. **Race condition on shared data** → Use lock or concurrent collections

6. **Partial reads** → TCP doesn't guarantee full messages arrive together. Loop until you've read the expected number of bytes.

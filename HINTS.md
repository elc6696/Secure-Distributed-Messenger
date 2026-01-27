# Concepts Reference Guide

This guide explains key concepts you'll need. See Microsoft docs for full API details.

---

## Sprint 1: Using the Starter Code

The starter code provides `TcpServer` and `TcpClientHandler` classes - you implement the methods inside them.

```
┌─────────────────┐                    ┌─────────────────────┐
│    TcpServer    │◄─── connection ────│   TcpClientHandler  │
│                 │                    │                     │
│  /listen 5000   │                    │  /connect host port │
│                 │                    │                     │
│  You implement: │                    │  You implement:     │
│  - Start()      │                    │  - ConnectAsync()   │
│  - ListenLoop() │                    │  - ReceiveLoop()    │
│  - ReceiveLoop()│                    │  - SendAsync()      │
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
_server = new TcpServer();
_client = new TcpClientHandler();

// Subscribe to events - these fire when things happen
_server.OnPeerConnected += (peer) => { /* new connection */ };
_server.OnMessageReceived += (peer, msg) => { /* got message */ };

_client.OnConnected += (peer) => { /* connected to server */ };
_client.OnMessageReceived += (peer, msg) => { /* got message */ };
```

### Sprint 2: Add Encryption Layer

Your Sprint 1 networking code stays the same. You add encryption *around* it:

1. Before sending → encrypt the content
2. After receiving → decrypt the content
3. On connect → exchange keys first

### Sprint 3: Everyone is Both

In P2P, every instance runs both server AND client simultaneously. Your `/listen` starts the server, `/connect` uses the client, and both can be active.

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
while (!token.IsCancellationRequested)
{
    // do work
}
```

---

## Locking

Use `lock` when multiple threads access the same data.

**The pattern:**
```csharp
private readonly object _lock = new();
private readonly List<Peer> _peers = new();

// In any method that touches _peers:
lock (_lock)
{
    // safe to access _peers here
}
```

**Important:**
- Always use the SAME lock object for the same data
- Return copies, not the original: `return _peers.ToList();`
- Don't hold locks during slow operations (network I/O)

---

## TCP Basics

**Server side (TcpListener):**
1. Create listener on a port
2. Call `Start()` to begin listening
3. Call `AcceptTcpClient()` to wait for a connection (blocks!)
4. Get `NetworkStream` from the client
5. Read/write using StreamReader/StreamWriter

**Client side (TcpClient):**
1. Create TcpClient
2. Call `ConnectAsync(host, port)`
3. Get `NetworkStream` with `GetStream()`
4. Read/write using StreamReader/StreamWriter

**Reading/Writing text:**
- `StreamWriter.WriteLine()` sends a line
- `StreamReader.ReadLine()` receives a line (blocks until newline received)
- If `ReadLine()` returns null, the connection closed

---

## Common Pitfalls

1. **Forgetting null check on events** → Use `?.Invoke()` not just `Invoke()`

2. **Returning internal collection** → Return `.ToList()` copy instead

3. **Blocking UI thread** → Network code should run on background threads

4. **Not handling closed connections** → Check for null from ReadLine()

5. **Race condition on shared data** → Use lock or concurrent collections

# Sprint 1 Documentation
## Secure Distributed Messenger

**Team Name:** Group 25

**Team Members:**
- Donald Tsang - Multi-Threaded Architecture
- Ethan Chang - Basic TCP Communication
- Cooper Miles - Console UI
- [Name 4] - [Role/Responsibilities]
- [Name 5] - [Role/Responsibilities]

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

- **Main Thread:** [Purpose]
- **Receive Thread:** [Purpose]
- **Send Thread:** [Purpose]
- [Additional threads...]

### Thread-Safe Message Queue
[Describe your message queue implementation and synchronization approach]

---

## Features Implemented

- [x] Multi-threaded architecture
- [ ] Thread-safe message queue
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
| Two instances can connect | Connection established | | |
| Messages sent and received | Message appears on other instance | | |
| Disconnection handled | No crash, appropriate message | | |
| Thread safety under load | No race conditions | | |

---

## Known Issues

| Issue | Description | Workaround |
|-------|-------------|------------|
| | | |

---

## Video Demo Checklist

Your demo video (3-5 minutes) should show:
- [ ] Starting two instances of the application
- [ ] Connecting the instances
- [ ] Sending messages in both directions
- [ ] Disconnecting gracefully
- [ ] (Optional) Showing thread-safe behavior under load

# Sprint 1 Documentation
## Secure Distributed Messenger

**Team Name:** Group 25

**Team Members:**
- Donald Tsang - [Role/Responsibilities]
- Ethan Chang - [Role/Responsibilxities]
- Cooper Miles - [Role/Responsibilities]
- Teju Rajbabu - [Role/Responsibilities]
- N/A - N/A

**Date:** [Submission Date]

---

## Build Instructions

### Prerequisites
- [List required software, e.g., .NET SDK version]
- [Any other dependencies]

### Building the Project
```
[Commands to build the project]
```

---

## Run Instructions

### Starting the Application
```
[Commands to run the application]
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

- [ ] Multi-threaded architecture
- [ ] Thread-safe message queue
- [ ] TCP server (listen for connections)
- [ ] TCP client (connect to peers)
- [ ] Send/receive text messages
- [ ] Graceful disconnection handling
- [ ] Console UI with commands

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

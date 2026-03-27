# Sprint 2 Documentation
## Secure Distributed Messenger

**Team Name:** Group 25

**Team Members:**
- Donald Tsang - AES Encryption
- Ethan Chang - RSA Encryption, Secure Key Exchange Protocol
- Cooper Miles - Chat Rooms
- Teju - Message Authentication

**Date:** 3/27/2026

---

## Build & Run Instructions

See [Sprint 1 Documentation - Build & Run Instructions](sprint-1-documentation.md#build-instructions).

---

## Security Protocol Overview

### Encryption Protocol

#### Key Exchange Process
[Describe step-by-step how keys are exchanged when two peers connect]

1. [Step 1]
2. [Step 2]
3. [Step 3]
4. ...

#### Message Encryption
Messages are encrypted using AES-256-CBC before being sent over the network. Each message uses a freshly generated random IV, which is attached to the ciphertext so the receiver can extract it for decryption.

- **Algorithm:** AES-256-CBC
- **Key Size:** 256 bits (32 bytes)
- **IV Generation:** Random 16-byte IV generated per message via `Aes.GenerateIV()`
- **Wire Format:** `[IV (16 bytes)][Ciphertext (variable length)]`

#### Message Signing
[Describe how messages are signed and verified]

- **Algorithm:** [e.g., RSA with SHA-256]
- **Key Size:** [e.g., 2048 bits]

---

## Key Management

### Key Generation
[Describe when and how keys are generated]

- **RSA Key Pair:** [When generated, how stored]
- **AES Session Key:** Generated via `AesEncryption.GenerateKey()` using `Aes.Create()` with `KeySize = 256` and `GenerateKey()`. Returns a random 32-byte key.

### Key Storage
The AES session key is stored as a `byte[]` field (`_key`) inside the `AesEncryption` instance, held in memory for the duration of the session. It is not persisted to disk.

[Include RSA here]

### Key Lifetime
| Key Type | Generated When | Expires When |
|----------|----------------|--------------|
| RSA Key Pair | | |
| AES Session Key | Generated per session via `AesEncryption.GenerateKey()` | When connection ends |

---

## Wire Protocol

### Message Format
```
[Describe your message format, e.g.:]
[4 bytes: length][1 byte: type][payload]
```

### Message Types
| Type ID | Name | Description |
|---------|------|-------------|
| 0x01 | PUBLIC_KEY | RSA public key exchange |
| 0x02 | SESSION_KEY | Encrypted AES session key |
| 0x03 | MESSAGE | Encrypted chat message |
| 0x04 | SIGNED_MESSAGE | Signed and encrypted message |
| | | |

---

## Threat Model

### Assets Protected
- [What are you protecting? e.g., message content, user identity]

### Threats Addressed
| Threat | Mitigation |
|--------|------------|
| Eavesdropping | AES encryption of all messages |
| Man-in-the-middle | [Your mitigation] |
| Message tampering | Digital signatures |
| Replay attacks | [Your mitigation, if any] |
| | |

### Known Limitations
[What threats are NOT addressed by your implementation?]

---

## Features Implemented

- [x] AES encryption of messages
- [ ] RSA key pair generation
- [ ] RSA key exchange
- [ ] AES session key exchange (encrypted with RSA)
- [ ] Message signing
- [ ] Signature verification
- [ ] Multiple simultaneous conversations
- [ ] Per-conversation encryption keys

---

## Testing Performed

### Security Tests
| Test | Expected Result | Actual Result | Pass/Fail |
|------|-----------------|---------------|-----------|
| Messages are encrypted on wire | Cannot read plaintext in network capture | Encrypted bytes returned by `Encrypt()` are unreadable without the key | Pass |
| Key exchange completes | Both peers have shared session key | | |
| Tampered message rejected | Signature verification fails | | |
| Different keys per conversation | Each peer pair has unique keys | | |

---

## Known Issues

| Issue | Description | Workaround |
|-------|-------------|------------|
| | | |

---

## Video Demo Checklist

Your demo video (5-7 minutes) should show:
- [ ] Two peers connecting and exchanging keys
- [ ] Sending encrypted messages
- [ ] Showing that messages are encrypted (e.g., log output)
- [ ] Demonstrating signature verification
- [ ] Showing what happens with a tampered message (if possible)
- [ ] Multiple simultaneous conversations

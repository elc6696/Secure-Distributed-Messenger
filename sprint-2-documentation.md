# Sprint 2 Documentation
## Secure Distributed Messenger

**Team Name:** Group 25

**Team Members:**
- Donald Tsang - AES Encryption, Documentation
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
When two peers connect, they interact with each other to establish a shared AES session key:

1. Both peers generate an RSA key pair (`KeyExchange` constructor creates a new `RsaEncryption` instance).
2. Both peers exchange their RSA public keys (`GetPublicKey()` / `ReceivePublicKey()`).
3. The initiating peer generates a random AES-256 session key (`AesEncryption.GenerateKey()`).
4. The initiator encrypts the session key with the responder's RSA public key (`CreateEncryptedSessionKey()`).
5. The responder decrypts the session key using their RSA private key (`ReceiveEncryptedSessionKey()`).
6. Both peers now share the same AES session key, then the state transitions to `Established`.

#### Message Encryption
Messages are encrypted using AES-256-CBC before being sent over the network. Each message uses a freshly generated random IV, which is attached to the ciphertext so the receiver can extract it for decryption.

- **Algorithm:** AES-256-CBC
- **Key Size:** 256 bits (32 bytes)
- **IV Generation:** Random 16-byte IV generated per message via `Aes.GenerateIV()`
- **Wire Format:** `[IV (16 bytes)][Ciphertext (variable length)]`

#### Message Signing
Outgoing messages are signed with the sender's RSA private key. The receiver verifies the signature using the sender's public key. Any message with an invalid or missing signature is rejected.

- **Algorithm:** RSA with SHA-256
- **Padding:** PKCS#1 v1.5 (`RSASignaturePadding.Pkcs1`)
- **Signing:** `MessageSigner.SignData()` calls `_rsa.SignData(data, SHA256, Pkcs1)` with the sender's private key
- **Verification:** `MessageSigner.VerifyData()` imports the sender's public key and calls `VerifyData()`. What it does it that it prints a warning and returns `false` if the signature is invalid or a `CryptographicException` is thrown

---

## Key Management

### Key Generation

- **RSA Key Pair:** Generated at startup when `KeyExchange` is constructed. It creates a new `RsaEncryption` instance which calls `RSA.Create(2048)` to produce a 2048-bit key pair.
- **AES Session Key:** Generated via `AesEncryption.GenerateKey()` using `Aes.Create()` with `KeySize = 256` and `GenerateKey()`. Returns a random 32-byte key.

### Key Storage
The AES session key is stored as a `byte[]` field (`_key`) inside the `AesEncryption` instance, held in memory for the duration of the session. It is not persisted to disk.

The RSA key pair is stored as an `RSA` field (`_rsa`) inside the `RsaEncryption` instance, held in memory. Only the public key is ever transmitted to peers; the private key never leaves the instance.

### Key Lifetime
| Key Type | Generated When | Expires When |
|----------|----------------|--------------|
| RSA Key Pair | On peer connection (`KeyExchange` constructor) | When connection ends / instance disposed |
| AES Session Key | Generated per session via `AesEncryption.GenerateKey()` | When connection ends |

---

## Wire Protocol

### Message Format
Messages are framed using a length-prefix scheme over TCP. Each message is a JSON-serialized `Message` object preceded by a 4-byte little-endian integer indicating the payload length.

```
[4 bytes: payload length (little-endian int32)][N bytes: UTF-8 JSON payload]
```

The JSON payload maps to the `Message` class with the following fields relevant to Sprint 2:

| Field | Type | Description |
|-------|------|-------------|
| `Type` | `MessageType` enum | Indicates how the message is handled |
| `Sender` | string | Sender identifier |
| `Content` | string | Plaintext content (empty when encrypted) |
| `EncryptedContent` | byte[]? | AES-encrypted message bytes (replaces `Content` when session key exists) |
| `Signature` | byte[]? | RSA-SHA256 signature over the plaintext content |
| `PublicKey` | byte[]? | Sender's RSA public key (included with every signed message) |

### Message Types
| Name | Enum Value | Description |
|------|------------|-------------|
| `Text` | 0 | Regular chat message (plaintext in Sprint 1, encrypted in Sprint 2) |
| `KeyExchange` | 1 | RSA public key exchange during handshake |
| `SessionKey` | 2 | AES session key encrypted with peer's RSA public key |

---

## Threat Model

### Assets Protected
- Message content (protected from eavesdroppers via AES-256-CBC encryption)
- Message integrity (protected from tampering via RSA-SHA256 digital signatures)
- Session key confidentiality (AES session key is transmitted encrypted with the peer's RSA public key)

### Threats Addressed
| Threat | Mitigation |
|--------|------------|
| Eavesdropping | All message content is AES-256-CBC encrypted; plaintext is never sent after key exchange |
| Man-in-the-middle | No mitigation implemented; see Known Limitations |
| Message tampering | Every message is signed with the sender's RSA private key; invalid signatures are rejected |
| Replay attacks | No mitigation implemented; see Known Limitations |

### Known Limitations
- **No peer authentication:** Public keys are accepted without verification, so there is no protection against a man-in-the-middle who substitutes their own public key during the handshake.
- **No replay attack protection:** Messages have no sequence numbers or timestamps checked during verification, so captured messages could be replayed.
- **Keys not persisted:** RSA key pairs are generated fresh each run, so there is no long-term identity for peers.

---

## Features Implemented

- [x] AES encryption of messages
- [x] RSA key pair generation
- [x] RSA key exchange
- [x] AES session key exchange (encrypted with RSA)
- [x] Message signing
- [x] Signature verification
- [x] Multiple simultaneous conversations
- [x] Per-conversation encryption keys

---

## Testing Performed

### Security Tests
| Test | Expected Result | Actual Result | Pass/Fail |
|------|-----------------|---------------|-----------|
| Messages are encrypted on wire | Cannot read plaintext in network capture | Encrypted bytes returned by `Encrypt()` are unreadable without the key | Pass |
| Key exchange completes | Both peers have shared session key | Both peers successfully complete handshake and share AES session key | Pass |
| Tampered message rejected | Signature verification fails | `VerifyData()` returns false and message is dropped | Pass |
| Different keys per conversation | Each peer pair has unique keys | Each session generates a unique AES key via `AesEncryption.GenerateKey()` | Pass |

---

## Known Issues

| Issue | Description | Workaround |
|-------|-------------|------------|
| `/create` room name parsed incorrectly | The `/create` command expects a string room name (e.g. `/create #room`), but was initially implemented expecting an integer. Room names are strings, not numbers. | Use `/create #<name>` with a string name as intended |

---

## Video Demo Checklist

Your demo video (5-7 minutes) should show:
- [x] Two peers connecting and exchanging keys
- [x] Sending encrypted messages
- [x] Showing that messages are encrypted (e.g., log output)
- [x] Demonstrating signature verification
- [x] Showing what happens with a tampered message (if possible)
- [x] Multiple simultaneous conversations

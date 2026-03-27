// Ethan Chang
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 2: Security & Encryption
// Due: Week 10 | Work on: Weeks 6-9
//

using System.Security.Cryptography;
using SecureMessenger.Core;

namespace SecureMessenger.Security;

/// <summary>
/// Sprint 2: Key exchange protocol handler.
/// Manages the handshake process between peers to establish a shared session key.
///
/// Key Exchange Protocol:
/// 1. Both peers generate RSA key pairs
/// 2. Peers exchange public keys
/// 3. One peer (initiator) generates an AES session key
/// 4. Initiator encrypts session key with responder's public key
/// 5. Responder decrypts session key with their private key
/// 6. Both peers now share the same AES session key for encryption
///
/// State Machine:
/// Disconnected -> SendingPublicKey -> ReceivingPublicKey ->
/// SendingSessionKey/ReceivingSessionKey -> Established
/// </summary>
public enum ConnectionState
{
    Disconnected,
    SendingPublicKey,
    ReceivingPublicKey,
    SendingSessionKey,
    ReceivingSessionKey,
    Established
}

public class KeyExchange
{
    private RsaEncryption? _rsa;
    private byte[]? _peerPublicKey;
    private byte[]? _sessionKey;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public byte[]? SessionKey => _sessionKey;

    /// <summary>
    /// Initialize the key exchange by creating our RSA key pair.
    ///
    /// TODO: Implement the following:
    /// 1. Create a new RsaEncryption instance
    /// </summary>
    public KeyExchange()
    {
        _rsa = new RsaEncryption();
    }

    /// <summary>
    /// Get our public key to send to the peer.
    ///
    /// TODO: Implement the following:
    /// 1. Update State to SendingPublicKey
    /// 2. Return our public key using _rsa.ExportPublicKey()
    /// </summary>
    public byte[] GetPublicKey()
    {
        State = ConnectionState.SendingPublicKey;
        return _rsa.ExportPublicKey();
    }

    /// <summary>
    /// Store the peer's public key when received.
    ///
    /// TODO: Implement the following:
    /// 1. Store the peer's public key in _peerPublicKey
    /// 2. Update State to ReceivingPublicKey
    /// </summary>
    public void ReceivePublicKey(byte[] peerPublicKey)
    {
        State = ConnectionState.ReceivingPublicKey;
        _peerPublicKey = peerPublicKey;
    }

    /// <summary>
    /// Generate a new AES session key and encrypt it for the peer (initiator side).
    ///
    /// TODO: Implement the following:
    /// 1. Generate a new AES key using AesEncryption.GenerateKey()
    /// 2. Store it in _sessionKey
    /// 3. Update State to SendingSessionKey
    /// 4. Encrypt the session key with peer's public key using _rsa.EncryptSessionKey()
    /// 5. Return the encrypted session key
    /// </summary>
    public byte[] CreateEncryptedSessionKey()
    {
        _sessionKey = AesEncryption.GenerateKey();
        State = ConnectionState.SendingSessionKey;
        byte[] encrypt_key = _rsa.EncryptSessionKey(_sessionKey, _peerPublicKey);
        return encrypt_key;
    }

    /// <summary>
    /// Decrypt the received session key (responder side).
    ///
    /// TODO: Implement the following:
    /// 1. Decrypt the encrypted key using _rsa.DecryptSessionKey()
    /// 2. Store the decrypted key in _sessionKey
    /// 3. Update State to Established
    /// </summary>
    public void ReceiveEncryptedSessionKey(byte[] encryptedKey)
    {
        _sessionKey = _rsa.DecryptSessionKey(encryptedKey);
        State = ConnectionState.Established;
    }

    /// <summary>
    /// Mark the key exchange as complete (initiator side, after sending session key).
    ///
    /// TODO: Implement the following:
    /// 1. Update State to Established
    /// </summary>
    public void Complete()
    {
        State = ConnectionState.Established;
    }

    /// <summary>
    /// Check if key exchange is complete and we have a valid session key.
    /// </summary>
    public bool IsEstablished => State == ConnectionState.Established && _sessionKey != null;
}

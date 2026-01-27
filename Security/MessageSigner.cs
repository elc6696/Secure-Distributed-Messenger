// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 2: Security & Encryption
// Due: Week 10 | Work on: Weeks 6-9
//

using System.Security.Cryptography;

namespace SecureMessenger.Security;

/// <summary>
/// Sprint 2: Message signing and verification.
/// Uses RSA with SHA-256 for digital signatures.
///
/// Digital Signature Configuration:
/// - Algorithm: RSA with SHA-256
/// - Padding: PKCS#1 v1.5 (RSASignaturePadding.Pkcs1)
///
/// Purpose:
/// - Signing proves the message came from the claimed sender
/// - Verification detects if the message was tampered with
/// - Reject any message with an invalid signature
/// </summary>
public class MessageSigner
{
    private readonly RSA _rsa;

    /// <summary>
    /// Create a MessageSigner with an RSA key pair.
    /// Use your own RSA instance for signing outgoing messages.
    /// </summary>
    public MessageSigner(RSA rsa)
    {
        _rsa = rsa;
    }

    /// <summary>
    /// Sign data with our private key.
    ///
    /// TODO: Implement the following:
    /// 1. Use _rsa.SignData() with:
    ///    - The data bytes to sign
    ///    - HashAlgorithmName.SHA256
    ///    - RSASignaturePadding.Pkcs1
    /// 2. Return the signature bytes
    /// </summary>
    public byte[] SignData(byte[] data)
    {
        throw new NotImplementedException("Implement SignData() - see TODO in comments above");
    }

    /// <summary>
    /// Verify a message signature with the sender's public key.
    ///
    /// TODO: Implement the following:
    /// 1. Create a new RSA instance for the sender's public key
    /// 2. Import the sender's public key
    /// 3. Use VerifyData() with:
    ///    - The original data bytes
    ///    - The signature bytes
    ///    - HashAlgorithmName.SHA256
    ///    - RSASignaturePadding.Pkcs1
    /// 4. If verification fails:
    ///    - Print a warning: "WARNING: Invalid signature detected - message may be tampered!"
    /// 5. Handle CryptographicException:
    ///    - Print error: "ERROR: Failed to verify signature - rejecting message"
    ///    - Return false
    /// 6. Return the verification result (true if valid, false if invalid)
    ///
    /// Security Note: Always reject messages with invalid signatures!
    /// </summary>
    public bool VerifyData(byte[] data, byte[] signature, byte[] publicKey)
    {
        throw new NotImplementedException("Implement VerifyData() - see TODO in comments above");
    }
}

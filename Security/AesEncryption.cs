// [Your Name Here]
// CSCI 251 - Secure Distributed Messenger
//
// SPRINT 2: Security & Encryption
// Due: Week 10 | Work on: Weeks 6-9
//

using System.Security.Cryptography;

namespace SecureMessenger.Security;

/// <summary>
/// Sprint 2: AES encryption for message content.
/// Uses AES-256-CBC with random IV for each message.
///
/// AES-256 Configuration:
/// - Key size: 256 bits (32 bytes)
/// - Block size: 128 bits (16 bytes)
/// - Mode: CBC (Cipher Block Chaining)
/// - IV: Random 16 bytes, prepended to ciphertext
///
/// Wire format: [IV (16 bytes)][Ciphertext (variable length)]
/// </summary>
public class AesEncryption
{
    private readonly byte[] _key;

    /// <summary>
    /// Create with existing key (32 bytes for AES-256)
    /// </summary>
    public AesEncryption(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("AES-256 requires a 32-byte key", nameof(key));
        _key = key;
    }

    /// <summary>
    /// Generate a new random AES-256 key.
    ///
    /// TODO: Implement the following:
    /// 1. Create an Aes instance using Aes.Create()
    /// 2. Set KeySize to 256
    /// 3. Call GenerateKey() to create a random key
    /// 4. Return the generated key bytes
    ///
    /// Hint: Use a 'using' statement for proper disposal
    /// </summary>
    public static byte[] GenerateKey()
    {
        throw new NotImplementedException("Implement GenerateKey() - see TODO in comments above");
    }

    /// <summary>
    /// Encrypt plaintext message using AES-256-CBC.
    ///
    /// TODO: Implement the following:
    /// 1. Create an Aes instance and configure:
    ///    - Set Key to _key
    ///    - Set Mode to CipherMode.CBC
    ///    - Generate a random IV using GenerateIV()
    /// 2. Create an encryptor using CreateEncryptor()
    /// 3. Convert plaintext to bytes using UTF8 encoding
    /// 4. Encrypt using TransformFinalBlock()
    /// 5. Create result array: [IV][Ciphertext]
    ///    - Use Buffer.BlockCopy to combine IV and ciphertext
    /// 6. Return the combined result
    ///
    /// Important: The IV must be prepended to the ciphertext so the
    /// receiver can extract it for decryption.
    /// </summary>
    public byte[] Encrypt(string plaintext)
    {
        throw new NotImplementedException("Implement Encrypt() - see TODO in comments above");
    }

    /// <summary>
    /// Decrypt ciphertext back to plaintext.
    ///
    /// TODO: Implement the following:
    /// 1. Create an Aes instance and configure:
    ///    - Set Key to _key
    ///    - Set Mode to CipherMode.CBC
    /// 2. Extract the IV from the first 16 bytes of ciphertext
    ///    - Create a 16-byte array for IV
    ///    - Use Buffer.BlockCopy to extract it
    ///    - Set aes.IV to the extracted IV
    /// 3. Extract the actual ciphertext (everything after IV)
    ///    - Create array of size (ciphertext.Length - 16)
    ///    - Use Buffer.BlockCopy to extract it
    /// 4. Create a decryptor using CreateDecryptor()
    /// 5. Decrypt using TransformFinalBlock()
    /// 6. Convert decrypted bytes to string using UTF8 encoding
    /// 7. Return the plaintext string
    /// </summary>
    public string Decrypt(byte[] ciphertext)
    {
        throw new NotImplementedException("Implement Decrypt() - see TODO in comments above");
    }
}

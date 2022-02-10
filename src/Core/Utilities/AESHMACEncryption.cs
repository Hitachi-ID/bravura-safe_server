using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Bit.Core.Utilities.Crypto
{
    public static class AESHMACEncryption
    {
        //Preconfigure Encryption Parameters
        public static readonly int BlockBitSize = 128;
        public static readonly int KeyBitSize = 256;

        //Preconfigured Password Key Derivation Parameters
        public static readonly int SaltBitSize = 64;
        public static readonly int Iterations = 10000;
        public static readonly int MinPasswordLength = 12;

        public static string SimpleEncrypt(string secretMessage, byte[] cryptKey, byte[] authKey, byte[] nonSecretPayload = null)
        {
            if(string.IsNullOrEmpty(secretMessage))
            {
                throw new ArgumentException("Secret Message Required!", "secretMessage");
            }

            var plainText = Encoding.UTF8.GetBytes(secretMessage);
            var cipherText = SimpleEncrypt(plainText, cryptKey, authKey, nonSecretPayload);
            return Convert.ToBase64String(cipherText);
        }

        public static byte[] SimpleEncrypt(byte[] secretMessage, byte[] cryptKey, byte[] authKey, byte[] nonSecretPayload = null)
        {
            // Error checks
            if (cryptKey == null || cryptKey.Length != KeyBitSize / 8)
                throw new ArgumentException(String.Format("Key needs to be {0} bit!", KeyBitSize), "cryptKey");

            if (authKey == null || authKey.Length != KeyBitSize / 8)
                throw new ArgumentException(String.Format("Key needs to be {0} bit!", KeyBitSize), "authKey");

            if (secretMessage == null || secretMessage.Length < 1)
                throw new ArgumentException("Secret message required!", "secretMessage");

            nonSecretPayload = nonSecretPayload ?? new byte[] { };

            byte[] cipherText;
            byte[] iv;

            using (var aes = new AesManaged
            {
                KeySize = KeyBitSize,
                BlockSize = BlockBitSize,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            {
                // Randomly generated IV
                aes.GenerateIV();
                iv = aes.IV;

                using(var encrypter = aes.CreateEncryptor(cryptKey,iv))
                using(var cipherStream = new MemoryStream()) 
                {
                    using(var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                    using(var binaryWriter = new BinaryWriter(cryptoStream))
                    {
                        // Encrypt data
                        binaryWriter.Write(secretMessage);
                    }
                    cipherText = cipherStream.ToArray();
                }
            }

            // Assemble encrypted message and add authentication
            using (var hmac = new HMACSHA256(authKey))
            using (var encryptedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(encryptedStream))
                {
                    // Prepend non-secret payload
                    binaryWriter.Write(nonSecretPayload);

                    // Prepend IV
                    binaryWriter.Write(iv);

                    // Write Ciphertext
                    binaryWriter.Write(cipherText);
                    binaryWriter.Flush();

                    // Authenticate data
                    var tag = hmac.ComputeHash(encryptedStream.ToArray());

                    // Postpend tag
                    binaryWriter.Write(tag);
                }
                return encryptedStream.ToArray();
            }
        }

        public static string SimpleDecrypt(string encryptedMessage, byte[] cryptKey, byte[] authKey, int nonSecretPayloadLength = 0)
        {
            if (string.IsNullOrWhiteSpace(encryptedMessage))
                throw new ArgumentException("Encrypted message required", "encryptedMessage");

            var cipherText = Convert.FromBase64String(encryptedMessage);
            var plainText = SimpleDecrypt(cipherText, cryptKey, authKey, nonSecretPayloadLength);
            return plainText == null ? null : Encoding.UTF8.GetString(plainText);
        }

        public static byte[] SimpleDecrypt(byte[] encryptedMessage, byte[] cryptKey, byte[] authKey, int nonSecretPayloadLength = 0)
        {

            // Error checking
            if (cryptKey == null || cryptKey.Length != KeyBitSize / 8)
                throw new ArgumentException(String.Format("CryptKey needs to be {0} bit"), "cryptKey");

            if (authKey == null || authKey.Length != KeyBitSize / 8)
                throw new ArgumentException(String.Format("AuthKey needs to be {0} bit"), "authKey");

            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted message required");

            using(var hmac = new HMACSHA256(authKey))
            {
                var sentTag = new byte[hmac.HashSize / 8];

                // Create tag
                var calcTag = hmac.ComputeHash(encryptedMessage, 0, encryptedMessage.Length - sentTag.Length);
                var ivLength = (BlockBitSize / 8);

                if (encryptedMessage.Length < sentTag.Length + nonSecretPayloadLength + ivLength)
                    return null;

                Array.Copy(encryptedMessage, encryptedMessage.Length - sentTag.Length, sentTag, 0, sentTag.Length);

                var compare = 0;
                for (var i = 0; i < sentTag.Length; i++)
                    compare |= sentTag[i] ^ calcTag[i];

                // message authentication returns null if doesn't match
                if (compare != 0)
                    return null;

                using(var aes = new AesManaged
                {
                    KeySize = KeyBitSize,
                    BlockSize = BlockBitSize,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                })
                {
                    // Start with IV from message
                    var iv = new byte[ivLength];
                    Array.Copy(encryptedMessage, nonSecretPayloadLength, iv, 0, iv.Length);

                    using (var decrypter = aes.CreateDecryptor(cryptKey, iv))
                    using (var plainTextStream = new MemoryStream())
                    {
                        using (var decrypterStream = new CryptoStream(plainTextStream, decrypter, CryptoStreamMode.Write))
                        using (var binaryWriter = new BinaryWriter(decrypterStream))
                        {
                            // Decrypt cipher text 
                            binaryWriter.Write(
                                encryptedMessage,
                                nonSecretPayloadLength + iv.Length,
                                encryptedMessage.Length - nonSecretPayloadLength - iv.Length - sentTag.Length
                                );

                        }

                        // Return results
                        return plainTextStream.ToArray();
                    }
                }
            }
        }

    }
}

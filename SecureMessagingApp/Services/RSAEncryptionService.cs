using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecureMessagingApp.Services
{
    public class RSAEncryptionService
    {
        private const string KeysFolder = "Keys";
        private const string PublicKeyFile = "public.xml";
        private const string EncryptedPrivateKeyFile = "private.enc";

        // AES parameters for encrypting the private key
        private const int AesKeySize = 256;
        private const int AesBlockSize = 128;
        private const int SaltSize = 32;
        private const int Iterations = 100_000;

        public RSAEncryptionService()
        {
            // Ensure the keys folder exists
            if (!Directory.Exists(KeysFolder))
            {
                Directory.CreateDirectory(KeysFolder);
            }
        }

        // Generates and saves a new RSA key pair if they do not already exist.
        // The public key is stored in plain text, while the private key is encrypted using the master password.
        public void EnsureKeysExist(string masterPassword)
        {
            string publicKeyPath = Path.Combine(KeysFolder, PublicKeyFile);
            string privateKeyPath = Path.Combine(KeysFolder, EncryptedPrivateKeyFile);

            // If both files exist, assume keys have already been generated.
            if (File.Exists(publicKeyPath) && File.Exists(privateKeyPath))
            {
                return;
            }

            // Create a new RSA key pair (2048 bits)
            using (RSA rsa = RSA.Create(2048))
            {
                // Export and save the public key (in XML format)
                string publicKeyXml = rsa.ToXmlString(false);
                File.WriteAllText(publicKeyPath, publicKeyXml);

                // Export the private key and encrypt it using the provided master password
                string privateKeyXml = rsa.ToXmlString(true);
                string encryptedPrivateKeyJson = EncryptPrivateKey(privateKeyXml, masterPassword);
                File.WriteAllText(privateKeyPath, encryptedPrivateKeyJson);
            }
        }
        
        // Encrypts the provided RSA private key XML string using AES.
        // The output is a JSON string that includes the salt, IV, and ciphertext.
        private string EncryptPrivateKey(string privateKeyXml, string masterPassword)
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = AesKeySize;
                aes.BlockSize = AesBlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Generate a random salt for key derivation
                byte[] salt = new byte[SaltSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                byte[] key = DeriveKey(masterPassword, salt);
                aes.Key = key;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(privateKeyXml);
                    }
                    byte[] cipherTextBytes = ms.ToArray();

                    // Create a JSON structure to hold the salt, IV, and ciphertext.
                    var data = new EncryptedPrivateKeyData
                    {
                        Salt = Convert.ToBase64String(salt),
                        IV = Convert.ToBase64String(iv),
                        CipherText = Convert.ToBase64String(cipherTextBytes)
                    };

                    return JsonSerializer.Serialize(data);
                }
            }
        }

        // Derives an AES key from the provided password and salt using PBKDF2.
        private byte[] DeriveKey(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(AesKeySize / 8);
            }
        }
        
        // Helper class for serializing the encrypted private key data.
        private class EncryptedPrivateKeyData
        {
            public string Salt { get; set; }
            public string IV { get; set; }
            public string CipherText { get; set; }
        }
        
        // Decrypts the encrypted private key JSON string using the master password
        private string DecryptPrivateKey(string encryptedDataJson, string masterPassword)
        {
            var data = JsonSerializer.Deserialize<EncryptedPrivateKeyData>(encryptedDataJson);
            if (data == null)
                throw new Exception("Invalid encrypted key data.");

            byte[] salt = Convert.FromBase64String(data.Salt);
            byte[] iv = Convert.FromBase64String(data.IV);
            byte[] cipherTextBytes = Convert.FromBase64String(data.CipherText);

            byte[] key = DeriveKey(masterPassword, salt);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = AesKeySize;
                aes.BlockSize = AesBlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(cipherTextBytes))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        // Loads and returns an RSA instance containing the decrypted private key.
        public RSA GetPrivateKey(string masterPassword)
        {
            string privateKeyPath = Path.Combine(KeysFolder, EncryptedPrivateKeyFile);
            if (!File.Exists(privateKeyPath))
                throw new FileNotFoundException("Private key file not found.");

            string encryptedDataJson = File.ReadAllText(privateKeyPath);
            string privateKeyXml = DecryptPrivateKey(encryptedDataJson, masterPassword);
            RSA rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);
            return rsa;
        }

        // Returns the public key (in XML format) stored on disk.
        public string GetPublicKey()
        {
            string publicKeyPath = Path.Combine(KeysFolder, PublicKeyFile);
            if (!File.Exists(publicKeyPath))
                throw new FileNotFoundException("Public key file not found.");

            return File.ReadAllText(publicKeyPath);
        }

        // Encrypts a message using the recipient's public key from a file.
        public byte[] EncryptMessage(string plainText, string recipientPublicKeyPath)
        {
            if (!File.Exists(recipientPublicKeyPath))
                throw new FileNotFoundException("Recipient public key file not found.");

            string recipientPublicKeyXml = File.ReadAllText(recipientPublicKeyPath);
            return EncryptMessageWithPublicKey(plainText, recipientPublicKeyXml);
        }

        // Encrypts a message using the recipient's public key provided as an XML string.
        public byte[] EncryptMessageWithPublicKey(string plainText, string recipientPublicKeyXml)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.FromXmlString(recipientPublicKeyXml);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                return rsa.Encrypt(plainBytes, RSAEncryptionPadding.OaepSHA256);
            }
        }

        // Decrypts an incoming message using the private key (secured with the master password).
        public string DecryptMessage(byte[] cipherText, string masterPassword)
        {
            using (RSA rsa = GetPrivateKey(masterPassword))
            {
                byte[] plainBytes = rsa.Decrypt(cipherText, RSAEncryptionPadding.OaepSHA256);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }
    }
}

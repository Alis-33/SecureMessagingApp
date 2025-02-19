using System;
using System.IO;

namespace SecureMessagingApp.Services
{
    public class AccountService
    {
        private const string KeysFolder = "Keys";
        private const string PublicKeyFile = "public.xml";
        private const string EncryptedPrivateKeyFile = "private.enc";

        private readonly RSAEncryptionService _encryptionService;

        public AccountService(RSAEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        public bool AccountExists()
        {
            string publicKeyPath = Path.Combine(KeysFolder, PublicKeyFile);
            string privateKeyPath = Path.Combine(KeysFolder, EncryptedPrivateKeyFile);
            return File.Exists(publicKeyPath) && File.Exists(privateKeyPath);
        }

        // Validate the provided master password by trying to load the private key.
        public bool ValidateMasterPassword(string masterPassword)
        {
            try
            {
                // Attempt to retrieve the private key. If the master password is wrong, an exception will occur.
                var rsa = _encryptionService.GetPrivateKey(masterPassword);
                return rsa != null;
            }
            catch
            {
                return false;
            }
        }

        // Create a new account by generating new RSA keys.
        public void CreateAccount(string masterPassword)
        {
            _encryptionService.EnsureKeysExist(masterPassword);
        }

        // Delete the account by removing the Keys folder.
        public void DeleteAccount()
        {
            if (Directory.Exists(KeysFolder))
            {
                Directory.Delete(KeysFolder, true);
            }
        }
    }
}
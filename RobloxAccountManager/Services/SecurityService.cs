using System;
using System.Security.Cryptography;
using System.Text;

namespace RobloxAccountManager.Services
{
    public interface ISecurityService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class SecurityService : ISecurityService
    {
        // Optional entropy to add an extra layer of protection.
        // In a real production app, this should be a managed secret or user-provided.
        // For this local utility, a static byte array unique to the app is sufficient to separate it from other DPAPI uses.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("RobloxAccountManager_Entropy_v1");

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipherBytes);
            }
            catch (Exception ex)
            {
                LogService.Error($"Encryption failed: {ex.Message}", "Security");
                return string.Empty;
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                LogService.Error($"Decryption failed: {ex.Message}", "Security");
                return string.Empty;
            }
        }
    }
}

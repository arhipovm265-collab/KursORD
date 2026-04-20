using System.Security.Cryptography;
using System.Text;

namespace WpfLibrary1
{
    public static class AuthHelper
    {
        public static byte[] ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.Unicode.GetBytes(input));
        }

        public static bool VerifyPassword(string password, byte[]? hash)
        {
            if (hash == null) return false;
            var computed = ComputeHash(password);
            if (computed.Length != hash.Length) return false;
            for (int i = 0; i < computed.Length; i++) if (computed[i] != hash[i]) return false;
            return true;
        }
    }
}
using System;
using System.Security.Cryptography;

namespace HospitalQualityDashboard.Services
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 10000;

        public static string Hash(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            var salt = new byte[SaltSize];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            var hash = Derive(password, salt);
            return string.Format("{0}:{1}:{2}", Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            var parts = storedHash.Split(':');
            if (parts.Length != 3)
            {
                return false;
            }

            int iterations;
            if (!int.TryParse(parts[0], out iterations) || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedHash = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualHash = Derive(password, salt, iterations);
            return FixedTimeEquals(actualHash, expectedHash);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations = Iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}

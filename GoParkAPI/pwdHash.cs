using System.Security.Cryptography;
using System;
using System.Text;

namespace GoParkAPI
{
    public class pwdHash
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit

        public (string Hash, string Salt) HashPassword(string password)
        {
            var salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            using (var hmac = new HMACSHA256(salt))
            {
                var key = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = new byte[SaltSize + key.Length];
                Array.Copy(salt, 0, hash, 0, SaltSize);
                Array.Copy(key, 0, hash, SaltSize, key.Length);

                return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
            }
        }

        public bool VerifyPassword(string password, string hashedPassword, string salt)
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);
            var saltBytes = Convert.FromBase64String(salt);

            if (hashBytes.Length != SaltSize + KeySize)
            {
                throw new ArgumentException("Invalid length of hashed password.");
            }

            using (var hmac = new HMACSHA256(saltBytes))
            {
                var key = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < key.Length; i++)
                {
                    if (hashBytes[i + SaltSize] != key[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}

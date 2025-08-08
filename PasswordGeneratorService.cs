using System;
using System.Linq;
using System.Security.Cryptography;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Services
{
    public class PasswordGeneratorService : IPasswordGeneratorService
    {
        private const string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowercaseLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const string SpecialCharacters = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        public string GeneratePassword(int length = 12)
        {
            if (length < 12)
                length = 12;

            var password = new char[length];
            var allCharacters = UppercaseLetters + LowercaseLetters + Digits + SpecialCharacters;

            using (var rng = RandomNumberGenerator.Create())
            {
                // En az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter olması için
                password[0] = GetRandomChar(rng, UppercaseLetters);
                password[1] = GetRandomChar(rng, LowercaseLetters);
                password[2] = GetRandomChar(rng, Digits);
                password[3] = GetRandomChar(rng, SpecialCharacters);

                // Geri kalan karakterleri rastgele doldur
                for (int i = 4; i < length; i++)
                {
                    password[i] = GetRandomChar(rng, allCharacters);
                }

                // Şifreyi karıştır
                return new string(password.OrderBy(x => Guid.NewGuid()).ToArray());
            }
        }

        private char GetRandomChar(RandomNumberGenerator rng, string characters)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomIndex = Math.Abs(BitConverter.ToInt32(bytes, 0)) % characters.Length;
            return characters[randomIndex];
        }
    }
}
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
{
    public class StudentCredential : ICosmosResource
    {
        public StudentCredential(string studentId, string passwordHash)
        {
            StudentId = studentId;
            PasswordHash = passwordHash;
        }

        [JsonIgnore]
        public string Id => $"Credential-{StudentId}";
        [JsonIgnore]
        public string PartitionKey => StudentId;

        public string StudentId { get; set; }
        public string PasswordHash { get; set; }
        public string? Iv { get; set; }

        /// <summary>
        /// Compute password hash for given username and password.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="schoolCode">The code representing the institution tenant.</param>
        /// <returns>The hashed password.</returns>
        public static string GetPasswordHash(string username, string password, string schoolCode)
        {
            MD5 md5 = MD5.Create();
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            byte[] hash = md5.ComputeHash(passwordBytes);
            StringBuilder builder = new StringBuilder(username.Length + 30 + schoolCode.Length);
            builder.Append(username);
            for (int i = 0; i < 15; i++)
            {
                builder.Append(hash[i].ToString("X2"));
            }
            builder.Append(schoolCode);
            string hashStr = builder.ToString();
            passwordBytes = Encoding.ASCII.GetBytes(hashStr);
            hash = md5.ComputeHash(passwordBytes);
            builder = new StringBuilder(30);
            for (int i = 0; i < 15; i++)
            {
                builder.Append(hash[i].ToString("X2"));
            }
            return builder.ToString();
        }
    }
}
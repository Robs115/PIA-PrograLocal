using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace piaWinUI.Services
{
    public class AuthService
    {
        private readonly string _usersFilePath;

        public AuthService(string usersFilePath)
        {
            _usersFilePath = usersFilePath;
        }

        public record User(string Username, string Password);

        public async Task<List<User>> LoadUsersAsync()
        {
            if (!File.Exists(_usersFilePath))
                return new List<User>();

            using var stream = File.OpenRead(_usersFilePath);
            return await JsonSerializer.DeserializeAsync<List<User>>(stream)
                   ?? new List<User>();
        }

        public async Task<bool> ValidateLoginAsync(string username, string password)
        {
            var users = await LoadUsersAsync();

            var user = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            return user != null && user.Password == password;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using piaWinUI.Models;

namespace piaWinUI.Services
{
    public class AuthService
    {
        private readonly string _usersFilePath;

        public AuthService(string usersFilePath)
        {
            _usersFilePath = usersFilePath;
        }

        public async Task<List<User>> LoadUsersAsync()
        {
            if (!File.Exists(_usersFilePath))
                return new List<User>();

            using var stream = File.OpenRead(_usersFilePath);
            return await JsonSerializer.DeserializeAsync<List<User>>(stream)
                   ?? new List<User>();
        }
        public enum LoginResult
        {
            Success,
            UserNotFound,
            WrongPassword
        }
        public async Task<(LoginResult Result, User? User)>ValidateLoginAsync(string username, string password)
        {
            var users = await LoadUsersAsync();

            var user = users.FirstOrDefault(u =>
                u.Username == username);

            if (user == null)
                return (LoginResult.UserNotFound, null);

            if (user.Password != password)
                return (LoginResult.WrongPassword, null);

            return (LoginResult.Success, user);
        }
    }
}

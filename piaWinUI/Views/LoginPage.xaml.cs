using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Login : Page
    {
        private readonly string _dataFolder;
        private readonly string _usersFilePath;

        public Login()
        {
            InitializeComponent();
            //_dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EvidenciaWinUI");
            //_usersFilePath = Path.Combine(_dataFolder, "users.json");
            _dataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
            _usersFilePath = Path.Combine(_dataFolder, "users.json");
            Directory.CreateDirectory(_dataFolder);
        }

        private record User(string Username, string Password);

        private async Task<List<User>> LoadUsersAsync()
        {
            try
            {
                if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);
                if (!File.Exists(_usersFilePath)) return new List<User>();

                using var stream = File.OpenRead(_usersFilePath);
                var users = await JsonSerializer.DeserializeAsync<List<User>>(stream);
                return users ?? new List<User>();
            }
            catch
            {
                return new List<User>();
            }
        }
        

        /*
        private async Task SaveUsersAsync(List<User> users)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            using var stream = File.Create(_usersFilePath);
            await JsonSerializer.SerializeAsync(stream, users, options);
        }

        */

        private void SetStatus(string text, bool isError = true)
        {
            StatusTextBlock.Text = text;
            StatusTextBlock.Foreground = isError ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }


        private async void CreateUser_Click(object sender, RoutedEventArgs e)
        {
            /*
            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password ?? string.Empty;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Ingrese un usuario y una contrasena.");
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                SetStatus("Ingrese un usuario.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                SetStatus("Ingrese una contrasena.");
                return;
            }

            var users = await LoadUsersAsync();
            if (users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)))
            {
                SetStatus("Ya existe un usuario con el nombre seleccionado, selecciona otro nombre de usuario.");
                return;
            }

            users.Add(new User(username, password));
            await SaveUsersAsync(users);
            SetStatus("Usuario creado.", isError: false);
        
        */
        }

        private async Task DoLoginAsync()
        {
            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password ?? string.Empty;

            if (string.IsNullOrEmpty(username) & string.IsNullOrEmpty(password))
            {
                SetStatus("Ingrese un usuario y una contrasena.");
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                SetStatus("Ingrese un usuario.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                SetStatus("Ingrese una contrasena.");
                return;
            }

            var users = await LoadUsersAsync();

            var user = users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                SetStatus("Usuario o contrasena invalidos.");
                return;
            }

            if (user.Password != password)
            {
                SetStatus("Usuario o contrasena invalidos.");
                return;
            }

            SetStatus("Ingreso exitoso.", isError: false);

            Frame.Navigate(typeof(MainPage));
        }


        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await DoLoginAsync();
        }

        private async void Root_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await DoLoginAsync();
            }
        }
    }
}


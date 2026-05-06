using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using piaWinUI.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    public sealed partial class UsersPage : Page
    {
        private AuthService _authService;

        public ObservableCollection<User> Users { get; set; } = new();

        private async void LoadUsers()
        {
            var users = await _authService.LoadUsersAsync();

            Users.Clear();

            foreach (var u in users)
            {
                u.isLoading = true;
                Users.Add(u);
                u.isLoading = false;
                u.IsDirty = false;
            }
        }

        private void SaveRow_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var user = (User)button.DataContext;

            user.IsDirty = false;

            SaveAllUsers();
        }

        private void SaveAllUsers()
        {
            var json = JsonSerializer.Serialize(Users, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(App.UsersFilePath, json);
        }

        public UsersPage()
        {
            InitializeComponent();

            _authService = new AuthService(App.UsersFilePath);

            LoadUsers();
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }
    }
}

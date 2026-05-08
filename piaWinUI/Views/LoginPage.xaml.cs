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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static piaWinUI.Services.AuthService;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Login : Page
    {

        private AuthService _authService;

        public Login()
        {
            InitializeComponent();
            _authService = new AuthService(App.UsersFilePath);
        }

        private void SetStatus(string text, bool isError = true)
        {
            StatusTextBlock.Text = text;
            StatusTextBlock.Foreground = isError ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        private void Usuario_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
           
            var regex = @"^[a-zA-Z0-9_-]*$";

            args.Cancel = !System.Text.RegularExpressions.Regex.IsMatch(args.NewText, regex);
        }

        private void Contrasena_BeforeTextChanging(PasswordBox sender, PasswordBoxPasswordChangingEventArgs args)
        {
          
            var password = sender.Password;

            if (password.Contains(" "))
            {
                
                sender.Password = password.Replace(" ", "");
            }
        }
        private async Task DoLoginAsync()
        {
            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) )
            {
                SetStatus("Ingrese usuario");
                return;
            }

           if (string.IsNullOrWhiteSpace(password))
                {
                SetStatus("Ingrese contraseña");
                return;
            }


            var result = await _authService.ValidateLoginAsync(username, password);

            switch (result)
            {
                case LoginResult.Success:
                    // Continuar login
                    break;
                case LoginResult.UserNotFound:
                    SetStatus("Usuario no existe.");
                    break;
                case LoginResult.WrongPassword:
                    SetStatus("Contraseña incorrecta.");
                    break;
            }

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


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    public sealed partial class UsersPage : Page, INotifyPropertyChanged
    {
        private AuthService _authService;
        public bool CanDeleteUsers => Users.Count > 1;


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

        private void UsernameBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            string text = args.NewText;

            // ❌ no espacios al inicio
            if (text.StartsWith(" "))
            {
                args.Cancel = true;
                return;
            }

            // ❌ no doble espacio
            if (text.Contains("  "))
            {
                args.Cancel = true;
                return;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pb = (PasswordBox)sender;

            if (pb.Password.Contains(" "))
            {
                pb.Password = pb.Password.Replace(" ", "");
            }
        }

        private async Task<bool> VerifyPasswordAsync(User user)
        {
            var passwordBox = new PasswordBox
            {
                PlaceholderText = "Ingrese la contraseña actual"
            };

            var dialog = new ContentDialog
            {
                Title = "Verificación de seguridad",
                Content = passwordBox,
                PrimaryButtonText = "Continuar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return false;

            return passwordBox.Password == user.Password;
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

            Users.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanDeleteUsers));
            };

            LoadUsers();

        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                var usernameBox = new TextBox
                {
                    Header = "Usuario",
                    PlaceholderText = "Ingrese el usuario",
                    MaxLength = 10
                };

                usernameBox.BeforeTextChanging += UsernameBox_BeforeTextChanging;

                var passwordBox = new PasswordBox
                {
                    Header = "Contraseña",
                    MaxLength = 15
                };


                passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

                var panel = new StackPanel
                {
                    Spacing = 12
                };

                panel.Children.Add(usernameBox);
                panel.Children.Add(passwordBox);

                var dialog = new ContentDialog
                {
                    Title = "Crear nuevo usuario",
                    Content = panel,
                    PrimaryButtonText = "Crear",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result != ContentDialogResult.Primary)
                    return;

                string password = passwordBox.Password;

                if (password.Any(char.IsWhiteSpace))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "La contraseña no puede contener espacios.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.XamlRoot
                    };

                    await errorDialog.ShowAsync();

                    continue;
                }

                string username = usernameBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(username))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Usuario inválido",
                        Content = "Debe ingresar un nombre de usuario.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.XamlRoot
                    };

                    await errorDialog.ShowAsync();

                    continue;
                }

                bool hasUppercase = password.Any(char.IsUpper);
                bool hasNumber = password.Any(char.IsDigit);
                bool validLength = password.Length >= 8;

                if (!validLength || !hasUppercase || !hasNumber)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "La contraseña debe tener mínimo 8 caracteres, una mayúscula y un número.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.XamlRoot
                    };

                    await errorDialog.ShowAsync();

                    continue;
                }

                var newUser = new User
                {
                    Username = username,
                    Password = password,
                    IsDirty = false
                };

                Users.Add(newUser);

                SaveAllUsers();

                break;
            }
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var user = (User)button.DataContext;

            // 🔐 1. pedir password actual antes de permitir edición
            var authBox = new PasswordBox
            {
                PlaceholderText = "Ingrese la contraseña actual"
            };

            var authDialog = new ContentDialog
            {
                Title = "Verificación de seguridad",
                Content = authBox,
                PrimaryButtonText = "Continuar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            var authResult = await authDialog.ShowAsync();

            if (authResult != ContentDialogResult.Primary)
                return;

            if (authBox.Password != user.Password)
            {
                await new ContentDialog
                {
                    Title = "Acceso denegado",
                    Content = "La contraseña es incorrecta.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            // ✏️ 2. abrir editor
            var usernameBox = new TextBox
            {
                Header = "Usuario",
                Text = user.Username,
                MaxLength = 10
            };

            usernameBox.BeforeTextChanging += UsernameBox_BeforeTextChanging;

            var passwordBox = new PasswordBox
            {
                Header = "Nueva contraseña (dejar vacío para no cambiar)",
                MaxLength = 15
            };

            passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(usernameBox);
            panel.Children.Add(passwordBox);

            var dialog = new ContentDialog
            {
                Title = "Editar usuario",
                Content = panel,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            string username = usernameBox.Text.Trim();
            string password = passwordBox.Password;

            // 🧾 username validation
            if (string.IsNullOrWhiteSpace(username))
            {
                await new ContentDialog
                {
                    Title = "Usuario inválido",
                    Content = "Debe ingresar un nombre de usuario.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            // 🔐 password validation solo si se cambió
            if (!string.IsNullOrEmpty(password))
            {
                if (password.Any(char.IsWhiteSpace))
                {
                    await new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "La contraseña no puede contener espacios.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    return;
                }

                bool hasUppercase = password.Any(char.IsUpper);
                bool hasNumber = password.Any(char.IsDigit);
                bool validLength = password.Length >= 8;

                if (!validLength || !hasUppercase || !hasNumber)
                {
                    await new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "La contraseña debe tener mínimo 8 caracteres, una mayúscula y un número.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    return;
                }

                user.Password = password;
            }

            // 💾 guardar cambios
            user.Username = username;
            user.IsDirty = false;

            SaveAllUsers();
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (Users.Count <= 1)
            {
                var dialog = new ContentDialog
                {
                    Title = "No se puede eliminar",
                    Content = "Debe existir al menos un usuario.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
                return;
            }

            var button = (Button)sender;
            var user = (User)button.DataContext;

            Users.Remove(user);

            SaveAllUsers();
        }

    }

    public class PasswordMaskConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "";

            return new string('•', value.ToString().Length);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

}


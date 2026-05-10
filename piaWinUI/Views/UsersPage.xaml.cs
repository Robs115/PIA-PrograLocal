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
using Windows.Devices.Enumeration;
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

        private void Contrasena_BeforeTextChanging(PasswordBox sender, PasswordBoxPasswordChangingEventArgs args)
        {

            var password = sender.Password;

            if (password.Contains(" "))
            {

                sender.Password = password.Replace(" ", "");
            }
        }

        private async Task<bool> VerifyPasswordAsync(User user)
        {
            var passwordBox = new PasswordBox
            {
                PlaceholderText = "Ingrese la contraseña actual",
                MaxLength=15
            };

            passwordBox.PasswordChanging += Contrasena_BeforeTextChanging;

            var dialog = new ContentDialog
            {
                Title = "Verificación de seguridad",
                Content = passwordBox,
                PrimaryButtonText = "Continuar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
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
            string cachedUsername = ""; 

            while (true)
            {
                var usernameBox = new TextBox
                {
                    Header = "Usuario",
                    PlaceholderText = "Ingrese el usuario",
                    MaxLength = 10,
                    Text = cachedUsername 
                };

                usernameBox.BeforeTextChanging += UsernameBox_BeforeTextChanging;

                var passwordBox = new PasswordBox
                {
                    Header = "Contraseña",
                    MaxLength = 15
                };

                passwordBox.PasswordChanging += Contrasena_BeforeTextChanging;

                var confirmPasswordBox = new PasswordBox
                {
                    Header = "Confirmar contraseña",
                    MaxLength = 15
                };

                confirmPasswordBox.PasswordChanging += Contrasena_BeforeTextChanging;

                var adminCheckBox = new CheckBox
                {
                    Content = "Administrador",
                };

                var panel = new StackPanel
                {
                    Spacing = 12
                };

                panel.Children.Add(usernameBox);
                panel.Children.Add(passwordBox);
                panel.Children.Add(confirmPasswordBox);
                panel.Children.Add(adminCheckBox);

                var dialog = new ContentDialog
                {
                    Title = "Crear nuevo usuario",
                    Content = panel,
                    PrimaryButtonText = "Crear",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary,
                };

                var result = await dialog.ShowAsync();

                if (result != ContentDialogResult.Primary)
                    return;

                string username = usernameBox.Text.Trim();
                string password = passwordBox.Password;
                string confirmPassword = confirmPasswordBox.Password;

                
                cachedUsername = username;

                // =========================
                // VALIDATIONS
                // =========================

                if (string.IsNullOrWhiteSpace(username))
                {
                    await new ContentDialog
                    {
                        Title = "Usuario inválido",
                        Content = "Debe ingresar un nombre de usuario.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                if (password != confirmPassword)
                {
                    await new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "Las contraseñas no coinciden.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                if (password.Any(char.IsWhiteSpace))
                {
                    await new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "La contraseña no puede contener espacios.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    await new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "Debes ingresar una contraseña.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                bool hasLowercase = password.Any(char.IsLower);
                bool hasUppercase = password.Any(char.IsUpper);
                bool hasNumber = password.Any(char.IsDigit);
                bool validLength = password.Length >= 8;

                if (!validLength || !hasUppercase || !hasNumber || !hasLowercase)
                {
                    await new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "Mínimo 8 caracteres, una minúscula, una mayúscula y un número.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                bool usernameExists = Users.Any(u =>
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (usernameExists)
                {
                    await new ContentDialog
                    {
                        Title = "Usuario inválido",
                        Content = "Ese nombre de usuario ya existe.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                // =========================
                // SUCCESS
                // =========================

                var newUser = new User
                {
                    Username = username,
                    Password = password,
                    IsAdmin = adminCheckBox.IsChecked == true,
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

            var users = Users.ToList();

            bool isOnlyAdmin =
            SessionService.CurrentUser?.IsAdmin == true &&
            users.Count(u => u.IsAdmin && u != user) == 0;

            bool isEditingSelf =
            SessionService.CurrentUser?.Username == user.Username;

            // 🔐 AUTH LOOP (must pass before editing)
            while (true)
            {
                var authBox = new PasswordBox
                {
                    PlaceholderText = "Contraseña",
                    MaxLength = 15
                };

                authBox.PasswordChanging += Contrasena_BeforeTextChanging;

                var panel = new StackPanel
                {
                    Spacing = 12
                };

                panel.Children.Add(new TextBlock
                {
                    Text = "Ingrese su contraseña para continuar."
                });

                panel.Children.Add(authBox);

                var authDialog = new ContentDialog
                {
                    Title = "Verificación de seguridad",
                    Content = panel,
                    PrimaryButtonText = "Continuar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary,
                };

                var authResult = await authDialog.ShowAsync();

                if (authResult != ContentDialogResult.Primary)
                    return;

                if (string.IsNullOrWhiteSpace(authBox.Password))
                {
                    await new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "Debes ingresar una contraseña.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                if (authBox.Password != SessionService.CurrentUser?.Password)
                {
                    await new ContentDialog
                    {
                        Title = "Acceso denegado",
                        Content = "La contraseña es incorrecta. Intente nuevamente.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue; // 🔁 retry auth
                }

                break; // ✅ authenticated
            }

            // =========================
            // EDIT LOOP
            // =========================
            while (true)
            {
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

                passwordBox.PasswordChanging += Contrasena_BeforeTextChanging;

                var confirmPasswordBox = new PasswordBox
                {
                    Header = "Confirmar nueva contraseña",
                    MaxLength = 15
                };

                confirmPasswordBox.PasswordChanging += Contrasena_BeforeTextChanging;

                var adminCheckBox = new CheckBox
                {
                    Content = "Administrador",
                    IsChecked = user.IsAdmin,
                    IsEnabled = !isOnlyAdmin && !isEditingSelf
                };

                var panel = new StackPanel
                {
                    Spacing = 12
                };

                panel.Children.Add(usernameBox);
                panel.Children.Add(passwordBox);
                panel.Children.Add(confirmPasswordBox);
                panel.Children.Add(adminCheckBox);

                var dialog = new ContentDialog
                {
                    Title = "Editar usuario",
                    Content = panel,
                    PrimaryButtonText = "Guardar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();

                if (result != ContentDialogResult.Primary)
                    return;

                string username = usernameBox.Text.Trim();
                string password = passwordBox.Password;

                // 🧾 USERNAME VALIDATION
                if (string.IsNullOrWhiteSpace(username))
                {
                    await new ContentDialog
                    {
                        Title = "Usuario inválido",
                        Content = "Debe ingresar un nombre de usuario.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                // 🔐 PASSWORD VALIDATION (only if changed)
                if (!string.IsNullOrEmpty(password))
                {
                    string confirmPassword = confirmPasswordBox.Password;

                    if (password != confirmPassword)
                    {
                        await new ContentDialog
                        {
                            Title = "Contraseña inválida",
                            Content = "Las contraseñas no coinciden.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        }.ShowAsync();

                        continue;
                    }

                    if (password.Any(char.IsWhiteSpace))
                    {
                        await new ContentDialog
                        {
                            Title = "Contraseña inválida",
                            Content = "No puede contener espacios.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        }.ShowAsync();

                        continue;
                    }

                    bool hasLowercase = password.Any(char.IsLower);
                    bool hasUppercase = password.Any(char.IsUpper);
                    bool hasNumber = password.Any(char.IsDigit);
                    bool validLength = password.Length >= 8;


                    if (!validLength || !hasUppercase || !hasNumber || !hasLowercase)
                    {
                        await new ContentDialog
                        {
                            Title = "Contraseña inválida",
                            Content = "Contraseña invalida, se requieren minimo 8 caracteres, una minuscula, una mayúscula y un número.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        }.ShowAsync();

                        continue;
                    }

                    user.Password = password;
                }

                bool usernameExists = Users.Any(u =>
                u != user &&
                u.Username.Equals(username));

                if (usernameExists)
                {
                    await new ContentDialog
                    {
                        Title = "Usuario inválido",
                        Content = "Ese nombre de usuario ya existe.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                // 💾 SAVE
                user.Username = username;
                user.IsDirty = false;
                user.IsAdmin = adminCheckBox.IsChecked == true;
                SaveAllUsers();

                break; // ✅ success exit
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (Users.Count <= 1)
            {
                await new ContentDialog
                {
                    Title = "No se puede eliminar",
                    Content = "Debe existir al menos un usuario.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            var button = (Button)sender;
            var user = (User)button.DataContext;

            if (SessionService.CurrentUser?.Username == user.Username)
            {
                await new ContentDialog
                {
                    Title = "Acción no permitida",
                    Content = "No puedes eliminar tu propio usuario.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            // =========================
            // 🔐 AUTH LOOP (retry until correct or cancel)
            // =========================
            while (true)
            {
                var authBox = new PasswordBox
                {
                    PlaceholderText = "Contraseña",
                    MaxLength = 15
                };

                authBox.PasswordChanging += Contrasena_BeforeTextChanging;

                var panel = new StackPanel
                {
                    Spacing = 12
                };

                panel.Children.Add(new TextBlock
                {
                    Text = "Ingrese su contraseña para confirmar la eliminación."
                });

                panel.Children.Add(authBox);

                var authDialog = new ContentDialog
                {
                    Title = "Verificacion de seguridad",
                    Content = panel,
                    PrimaryButtonText = "Continuar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var authResult = await authDialog.ShowAsync();

                // ❌ user cancelled
                if (authResult != ContentDialogResult.Primary)
                    return;

                if (string.IsNullOrWhiteSpace(authBox.Password))
                {
                    await new ContentDialog
                    {
                        Title = "Contraseña inválida",
                        Content = "Debes ingresar una contraseña.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                // ❌ wrong password → retry
                if (authBox.Password != SessionService.CurrentUser?.Password)
                {
                    await new ContentDialog
                    {
                        Title = "Acceso denegado",
                        Content = "La contraseña es incorrecta. Intente nuevamente.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();

                    continue;
                }

                break; // ✅ authenticated
            }

            // =========================
            // ⚠ CONFIRMATION LOOP (optional retry safety)
            // =========================
            while (true)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirmar eliminación",
                    Content = $"¿Desea eliminar el usuario \"{user.Username}\"?",
                    PrimaryButtonText = "Eliminar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var confirmResult = await confirmDialog.ShowAsync();

                // ❌ cancelled → exit
                if (confirmResult != ContentDialogResult.Primary)
                    return;

                // 🗑 delete
                Users.Remove(user);
                SaveAllUsers();

                break;
            }
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

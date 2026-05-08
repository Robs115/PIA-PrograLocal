using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using static SkiaSharp.HarfBuzz.SKShaper;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public readonly string _dataFolder;
        public readonly string _usersFilePath;

        public MainPage()
        {
            InitializeComponent();
            ContentFrame.Navigate(typeof(InicioPag));
            _dataFolder = App.DataFolder;
            _usersFilePath = App.UsersFilePath;

        }
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItemContainer as NavigationViewItem;
            if (item == null) return;

            switch (item.Tag?.ToString())
            {
                case "Inicio":
                    ContentFrame.Navigate(typeof(InicioPag));
                    break;
                case "Reportes":
                    ContentFrame.Navigate(typeof(Reportes));
                    break;

                case "Productos":
                    ContentFrame.Navigate(typeof(ProductosPag));
                    break;

                case "Ventas":
                    ContentFrame.Navigate(typeof(VentasPag));
                    break;

                case "Clientes":
                    ContentFrame.Navigate(typeof(ClientesPag));
                    break;

                case "Pedidos":
                    ContentFrame.Navigate(typeof(PedidosPag));
                    break;

                case "Proveedores":
                    ContentFrame.Navigate(typeof(ProveedoresPag));
                    break;
            }
        }

        private ContentDialog currentDialog;

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = null;
            NavView.Focus(FocusState.Programmatic);
            bool ok = await RequireAdminLoginAsync();
            NavView.SelectedItem = null;
            NavView.IsPaneToggleButtonVisible = true; // optional no-op safety

            ContentFrame.Focus(FocusState.Programmatic);

            if (!ok)
                return;

            var stack = new StackPanel
            {
                Spacing = 12,
                Width = 280,
                Padding = new Thickness(10)
            };

            var title = new TextBlock
            {
                Text = "Opciones",
                FontSize = 26,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };

            var btnUsuarios = new Button
            {
                Content = "Usuarios",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 16,
                Padding = new Thickness(12, 10, 12, 10)
            };

            btnUsuarios.Click += (s, args) =>
            {
                ContentFrame.Navigate(typeof(UsersPage));
                currentDialog?.Hide();
            };

            var btnAbrirCarpeta = new Button
            {
                Content = "Abrir carpeta de datos",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 16,
                Padding = new Thickness(12, 10, 12, 10)
            };

            btnAbrirCarpeta.Click += (s, args) =>
            {
                string localFolder = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path,"Local","piaWinUI");

                Process.Start(new ProcessStartInfo
                {
                    FileName = localFolder,
                    UseShellExecute = true
                });
            };

            var btnCerrar = new Button
            {
                Content = "Regresar",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 16,
                Padding = new Thickness(12, 10, 12, 10)
            };

            btnCerrar.Click += (s, args) =>
            {
                // cerrar dialog
                currentDialog?.Hide();
            };

            stack.Children.Add(title);
            stack.Children.Add(btnUsuarios);
            stack.Children.Add(btnAbrirCarpeta);
            stack.Children.Add(btnCerrar);

            var dialog = new ContentDialog
            {
                Content = stack,
                XamlRoot = this.XamlRoot,
                CloseButtonText = null,
                MinWidth = 320
            };

            currentDialog = dialog;
            await dialog.ShowAsync();
        }

        private void ExitApp_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async Task<bool> RequireAdminLoginAsync()
        {
            while (true)
            {
                var usernameBox = new TextBox
                {
                    Header = "Usuario",
                    MaxLength = 10
                };

                usernameBox.BeforeTextChanging += Username_Login_BeforeTextChanging;

                var passwordBox = new PasswordBox
                {
                    Header = "Contraseña",
                    MaxLength = 15
                };

                passwordBox.PasswordChanging += Contrasena_BeforeTextChanging;

                var panel = new StackPanel
                {
                    Spacing = 12
                };

                panel.Children.Add(usernameBox);
                panel.Children.Add(passwordBox);

                var dialog = new ContentDialog
                {
                    Title = "Inicio de sesión requerido",
                    Content = panel,
                    PrimaryButtonText = "Entrar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                NavView.SelectedItem = null;
                NavView.Focus(FocusState.Programmatic);

                var result = await dialog.ShowAsync();

                NavView.SelectedItem = null;
                NavView.IsPaneToggleButtonVisible = true; // optional no-op safety

                ContentFrame.Focus(FocusState.Programmatic);

                // ❌ cancelado → salir completamente
                if (result != ContentDialogResult.Primary)
                    return false;

                var users = await new piaWinUI.Services.AuthService(_usersFilePath)
                    .LoadUsersAsync();

                var user = users.FirstOrDefault(u =>
                    u.Username == usernameBox.Text &&
                    u.Password == passwordBox.Password);

                // ✅ login correcto
                if (user != null)
                return true;

                NavView.SelectedItem = null;
                NavView.Focus(FocusState.Programmatic);

                // 🔴 login incorrecto → mostrar error y repetir loop
                await new ContentDialog
                {
                    Title = "Error de autenticación",
                    Content = "Usuario o contraseña incorrectos. Intente nuevamente.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                NavView.SelectedItem = null;
                NavView.IsPaneToggleButtonVisible = true;


                // el while(true) hace que se vuelva a mostrar el login
            }
        }

        private void Username_Login_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            var text = args.NewText;

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


    }
}



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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using piaWinUI.Views;
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
            ContentFrame.Navigate(typeof(Reportes));
            _dataFolder = App.DataFolder;
            _usersFilePath = App.UsersFilePath;

        }
        private void NavView_ItemInvoked(NavigationView sender,
            NavigationViewItemInvokedEventArgs args)
        {
            switch (args.InvokedItem?.ToString())
            {
                case "Reportes":
                    ContentFrame.Navigate(typeof(Reportes));
                    break;

                case "Productos":
                    ContentFrame.Navigate(typeof(ProductosPag));
                    break;

                case "Ventas":
                    ContentFrame.Navigate(typeof(VentasPag));
                    break;

                case "Caja":
                    ContentFrame.Navigate(typeof(CajaPag));
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
            stack.Children.Add(btnCerrar);

            var dialog = new ContentDialog
            {
                Content = stack,
                XamlRoot = this.XamlRoot,
                CloseButtonText = null, // importante: quitamos el botón feo default
                MinWidth = 320
            };

            currentDialog = dialog;
            await dialog.ShowAsync();
        }

        private void ExitApp_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Application.Current.Exit();
        }

    }
}



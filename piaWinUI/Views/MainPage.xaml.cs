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

        private void ExitApp_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Application.Current.Exit();
        }

    }
}



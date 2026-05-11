using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using piaWinUI.Helpers;
using piaWinUI.Models;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        public static Window MainWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            Directory.CreateDirectory(FilePaths.DataFolder);

            QuestPDF.Settings.License = LicenseType.Community;


            if (!File.Exists(FilePaths.Users))
            {
                var defaultUsers = new[]
                {
                    new User { Username = "admin", Password = "1234", IsAdmin = true }
                };

                File.WriteAllText(
                    UsersFilePath,
                    JsonSerializer.Serialize(defaultUsers, new JsonSerializerOptions { })
                );
            }

        }
        public static string CajaFilePath => System.IO.Path.Combine(DataFolder, "caja.json");
        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();

            _window = MainWindow;

            var hwnd = WindowNative.GetWindowHandle(_window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

            _window.Activate();
        }
    

    
        public static string DataFolder { get; } =
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "piaWinUI");

        public static string UsersFilePath => FilePaths.Users;

        public static string ProductsFilePath { get; } =
            System.IO.Path.Combine(DataFolder, "products.json");
        public static string ClientesFilePath { get; } =
            System.IO.Path.Combine(DataFolder, "clientes.json");
        public static string VentasFilePath { get; } =
            System.IO.Path.Combine(DataFolder, "ventas.json");
        public static string ProveedorFilePath { get; } =
            System.IO.Path.Combine(DataFolder, "proveedores.json");
        public static string PedidosFilePath { get; } =
            System.IO.Path.Combine(DataFolder, "pedidos.json");

        public static string DetalleVentasFilePath { get; } =
            System.IO.Path.Combine(DataFolder, "detalleventas.json");
    }
}

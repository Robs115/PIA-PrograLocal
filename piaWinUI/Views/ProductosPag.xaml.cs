using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed partial class ProductosPag : Page
    {
        public ObservableCollection<Producto> Productos { get; set; }
            = new ObservableCollection<Producto>();

        public Producto ProductoSeleccionado { get; set; }

        public readonly string _dataFolder = App.DataFolder;
        public readonly string _productsFilePath = App.ProductsFilePath;

        private async Task<List<Producto>> LoadProductsAsync()
        {
            try
            {
                if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);
                if (!File.Exists(_productsFilePath)) return new List<Producto>();

                using var stream = File.OpenRead(_productsFilePath);
                var products = await JsonSerializer.DeserializeAsync<List<Producto>>(stream);
                return products ?? new List<Producto>();
            }
            catch
            {
                return new List<Producto>();
            }
        }
        public async Task LoadProductsData()
        {
            var productos = await LoadProductsAsync();

            Productos.Clear();

            foreach (var p in productos)
                Productos.Add(p);
        }

        public ProductosPag()
        {
            this.InitializeComponent();

            this.DataContext = this;

        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await LoadProductsData();
        }
    }
}
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

        private readonly ProductService _service = new ProductService();

        public ProductosPag()
        {
            this.InitializeComponent();

            this.DataContext = this;

        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await LoadProductsData();
        }

        private async Task LoadProductsData()
        {
            var productos = await _service.GetProductsAsync();

            Productos.Clear();

            foreach (var p in productos)
                Productos.Add(p);
        }

        private void CrearProducto_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(CreateProductPage));
        }
    }
}
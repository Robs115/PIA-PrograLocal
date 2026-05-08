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
using Windows.ApplicationModel.DataTransfer;

namespace piaWinUI.Views
{
    public class ProductoView
    {
        public Producto Model { get; }

        public ProductoView(Producto model)
        {
            Model = model;
        }

        public string Nombre => Model.Nombre;
        public string Descripcion => Model.Descripcion;
        public decimal PrecioCompra => Model.PrecioCompra;
        public decimal PrecioVenta => Model.PrecioVenta;
        public Guid IdProveedor => Model.IdProveedor;
        public string Categoria => Model.Categoria;
        public int Stock => Model.Stock;

        public string ProveedorNombre { get; set; }
    }

    public sealed partial class ListarProductosPage : Page
    {

        public ObservableCollection<ProductoView> Productos { get; set; }
            = new ObservableCollection<ProductoView>();

        public ProductoView ProductoSeleccionado { get; set; }

        private readonly ProductService _service = new ProductService();

        private Dictionary<Guid, string> _proveedorLookup = new();

        public ListarProductosPage()
        {
            InitializeComponent();
            this.DataContext = this;
        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await LoadProductsData();
        }

        private async Task LoadProductsData()
        {
            var productos = await _service.GetAllAsync();
            var proveedores = await new ProveedorService().GetAllAsync();
            _proveedorLookup = proveedores.ToDictionary(p => p.IdProveedor, p => p.Nombre);

            Productos.Clear();

            foreach (var p in productos)
            {
                Productos.Add(new ProductoView(p)
                {
                    ProveedorNombre = _proveedorLookup.TryGetValue(p.IdProveedor, out var nombre)
                        ? nombre
                        : "Desconocido"
                });
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is ProductoView productoView)
            {
                Frame.Navigate(typeof(EditProductoPage), productoView.Model);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is ProductoView productoView)
            {
                await _service.DeleteProductoAsync(productoView.Model.Id);

                Productos.Remove(productoView);
            }
        }

        private void Nombre_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is TextBlock tb &&
                tb.DataContext is ProductoView productoView)
            {
                var guid = productoView.Model.Id.ToString();

                var dataPackage = new DataPackage();
                dataPackage.SetText(guid);

                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace piaWinUI.Views
{
    public sealed partial class PedidosPag : Page
    {
        private readonly ProveedorService _proveedorService = new ProveedorService();
        private readonly ProductService _productService = new ProductService();
        private readonly PedidoService _pedidoService = new PedidoService();

        private List<Proveedor> _proveedores = new();
        private List<Producto> _productos = new();
        private List<PedidoView> _pedidos = new();



        public PedidosPag()
        {
            this.InitializeComponent();

            Loaded += async (_, __) =>
            {
                await Inicializar();
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await CargarPedidos();
        }

        private async void RegistrarPedido_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbProductoPedido.SelectedItem == null)
                    return;

                var producto = cmbProductoPedido.SelectedItem as Producto;
                var proveedor = cmbProveedorPedido.SelectedItem as Proveedor;

                var pedidos = await _pedidoService.GetPedidosAsync();

                var nuevo = new Pedidos
                {
                    Id = Guid.NewGuid(),

                    IdProducto = producto.Id,
                    IdProveedor = proveedor.IdProveedor,

                    NombreProducto = producto.Nombre,
                    NombreProveedor = proveedor.Nombre,

                    Cantidad = int.Parse(txtCantidadPedido.Text),
                    Fecha = DateTime.Now
                };

                pedidos.Add(nuevo);

                await _pedidoService.SavePedidosAsync(pedidos);

                await CargarPedidos();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private async Task CargarPedidos()
        {
            var pedidos = await _pedidoService.GetPedidosAsync();
            gridPedidos.ItemsSource = pedidos;
        }

        private async Task Inicializar()
        {
            await CargarProveedores();
            await CargarProductos();

            gridPedidos.ItemsSource = _pedidos;
        }

        // 🔥 CARGA PROVEEDORES (TU SERVICE)
        private async Task CargarProveedores()
        {
            try
            {
                _proveedores = await _proveedorService.GetProveedorAsync();

                System.Diagnostics.Debug.WriteLine($"Proveedores: {_proveedores.Count}");

                if (_proveedores == null || !_proveedores.Any())
                {
                    cmbProveedorPedido.ItemsSource = null;
                    return;
                }

                cmbProveedorPedido.ItemsSource = _proveedores;
                cmbProveedorPedido.DisplayMemberPath = "Nombre";
            }
            catch
            {
                cmbProveedorPedido.ItemsSource = null;
            }
        }

        // 🔥 CARGA PRODUCTOS
        private async Task CargarProductos()
        {
            try
            {
                _productos = await _productService.GetProductsAsync();

                if (_productos == null || !_productos.Any())
                {
                    cmbProductoPedido.ItemsSource = null;
                    return;
                }

                cmbProductoPedido.ItemsSource = _productos;
                cmbProductoPedido.DisplayMemberPath = "Nombre";
            }
            catch
            {
                cmbProductoPedido.ItemsSource = null;
            }
        }

        // 🔥 AUTOSELECCIÓN DE PROVEEDOR
        private void cmbProductoPedido_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var producto = cmbProductoPedido.SelectedItem as Producto;

            if (producto == null) return;

            // 🔥 buscar proveedor automáticamente
            var proveedor = _proveedores
                .FirstOrDefault(p => p.IdProveedor == producto.IdProveedor);

            if (proveedor != null)
            {
                cmbProveedorPedido.SelectedItem = proveedor;
            }
        }


        // 🔍 BUSCAR
        private void BuscarPedido_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filtro = txtBuscarPedido.Text?.ToLower() ?? "";

            var filtrados = _pedidos.Where(p =>
                p.NombreProducto.ToLower().Contains(filtro) ||
                p.NombreProveedor.ToLower().Contains(filtro)
            ).ToList();

            gridPedidos.ItemsSource = filtrados;
        }

        // 🔔 DIALOGO
        private async Task MostrarDialogo(string titulo, string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    // 🔥 MODELO PARA EL GRID
    public class PedidoView
    {
        public DateTime Fecha { get; set; }
        public string NombreProducto { get; set; }
        public string NombreProveedor { get; set; }
        public int Cantidad { get; set; }
    }
}
using piaWinUI.Models;
using piaWinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Collections.ObjectModel;



namespace piaWinUI.Views
{
        public sealed partial class VentasPag : Page
        {
            private ProductService _productoService = new ProductService();
            private VentasService _ventaService = new VentasService();
             private List<Producto> todosProductos = new List<Producto>();
            private ObservableCollection<DetalleVentas> carrito = new ObservableCollection<DetalleVentas>();
           

        public VentasPag()
            {
                this.InitializeComponent();
                 ProductosList.ItemsSource = carrito;
        }

        private void AgregarProductoAlCarrito(Producto producto)
        {
            var existente = carrito.FirstOrDefault(p => p.IdProducto == producto.Id);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                var nuevo = new DetalleVentas
                {
                    IdProducto = producto.Id,
                    NombreProducto = producto.Nombre,
                    PrecioUnitario = producto.PrecioVenta,
                    Cantidad = 1,
                    StockDisponible = producto.Stock
                };

                nuevo.OnError += async (mensaje) =>
                {
                    await ShowDialogAsync("Error", mensaje);
                };

                nuevo.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(DetalleVentas.Cantidad) ||
                        e.PropertyName == nameof(DetalleVentas.PrecioUnitario))
                    {
                        ActualizarTotal();
                    }
                };

                carrito.Add(nuevo);

                // 🔥 forzar cálculo inicial
                nuevo.Cantidad = nuevo.Cantidad;
            }
            
            ActualizarTotal();
        }

        private async Task ShowDialogAsync(string title, string content)
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();

            }

            // 🔥 TOTAL
            private void ActualizarTotal()
            {
                var total = carrito.Sum(p => p.Subtotal);
                TotalText.Text = $"Total: {total:C}";
            }
            private void limpiarbarrabusqueda() {
            CodigoBox.Text = "";
        }
        // 🔥 AGREGAR PRODUCTO
        private async void CargarProductos()
        {
            var productos = await _productoService.GetAllAsync() ?? new List<Producto>();

            string codigo = CodigoBox.Text;

            if (string.IsNullOrWhiteSpace(codigo))
            {
                await ShowDialogAsync("Error", "No hay código.");
                return;
            }

            if (!Guid.TryParse(codigo, out Guid guidBuscado))
            {
                await ShowDialogAsync("Error", "Código inválido");
                limpiarbarrabusqueda();
                return;
            }

            var producto = productos.FirstOrDefault(p => p.Id == guidBuscado);

            if (producto == null)
            {
                await ShowDialogAsync("Error", "Producto no encontrado");
                limpiarbarrabusqueda();
                return;
            }

            if (producto.Stock <= 0)
            {
                await ShowDialogAsync("Error", "No hay stock de este producto");
                limpiarbarrabusqueda();
                return;
            }

            // 🔥 SOLO ESTO
            AgregarProductoAlCarrito(producto);
           
            limpiarbarrabusqueda();
        }
        private async void EliminarProducto_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as DetalleVentas;

            if (item == null)
                return;

            var confirm = new ContentDialog
            {
                Title = "Eliminar",
                Content = "¿Eliminar producto?",
                PrimaryButtonText = "Sí",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                carrito.Remove(item);
                ActualizarTotal();
            }
        }

        private void obtenerproducto_keydown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    CargarProductos();
                }
            }

            // 🔥 GUARDAR VENTA
            private async void GuardarVenta_Click(object sender, RoutedEventArgs e)
            {
                if (carrito.Count == 0)
                {
                    await ShowDialogAsync("Error", "No hay productos en la venta");
                    return;
                }

            // Validación final
            if (carrito.Any(p => p.TieneError))
            {
                await ShowDialogAsync("Error", "Hay productos con cantidades inválidas");
                return;
            }

            var venta = new Venta
                {
                    IdVenta = Guid.NewGuid(),
                    IdUsuario = Guid.NewGuid(),
                    IdCliente = Guid.NewGuid(),
                    Fecha = DateTime.Now,
                    Total = carrito.Sum(p => p.Subtotal)
                };

                foreach (var item in carrito)
                {
                    item.IdVenta = venta.IdVenta;
                }

                var ventas = await _ventaService.GetAllAsync() ?? new List<Venta>();
                ventas.Add(venta);
                await _ventaService.SaveAllAsync(ventas);
                

            var detalleService = new DetalleVentasService();
                var detalles = await detalleService.GetAllAsync() ?? new List<DetalleVentas>();
                detalles.AddRange(carrito);
                await detalleService.SaveAllAsync(detalles);
            var productos = await _productoService.GetAllAsync() ?? new List<Producto>();
            foreach (var item in carrito)
            {
                var producto = productos.FirstOrDefault(p => p.Id == item.IdProducto);

                if (producto != null)
                {
                    producto.Stock -= item.Cantidad;

                    if (producto.Stock < 0)
                        producto.Stock = 0;
                }
            }

            await _productoService.SaveAllAsync(productos);
            carrito.Clear();
            CodigoBox.Text = "";

                ActualizarTotal();

                await ShowDialogAsync("Éxito", "Venta registrada correctamente");
            }

        private void BuscarProductoBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = BuscarProductoBox.Text.ToLower();

            var filtrados = todosProductos
                .Where(p => p.Nombre.ToLower().Contains(texto))
                .ToList();

            ResultadosProductosList.ItemsSource = filtrados;
        }
        private async void BuscarProducto_Click(object sender, RoutedEventArgs e)
        {
            todosProductos = await _productoService.GetAllAsync() ?? new List<Producto>();

            ResultadosProductosList.ItemsSource = todosProductos;

            var result = await BuscarProductoDialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            var seleccionado = ResultadosProductosList.SelectedItem as Producto;

            if (seleccionado == null)
                return;
            if (seleccionado.Stock <= 0)
            {
                await ShowDialogAsync("Error", "No hay stock de este producto");
                return;
            }   

            AgregarProductoAlCarrito(seleccionado);
        }

        private async void CargarVentas()
        {
            //not yet
        }
    }
}
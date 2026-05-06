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
            private VentaService _ventaService = new VentaService();
            
            private ObservableCollection<DetalleVentas> carrito = new ObservableCollection<DetalleVentas>();
            bool    bandera = false;

        public VentasPag()
            {
                this.InitializeComponent();
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

            // 🔥 AGREGAR PRODUCTO
            private async void CargarProductos()
            {
                var productos = await _productoService.GetProductsAsync() ?? new List<Producto>();

                string codigo = CodigoBox.Text;

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    await ShowDialogAsync("Error", "No hay código.");

                    return;
                }

                if (!Guid.TryParse(codigo, out Guid guidBuscado))
                {
                    await ShowDialogAsync("Error", "Código inválido");
                CodigoBox.Text = "";
                    return;
                }

                var producto = productos.FirstOrDefault(p => p.Id == guidBuscado);

                if (producto == null)
                {
                    await ShowDialogAsync("Error", "Producto no encontrado");
                    return;
                }

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

                    // 🔥 error desde modelo
                    nuevo.OnError += async (mensaje) =>
                    {
                        await ShowDialogAsync("Error", mensaje);
                        bandera = true;
                    };

                    // 🔥 actualizar total cuando cambie cantidad
                    nuevo.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(DetalleVentas.Cantidad))
                        {
                            ActualizarTotal();
                        }
                    };

                    carrito.Add(nuevo);
                    nuevo.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(DetalleVentas.Cantidad) ||
                            e.PropertyName == nameof(DetalleVentas.PrecioUnitario))
                        {
                            ActualizarTotal();
                        }
                    };
                // 🔥 FORZAR PRIMER CALCULO
                nuevo.Cantidad = nuevo.Cantidad;
            }

              
                ProductosList.ItemsSource = carrito;

                CodigoBox.Text = "";

               

            
            
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
                if (carrito.Any(p => p.Cantidad > p.StockDisponible) || bandera == true)
                {
                    await ShowDialogAsync("Error", "Hay cantidades inválidas");
                bandera = false;
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

                var ventas = await _ventaService.GetVentasAsync() ?? new List<Venta>();
                ventas.Add(venta);
                await _ventaService.SaveVentasAsync(ventas);

                var detalleService = new DetalleVentasService();
                var detalles = await detalleService.GetDetalleVentasAsync() ?? new List<DetalleVentas>();
                detalles.AddRange(carrito);
                await detalleService.SaveDetalleVentasAsync(detalles);
            var productos = await _productoService.GetProductsAsync() ?? new List<Producto>();

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

            await _productoService.SaveProductsAsync(productos);
            carrito.Clear();
            CodigoBox.Text = "";

                ActualizarTotal();

                await ShowDialogAsync("Éxito", "Venta registrada correctamente");
            }
       
        private async void BuscarProducto_Click(object sender, RoutedEventArgs e)
        {
            //not yet
        }

        private async void CargarVentas()
        {
            //not yet
        }
    }
}
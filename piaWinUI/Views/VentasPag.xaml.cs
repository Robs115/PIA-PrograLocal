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

namespace piaWinUI.Views
{
    public sealed partial class VentasPag : Page
    {
        private ProductService _productoService = new ProductService();
        private VentaService _ventaService = new VentaService();
        private List<DetalleVentas> carrito = new List<DetalleVentas>();
        private List<Venta> listaVentas = new List<Venta>();

        public VentasPag()
        {
            this.InitializeComponent();
        }

        private async Task ShowDialogAsync(string title, string content, string closeText = "Cerrar")
        {
            var xamlRoot = this.Content?.XamlRoot;
            if (xamlRoot == null)
                return;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = closeText,
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void CargarProductos()
        {
            var productos = await _productoService.GetProductsAsync() ?? new List<Producto>();

            string codigo = CodigoBox?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(codigo))
            {
                await ShowDialogAsync("Error", "No hay codigo.");
                return;
            }
            if (!Guid.TryParse(codigo, out Guid guidBuscado))
            {
                await ShowDialogAsync("Error", "Codigo inválido");
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
                


                carrito.Add(new DetalleVentas
                {
                    IdProducto = producto.Id,
                    NombreProducto = producto.Nombre,
                    Cantidad = 1,
                    PrecioUnitario = producto.PrecioVenta
                });
            }


            


            ProductosList.ItemsSource = null;
            ProductosList.ItemsSource = carrito;
            var cantidadActual = carrito
    .Where(p => p.IdProducto == producto.Id)
    .Sum(p => p.Cantidad);

            if (cantidadActual > producto.Stock)
            {
                await ShowDialogAsync("Error", "Stock insuficiente");
                return;
            }
            CodigoBox?.SetValue(Microsoft.UI.Xaml.Controls.TextBox.TextProperty, string.Empty);
        }

        private void obtenerproducto_keydown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                CargarProductos();
            }
        }

        private async void GuardarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                await ShowDialogAsync("Error", "No hay productos en la venta");
                return;
            }

            // 🔹 Crear venta
            var venta = new Venta
            {
                IdVenta = Guid.NewGuid(),
                IdUsuario = Guid.NewGuid(), // luego pones el usuario real
                IdCliente = Guid.NewGuid(), // luego cliente real
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

            // 🔹 Limpiar UI
            carrito.Clear();
            ProductosList.ItemsSource = null;

            await ShowDialogAsync("Éxito", "Venta registrada correctamente", "OK");
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
using piaWinUI.Models;
using piaWinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        

        private async void CargarProductos()
        {
            var productos = await _productoService.GetProductsAsync();

            string codigo = CodigoBox.Text;

            if (string.IsNullOrWhiteSpace(codigo))
                return;

            if (!Guid.TryParse(codigo, out Guid guidBuscado))
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = "Código inválido",
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.Content.XamlRoot
                }.ShowAsync();
                return;
            }

            var producto = productos.FirstOrDefault(p => p.Id == guidBuscado);

            if (producto == null)
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = "Producto no encontrado",
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.Content.XamlRoot
                }.ShowAsync();
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
                    PrecioUnitario = producto?.PrecioVenta ?? 0
                });
            }

           
            
            ProductosList.ItemsSource = carrito;

            CodigoBox.Text = "";
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
                await new ContentDialog
                {
                    Title = "Error",
                    Content = "No hay productos en la venta",
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.Content.XamlRoot
                }.ShowAsync();
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

            // 🔹 Asignar IdVenta a cada detalle
            foreach (var item in carrito)
            {
                item.IdVenta = venta.IdVenta;
            }

            // 🔹 Guardar venta
            var ventas = await _ventaService.GetVentasAsync();
            ventas.Add(venta);
            await _ventaService.SaveVentasAsync(ventas);

            // 🔹 Guardar detalles (necesitas este service 👇)
            var detalleService = new DetalleVentasService();
            var detalles = await detalleService.GetDetalleVentasAsync();
            detalles.AddRange(carrito);
            await detalleService.SaveDetalleVentasAsync(detalles);

            // 🔹 Limpiar UI
            carrito.Clear();
            ProductosList.ItemsSource = null;

            await new ContentDialog
            {
                Title = "Éxito",
                Content = "Venta registrada correctamente",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            }.ShowAsync();
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
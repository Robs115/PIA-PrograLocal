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
        private List<Venta> listaVentas = new List<Venta>();
        private List<ProductoVenta> carrito = new List<ProductoVenta>();
        public VentasPag()
        {
            
            this.InitializeComponent();
            
        }

        public class ProductoVenta
        {
            public string Nombre { get; set; }
            public int Cantidad { get; set; }
            public decimal Precio { get; set; }
            public decimal Subtotal => Cantidad * Precio;
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

            
            var existente = carrito.FirstOrDefault(p => p.Nombre == producto.Nombre);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                carrito.Add(new ProductoVenta
                {
                    Nombre = producto.Nombre,
                    Cantidad = 1,
                    Precio = producto.PrecioVenta
                });
            }

           
            ProductosList.ItemsSource = null;
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
           //not yet
        }

        private async void BuscarProducto_Click(object sender, RoutedEventArgs e)
        {
            //not yet
        }

        private async void CargarVentas()
        {
            //not yet
        }

        private void LimpiarFormulario()
        {
            
        }
    }
}
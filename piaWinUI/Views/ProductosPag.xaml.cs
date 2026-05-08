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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    public sealed partial class ProductosPag : Page
    {

        public ProductosPag()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            Submit.IsEnabled = false;
            Loaded += async (_, __) => await CargarProveedores();

        }

        private readonly ProductService _service = new ProductService();
        private readonly ProveedorService _proveedorService = new ProveedorService();

        private async Task CargarProveedores()
        {
            var proveedores = await _proveedorService.GetAllAsync();

            cmbProveedor.ItemsSource = proveedores;
            Submit.IsEnabled = true;
        }

        private async void Validator(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                SetStatus("El nombre es obligatorio.");
                return;
            }

            if (txtNombre.Text.Length > 50)
            {
                SetStatus("Máximo 50 caracteres en nombre.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                SetStatus("La descripción es obligatoria.");
                return;
            }

            if (cmbCategoria.SelectedItem is null)
            {
                SetStatus("Selecciona una categoría.");
                return;
            }

            if (!decimal.TryParse(txtPrecioCompra.Text, out decimal precioCompra))
            {
                SetStatus("Precio de compra inválido.");
                return;
            }

            if (!decimal.TryParse(txtPrecioVenta.Text, out decimal precioVenta))
            {
                SetStatus("Precio de venta inválido.");
                return;
            }

            if (precioCompra >= precioVenta)
            {
                SetStatus("El precio de venta no puede ser menor o igual al precio de compra.");
                return;
            }

            if (precioCompra < 0 || precioVenta < 0)
            {
                SetStatus("Los precios no pueden ser negativos.");
                return;
            }

            if (!int.TryParse(txtStock.Text, out int stock))
            {
                SetStatus("Stock inválido.");
                return;
            }

            if (stock < 0)
            {
                SetStatus("El stock no puede ser negativo.");
                return;
            }

            if (cmbProveedor.SelectedItem is null)
            {
                SetStatus("Selecciona un proveedor.");
                return;
            }

            SetStatus("Producto válido.", false);

            await Guardar_Click(sender, e);

        }

        private async Task Guardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productos = await _service.GetAllAsync();

                var nuevo = new Producto
                {
                    Id = Guid.NewGuid(),
                    Nombre = txtNombre.Text.Trim(),
                    Descripcion = txtDescripcion.Text.Trim(),
                    Categoria = (cmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString(),

                    PrecioCompra = decimal.Parse(txtPrecioCompra.Text),
                    PrecioVenta = decimal.Parse(txtPrecioVenta.Text),

                    Stock = int.Parse(txtStock.Text),

                    IdProveedor = ((Proveedor)cmbProveedor.SelectedItem).IdProveedor
                };

                
                productos.Add(nuevo);

                await _service.SaveAllAsync(productos);

                LimpiarFormulario();

                SetStatus("Registro exitoso.", false);


            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void LimpiarFormulario()
        {
            txtNombre.Text = "";
            txtDescripcion.Text = "";
            cmbCategoria.SelectedIndex = 0;

            txtPrecioCompra.Text = "";
            txtPrecioVenta.Text = "";
            txtStock.Text = "";

            cmbProveedor.SelectedIndex = 0;

            StatusTextBlock.Text = "";
        }

        private async Task SetStatus(string text, bool isError = true)
        {
            StatusTextBlock.Text = text;
            StatusTextBlock.Foreground = isError ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);

            await Task.Delay(3000);
            StatusTextBlock.Text = "";
        }

        private void MoveTo(object sender, RoutedEventArgs e)
        {
            Button boton = (Button)sender;

            if (boton.Name == "InventarioProductos")
            {
                Frame.Navigate(typeof(ListarProductosPage));
            }

            if (boton.Name == "reportesProductos")
            {
                Frame.Navigate(typeof(ReporteProductosPag));
            }
        }


    }
}
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

            Submit.IsEnabled = false;
            Loaded += async (_, __) => await CargarProveedores();

        }

        private readonly ProductService _service = new ProductService();
        private readonly ProveedorService _proveedorService = new ProveedorService();

        private async Task CargarProveedores()
        {
            var proveedores = await _proveedorService.GetProveedorAsync();

            cmbProveedor.ItemsSource = proveedores;
            Submit.IsEnabled = true;
        }

        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productos = await _service.GetProductsAsync();

                var nuevo = new Producto
                {
                    Id = Guid.NewGuid(),
                    Nombre = txtNombre.Text,
                    Descripcion = txtDescripcion.Text,
                    Categoria = cmbCategoria.Text,

                    PrecioCompra = decimal.Parse(txtPrecioCompra.Text),
                    PrecioVenta = decimal.Parse(txtPrecioVenta.Text),

                    Stock = int.Parse(txtStock.Text),

                    IdProveedor = ((Proveedor)cmbProveedor.SelectedItem).IdProveedor
                };

                
                productos.Add(nuevo);

                await _service.SaveProductsAsync(productos);

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

        private void SetStatus(string text, bool isError = true)
        {
            StatusTextBlock.Text = text;
            StatusTextBlock.Foreground = isError ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }

        private void MoveTo(object sender, RoutedEventArgs e)
        {
            Button boton = (Button)sender;

            if (boton.Name == "InventarioProductos")
            {
                Frame.Navigate(typeof(ListarProductosPage));
            }
        }


    }
}
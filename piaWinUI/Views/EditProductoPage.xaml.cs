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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    public sealed partial class EditProductoPage : Page
    {
        private readonly ProductService _service = new ProductService();
        private Producto? _producto;
        private readonly ProveedorService _proveedorService = new ProveedorService();

        public EditProductoPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await CargarProveedores();

            if (e.Parameter is Producto producto)
            {
                _producto = producto;

                txtNombre.Text = producto.Nombre;
                txtDescripcion.Text = producto.Descripcion;
                txtPrecioCompra.Text = producto.PrecioCompra.ToString();
                txtPrecioVenta.Text = producto.PrecioVenta.ToString();
                txtStock.Text = producto.Stock.ToString();

                // provider (SAFE now because ItemsSource is loaded first)
                cmbProveedor.SelectedValue = producto.IdProveedor;

                // category (string match)
                cmbCategoria.SelectedItem = cmbCategoria.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(x => (x.Content?.ToString() ?? "") == producto.Categoria);
            }
        }

        private async Task CargarProveedores()
        {
            var proveedores = await _proveedorService.GetAllAsync();

            cmbProveedor.ItemsSource = proveedores;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_producto == null) return;

            _producto.Nombre = txtNombre.Text.Trim();
            _producto.Descripcion = txtDescripcion.Text.Trim();

            _producto.PrecioCompra = decimal.Parse(txtPrecioCompra.Text);
            _producto.PrecioVenta = decimal.Parse(txtPrecioVenta.Text);
            _producto.Stock = int.Parse(txtStock.Text);

            if (cmbCategoria.SelectedItem is ComboBoxItem item)
                _producto.Categoria = item.Content?.ToString();

            if (cmbProveedor.SelectedValue is Guid id)
                _producto.IdProveedor = id;

            var productos = await _service.GetAllAsync();

            var index = productos.FindIndex(p => p.Id == _producto.Id);

            if (index != -1)
            {
                productos[index] = _producto;
                await _service.SaveAllAsync(productos);
            }

            Frame.GoBack();
        }
    }
}
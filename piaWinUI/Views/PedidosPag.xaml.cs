using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.WindowsAppSDK.Runtime.Packages;
using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

using piaWinUI.Services;

using System.Text.Json;

namespace piaWinUI
{
    public sealed partial class PedidosPag : Page
    {
        private PedidoService _pedidoService = AppServices.Pedido;

        private List<Producto> _productos = new();
        private List<Proveedor> _proveedores = new();

        public PedidosPag()
        {
            this.InitializeComponent();
            CargarDatos();
        }

        private async void CargarDatos()
        {
            try
            {
                
                if (!File.Exists("productos.json"))
                    throw new Exception("No se encontró productos.json");

                var jsonProductos = File.ReadAllText("productos.json");

                _productos = JsonSerializer.Deserialize<List<Producto>>(jsonProductos) ?? new();

                cmbProducto.ItemsSource = _productos;

                if (!File.Exists("proveedores.json"))
                    throw new Exception("No se encontró proveedores.json");

                var jsonProveedores = File.ReadAllText("proveedores.json");

                _proveedores = JsonSerializer.Deserialize<List<Proveedor>>(jsonProveedores) ?? new();

                cmbProveedor.ItemsSource = _proveedores;
            }
            catch (Exception ex)
            {
                await MostrarDialogo("Error al cargar datos", ex.Message);
            }
        }

        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var producto = cmbProducto.SelectedItem as Producto;
            if (producto == null) return;

            var proveedor = _proveedores
                .FirstOrDefault(p => p.IdProveedor == producto.IdProveedor);

            cmbProveedor.SelectedItem = proveedor;
        }

        private async void RegistrarPedido_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var producto = cmbProducto.SelectedItem as Producto;
                var proveedor = cmbProveedor.SelectedItem as Proveedor;

                if (producto == null)
                    throw new Exception("Selecciona un producto");

                if (proveedor == null)
                    throw new Exception("Proveedor inválido");

                if (!int.TryParse(txtCantidad.Text, out int cantidad))
                    throw new Exception("Cantidad inválida");

                _pedidoService.RegistrarPedido(producto.Nombre, proveedor.Nombre, cantidad);

                gridPedidos.ItemsSource = null;
                gridPedidos.ItemsSource = _pedidoService.ObtenerPedidos();

                txtCantidad.Text = "";

                await MostrarDialogo("OK", "Pedido registrado");
            }
            catch (Exception ex)
            {
                await MostrarDialogo("Error", ex.Message);
            }
        }

        private void Buscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filtro = txtBuscar.Text.ToLower();

            var filtrados = _pedidoService.ObtenerPedidos()
                .Where(p =>
                    p.Producto.ToLower().Contains(filtro) ||
                    p.Proveedor.ToLower().Contains(filtro))
                .ToList();

            gridPedidos.ItemsSource = filtrados;
        }

        private async System.Threading.Tasks.Task MostrarDialogo(string t, string m)
        {
            var dialog = new ContentDialog
            {
                Title = t,
                Content = m,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
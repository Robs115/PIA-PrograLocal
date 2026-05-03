
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            this.Loaded += PedidosPag_Loaded;
        }

        private void PedidosPag_Loaded(object sender, RoutedEventArgs e)
        {
            CargarDatos();
        }

        // 🔥 CARGA SEGURA (NO CRASHEA)
        private void CargarDatos()
        {
            _productos = CargarListaSegura<Producto>("productos.json");
            _proveedores = CargarListaSegura<Proveedor>("proveedores.json");

            cmbProducto.ItemsSource = _productos;
            cmbProveedor.ItemsSource = _proveedores;

            // Desactiva botón si no hay datos
            btnRegistrar.IsEnabled = _productos.Any() && _proveedores.Any();
        }

        // 🔥 MÉTODO SEGURO
        private List<T> CargarListaSegura<T>(string ruta)
        {
            try
            {
                if (!File.Exists(ruta))
                    return new List<T>();

                var json = File.ReadAllText(ruta);

                var datos = JsonSerializer.Deserialize<List<T>>(json);

                return datos ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        // 🔥 AUTOSELECCIÓN
        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var producto = cmbProducto.SelectedItem as Producto;
                if (producto == null) return;

                var proveedor = _proveedores
                    .FirstOrDefault(p => p.IdProveedor == producto.IdProveedor);

                cmbProveedor.SelectedItem = proveedor;
            }
            catch
            {
                // no rompe
            }
        }

        // 🔥 REGISTRAR PEDIDO
        private async void RegistrarPedido_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_productos.Any())
                {
                    await MostrarDialogo("Sin datos", "No hay productos disponibles");
                    return;
                }

                if (!_proveedores.Any())
                {
                    await MostrarDialogo("Sin datos", "No hay proveedores disponibles");
                    return;
                }

                var producto = cmbProducto.SelectedItem as Producto;
                var proveedor = cmbProveedor.SelectedItem as Proveedor;

                if (producto == null)
                {
                    await MostrarDialogo("Validación", "Selecciona un producto");
                    return;
                }

                if (proveedor == null)
                {
                    await MostrarDialogo("Validación", "Selecciona un proveedor");
                    return;
                }

                if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
                {
                    await MostrarDialogo("Validación", "Cantidad inválida");
                    return;
                }

                _pedidoService.RegistrarPedido(producto.Nombre, proveedor.Nombre, cantidad);

                gridPedidos.ItemsSource = null;
                gridPedidos.ItemsSource = _pedidoService.ObtenerPedidos();

                txtCantidad.Text = "";

                await MostrarDialogo("Éxito", "Pedido registrado");
            }
            catch (Exception ex)
            {
                await MostrarDialogo("Error", ex.Message);
            }
        }

        // 🔍 BUSCAR
        private void Buscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var filtro = txtBuscar.Text?.ToLower() ?? "";

                var filtrados = _pedidoService.ObtenerPedidos()
                    .Where(p =>
                        p.Producto.ToLower().Contains(filtro) ||
                        p.Proveedor.ToLower().Contains(filtro))
                    .ToList();

                gridPedidos.ItemsSource = filtrados;
            }
            catch
            {
                // no rompe
            }
        }

        // 🔔 DIALOGO
        private async System.Threading.Tasks.Task MostrarDialogo(string titulo, string mensaje)
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
}
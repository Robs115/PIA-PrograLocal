using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace piaWinUI.Views
{
    public sealed partial class PedidosPag : Page
    {
        private readonly ProveedorService _proveedorService = new();
        private readonly ProductService _productService = new();
        private readonly PedidoService _pedidoService = new();

        private List<Proveedor> _proveedores = new();
        private List<Producto> _productos = new();
        private List<PedidoView> _pedidos = new();

        // 🔥 EVITAR BUCLES
        private bool _actualizandoCombos = false;

        // 🔥 GUARDAR ESTADO ENTRE PAGINAS
        private Guid? _productoSeleccionadoId;
        private Guid? _proveedorSeleccionadoId;

        private string _cantidadTemporal = "";
        private string _busquedaTemporal = "";

        public PedidosPag()
        {
            this.InitializeComponent();

            Loaded += PedidosPag_Loaded;

            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            txtBuscar.TextChanged += TxtBuscarEstado_TextChanged;
        }

        // 🔥 CARGA INICIAL
        private async void PedidosPag_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarTodo();
        }

        // 🔥 CARGAR TODO
        private async Task CargarTodo()
        {
            try
            {
                _productos = await _productService.GetAllAsync() ?? new();
                _proveedores = await _proveedorService.GetAllAsync() ?? new();

                var pedidosModel = await _pedidoService.GetAllAsync();

                _pedidos = pedidosModel?
                    .Select(p => new PedidoView
                    {
                        NombreProducto = p.NombreProducto,
                        NombreProveedor = p.NombreProveedor,
                        Cantidad = p.Cantidad,
                        Fecha = p.Fecha
                    })
                    .OrderByDescending(p => p.Fecha)
                    .ToList()
                    ?? new();

                // 🔥 COMBOS
                cmbProductoPedido.ItemsSource = _productos;
                cmbProductoPedido.DisplayMemberPath = "Nombre";

                cmbProveedorPedido.ItemsSource = _proveedores;
                cmbProveedorPedido.DisplayMemberPath = "Nombre";

                // 🔥 GRID
                gridPedidos.ItemsSource = _pedidos;

                // 🔥 RESTAURAR ESTADO
                RestaurarEstado();
            }
            catch (Exception ex)
            {
                await MostrarDialogo("Error",
                    $"Error al cargar datos.\n{ex.Message}");
            }
        }

        // 🔥 RESTAURAR ESTADO
        private void RestaurarEstado()
        {
            try
            {
                _actualizandoCombos = true;

                // 🔥 PRODUCTO
                if (_productoSeleccionadoId.HasValue)
                {
                    var producto = _productos
                        .FirstOrDefault(p =>
                            p.Id == _productoSeleccionadoId.Value);

                    if (producto != null)
                    {
                        cmbProductoPedido.SelectedItem = producto;
                    }
                }

                // 🔥 PROVEEDOR
                if (_proveedorSeleccionadoId.HasValue)
                {
                    var proveedor = _proveedores
                        .FirstOrDefault(p =>
                            p.IdProveedor == _proveedorSeleccionadoId.Value);

                    if (proveedor != null)
                    {
                        cmbProveedorPedido.SelectedItem = proveedor;
                    }
                }

                // 🔥 TEXTOS
                txtCantidadPedido.Text = _cantidadTemporal;
                txtBuscar.Text = _busquedaTemporal;

                // 🔥 FILTRO
                if (!string.IsNullOrWhiteSpace(_busquedaTemporal))
                {
                    FiltrarPedidos(_busquedaTemporal);
                }
            }
            finally
            {
                _actualizandoCombos = false;
            }
        }

        // 🔥 FILTRAR
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarPedidos(txtBuscar.Text);
        }

        private void FiltrarPedidos(string texto)
        {
            if (_pedidos == null)
                return;

            texto ??= "";

            var filtrados = _pedidos
                .Where(p =>
                    p.NombreProducto.Contains(texto,
                        StringComparison.OrdinalIgnoreCase) ||

                    p.NombreProveedor.Contains(texto,
                        StringComparison.OrdinalIgnoreCase) ||

                    p.Cantidad.ToString().Contains(texto) ||

                    p.FechaFormateada.Contains(texto,
                        StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            gridPedidos.ItemsSource = filtrados;
        }

        // 🔥 GUARDAR BUSQUEDA
        private void TxtBuscarEstado_TextChanged(object sender, TextChangedEventArgs e)
        {
            _busquedaTemporal = txtBuscar.Text;
        }

        // 🔥 VALIDAR CANTIDAD
        private void txtCantidadPedido_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _cantidadTemporal = txtCantidadPedido.Text;

                // 🔥 SOLO NUMEROS
                string soloNumeros = new string(
                    txtCantidadPedido.Text
                    .Where(char.IsDigit)
                    .ToArray());

                // 🔥 MAXIMO 4
                if (soloNumeros.Length > 4)
                {
                    soloNumeros = soloNumeros.Substring(0, 4);
                }

                // 🔥 EVITAR RECURSION
                if (txtCantidadPedido.Text != soloNumeros)
                {
                    int cursor = txtCantidadPedido.SelectionStart;

                    txtCantidadPedido.Text = soloNumeros;

                    txtCantidadPedido.SelectionStart =
                        Math.Min(cursor, soloNumeros.Length);
                }
            }
            catch
            {
                txtCantidadPedido.Text = "";
            }
        }

        // 🔥 PRODUCTO CAMBIADO
        private void cmbProductoPedido_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_actualizandoCombos)
                return;

            try
            {
                _actualizandoCombos = true;

                if (cmbProductoPedido.SelectedItem is Producto productoSeleccionado)
                {
                    _productoSeleccionadoId = productoSeleccionado.Id;
                }
                else
                {
                    _productoSeleccionadoId = null;
                }

                var producto = cmbProductoPedido.SelectedItem as Producto;

                // 🔥 SI QUITAN PRODUCTO
                if (producto == null)
                {
                    cmbProveedorPedido.IsEnabled = true;

                    cmbProductoPedido.ItemsSource = _productos;

                    return;
                }

                // 🔥 BUSCAR PROVEEDOR
                var proveedor = _proveedores
                    .FirstOrDefault(p =>
                        p.IdProveedor == producto.IdProveedor);

                if (proveedor != null)
                {
                    cmbProveedorPedido.SelectedItem = proveedor;

                    cmbProveedorPedido.IsEnabled = false;
                }
            }
            finally
            {
                _actualizandoCombos = false;
            }
        }

        // 🔥 PROVEEDOR CAMBIADO
        private void cmbProveedorPedido_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_actualizandoCombos)
                return;

            try
            {
                _actualizandoCombos = true;

                if (cmbProveedorPedido.SelectedItem is Proveedor proveedorSeleccionado)
                {
                    _proveedorSeleccionadoId =
                        proveedorSeleccionado.IdProveedor;
                }
                else
                {
                    _proveedorSeleccionadoId = null;
                }

                // 🔥 SI YA HAY PRODUCTO
                if (cmbProductoPedido.SelectedItem != null)
                    return;

                var proveedor = cmbProveedorPedido.SelectedItem as Proveedor;

                // 🔥 RESTAURAR TODO
                if (proveedor == null)
                {
                    cmbProductoPedido.ItemsSource = _productos;
                    return;
                }

                // 🔥 FILTRAR PRODUCTOS
                var productosFiltrados = _productos
                    .Where(p =>
                        p.IdProveedor == proveedor.IdProveedor)
                    .ToList();

                cmbProductoPedido.ItemsSource = productosFiltrados;
            }
            finally
            {
                _actualizandoCombos = false;
            }
        }

        // 🔥 REGISTRAR
        private async void RegistrarPedido_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔥 VALIDAR PRODUCTO
                if (cmbProductoPedido.SelectedItem == null)
                {
                    await MostrarDialogo("Producto requerido",
                        "Selecciona un producto.");
                    return;
                }

                // 🔥 VALIDAR PROVEEDOR
                if (cmbProveedorPedido.SelectedItem == null)
                {
                    await MostrarDialogo("Proveedor requerido",
                        "Selecciona un proveedor.");
                    return;
                }

                var producto = cmbProductoPedido.SelectedItem as Producto;
                var proveedor = cmbProveedorPedido.SelectedItem as Proveedor;

                // 🔥 VALIDAR CANTIDAD VACIA
                if (string.IsNullOrWhiteSpace(txtCantidadPedido.Text))
                {
                    await MostrarDialogo("Cantidad requerida",
                        "Ingresa una cantidad.");
                    return;
                }

                // 🔥 VALIDAR ENTERO
                if (!int.TryParse(txtCantidadPedido.Text, out int cantidad))
                {
                    await MostrarDialogo("Cantidad inválida",
                        "Solo se permiten números.");
                    return;
                }

                // 🔥 VALIDAR MAYOR A 0
                if (cantidad <= 0)
                {
                    await MostrarDialogo("Cantidad inválida",
                        "La cantidad debe ser mayor a 0.");
                    return;
                }

                // 🔥 VALIDAR LIMITE
                if (cantidad > 1000)
                {
                    await MostrarDialogo("Cantidad demasiado grande",
                        "Máximo permitido: 1000");
                    return;
                }

                // 🔥 VALIDAR RELACION
                if (producto.IdProveedor != proveedor.IdProveedor)
                {
                    await MostrarDialogo("Error",
                        "El producto no pertenece al proveedor.");
                    return;
                }

                // 🔥 OBTENER PEDIDOS
                var pedidosModel =
                    await _pedidoService.GetAllAsync();

                // 🔥 NUEVO PEDIDO
                var nuevo = new Pedidos
                {
                    Id = Guid.NewGuid(),

                    IdProducto = producto.Id,
                    IdProveedor = proveedor.IdProveedor,

                    NombreProducto = producto.Nombre,
                    NombreProveedor = proveedor.Nombre,

                    Cantidad = cantidad,
                    Fecha = DateTime.Now
                };

                pedidosModel.Add(nuevo);

                // 🔥 AUMENTAR STOCK
                producto.Stock += cantidad;

                // 🔥 GUARDAR
                await _pedidoService.SaveAllAsync(pedidosModel);

                await _productService.SaveAllAsync(_productos);

                // 🔥 RECARGAR
                await CargarPedidos();

                // 🔥 LIMPIAR
                LimpiarFormulario();

                await MostrarDialogo("Pedido registrado",
                    "El pedido fue registrado correctamente.");
            }
            catch (Exception ex)
            {
                await MostrarDialogo("Error inesperado",
                    ex.Message);
            }
        }

        // 🔥 LIMPIAR
        private void LimpiarFormulario()
        {
            _actualizandoCombos = true;

            txtCantidadPedido.Text = "";

            cmbProductoPedido.SelectedItem = null;
            cmbProveedorPedido.SelectedItem = null;

            cmbProveedorPedido.IsEnabled = true;

            cmbProductoPedido.ItemsSource = _productos;

            _actualizandoCombos = false;
        }

        // 🔥 RECARGAR PEDIDOS
        private async Task CargarPedidos()
        {
            try
            {
                var pedidosModel =
                    await _pedidoService.GetAllAsync();

                _pedidos = pedidosModel?
                    .Select(p => new PedidoView
                    {
                        NombreProducto = p.NombreProducto,
                        NombreProveedor = p.NombreProveedor,
                        Cantidad = p.Cantidad,
                        Fecha = p.Fecha
                    })
                    .OrderByDescending(p => p.Fecha)
                    .ToList()
                    ?? new();

                gridPedidos.ItemsSource = _pedidos;
            }
            catch (Exception ex)
            {
                await MostrarDialogo("Error",
                    $"No se pudieron cargar pedidos.\n{ex.Message}");
            }
        }

        // 🔥 DIALOGO
        private async Task MostrarDialogo(string titulo, string mensaje)
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

    // 🔥 VIEW GRID
    public class PedidoView
    {
        public string NombreProducto { get; set; } = "";
        public string NombreProveedor { get; set; } = "";
        public int Cantidad { get; set; }

        public DateTime Fecha { get; set; }

        public string FechaFormateada =>
            Fecha.ToString("dd/MM/yyyy HH:mm");
    }
}
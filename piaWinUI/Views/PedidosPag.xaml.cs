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
        private int? _productoSeleccionadoId;
        private int? _proveedorSeleccionadoId;

        private string _cantidadTemporal = "";
        private string _busquedaTemporal = "";

        public PedidosPag()
        {
            this.InitializeComponent();

            Loaded += PedidosPag_Loaded;

            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            // 🔥 VALIDAR BUSCADOR
            ValidarTexto(txtBuscar);
        }

        // 🔥 VALIDAR BUSCADOR
        private void ValidarTexto(TextBox tb)
        {
            tb.BeforeTextChanging += (s, e) =>
            {
                string texto = e.NewText;

                // ✅ Permitir vacío
                if (string.IsNullOrWhiteSpace(texto))
                    return;

                // ❌ No iniciar con espacio
                if (texto.StartsWith(" "))
                {
                    e.Cancel = true;
                    return;
                }

                // ❌ No permitir espacios dobles
                if (texto.Contains("  "))
                {
                    e.Cancel = true;
                    return;
                }

                // ❌ Bloquear caracteres especiales
                foreach (char c in texto)
                {
                    bool valido =
                        char.IsLetterOrDigit(c) ||
                        char.IsWhiteSpace(c);

                    if (!valido)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            };

            // 🔥 LIMPIEZA EXTRA
            tb.TextChanged += (s, e) =>
            {
                try
                {
                    string texto = tb.Text;

                    if (string.IsNullOrEmpty(texto))
                        return;

                    // 🔥 QUITAR DOBLES ESPACIOS
                    while (texto.Contains("  "))
                    {
                        texto = texto.Replace("  ", " ");
                    }

                    // 🔥 QUITAR ESPACIOS AL INICIO
                    texto = texto.TrimStart();

                    // 🔥 EVITAR RECURSION
                    if (tb.Text != texto)
                    {
                        int cursor = tb.SelectionStart;

                        tb.Text = texto;

                        tb.SelectionStart =
                            Math.Min(cursor, texto.Length);
                    }
                }
                catch
                {
                    tb.Text = "";
                }
            };
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
                            p.Id == _proveedorSeleccionadoId.Value);

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

        // 🔥 BUSCADOR
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string texto = txtBuscar.Text?.Trim() ?? "";

                _busquedaTemporal = texto;

                // 🔥 SI ESTA VACIO
                if (string.IsNullOrWhiteSpace(texto))
                {
                    gridPedidos.ItemsSource = _pedidos;
                    return;
                }

                FiltrarPedidos(texto);
            }
            catch
            {
                gridPedidos.ItemsSource = _pedidos;
            }
        }

        // 🔥 FILTRAR
        private void FiltrarPedidos(string texto)
        {
            if (_pedidos == null)
                return;

            texto = texto?.Trim() ?? "";

            // 🔥 SI ESTA VACIO
            if (string.IsNullOrWhiteSpace(texto))
            {
                gridPedidos.ItemsSource = _pedidos;
                return;
            }

            var filtrados = _pedidos
                .Where(p =>

                    (!string.IsNullOrWhiteSpace(p.NombreProducto) &&
                     p.NombreProducto.Contains(texto,
                         StringComparison.OrdinalIgnoreCase))

                    ||

                    (!string.IsNullOrWhiteSpace(p.NombreProveedor) &&
                     p.NombreProveedor.Contains(texto,
                         StringComparison.OrdinalIgnoreCase))

                    ||

                    p.Cantidad.ToString().Contains(texto)

                    ||

                    p.FechaFormateada.Contains(texto,
                        StringComparison.OrdinalIgnoreCase)

                )
                .ToList();

            gridPedidos.ItemsSource = filtrados;
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
                        p.Id == producto.IdProveedor);

                if (proveedor != null)
                {
                    cmbProveedorPedido.SelectedItem = proveedor;

                    // 🔥 BLOQUEAR CAMBIO
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
                        proveedorSeleccionado.Id;
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
                        p.IdProveedor == proveedor.Id)
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
                if (cmbProductoPedido.SelectedItem == null)
                {
                    await MostrarDialogo("Producto requerido",
                        "Selecciona un producto.");
                    return;
                }

                if (cmbProveedorPedido.SelectedItem == null)
                {
                    await MostrarDialogo("Proveedor requerido",
                        "Selecciona un proveedor.");
                    return;
                }

                var producto = cmbProductoPedido.SelectedItem as Producto;
                var proveedor = cmbProveedorPedido.SelectedItem as Proveedor;

                if (string.IsNullOrWhiteSpace(txtCantidadPedido.Text))
                {
                    await MostrarDialogo("Cantidad requerida",
                        "Ingresa una cantidad.");
                    return;
                }

                if (!int.TryParse(txtCantidadPedido.Text, out int cantidad))
                {
                    await MostrarDialogo("Cantidad inválida",
                        "Solo se permiten números.");
                    return;
                }

                if (cantidad <= 0)
                {
                    await MostrarDialogo("Cantidad inválida",
                        "La cantidad debe ser mayor a 0.");
                    return;
                }

                if (cantidad > 1000)
                {
                    await MostrarDialogo("Cantidad demasiado grande",
                        "Máximo permitido: 1000");
                    return;
                }

                if (producto.IdProveedor != proveedor.Id)
                {
                    await MostrarDialogo("Error",
                        "El producto no pertenece al proveedor.");
                    return;
                }

                // ✨ LÓGICA DE CAJA (AÑADIDA) ✨
                if (!CajaState.IsCajaAbierta)
                {
                    await MostrarDialogo("Caja Cerrada",
                        "Debes abrir la caja (en Ventas) para poder registrar un pago a proveedor.");
                    return;
                }

                // ✨ CALCULAR EL COSTO AUTOMÁTICAMENTE ✨
                // Multiplicamos la cantidad solicitada por el precio.
                // NOTA: Si en tu modelo 'Producto' tienes una propiedad llamada 'PrecioCompra' o 'Costo',
                // puedes cambiar 'producto.PrecioVenta' por esa propiedad para mayor exactitud.
                decimal costoCalculado = cantidad * producto.PrecioVenta;

                var dialogMonto = new ContentDialog
                {
                    Title = "Registrar Egreso de Caja",
                    Content = $"Se descontarán automáticamente ${costoCalculado} de la caja por el pedido de {cantidad}x {producto.Nombre}.\n\n¿Deseas continuar?",
                    PrimaryButtonText = "Registrar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await dialogMonto.ShowAsync() != ContentDialogResult.Primary)
                    return;

                decimal montoEgreso = costoCalculado;

                if (montoEgreso <= 0)
                {
                    await MostrarDialogo("Monto inválido", "El egreso debe ser mayor a 0.");
                    return;
                }
                // ✨ FIN LÓGICA DE CAJA ✨

                var pedidosModel =
                    await _pedidoService.GetAllAsync();

                var nuevo = new Pedidos
                {
                    Id = pedidosModel.Any() ? pedidosModel.Max(p => p.Id) + 1 : 1,

                    IdProducto = producto.Id,
                    IdProveedor = proveedor.Id,

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

                // ✨ REGISTRAR EL MOVIMIENTO EN LA CAJA ✨
                CajaState.Movimientos.Add(new MovimientoCaja
                {
                    Fecha = DateTime.Now,
                    Tipo = "Egreso",
                    Concepto = $"Pedido: {cantidad}x {producto.Nombre} a {proveedor.Nombre}",
                    Monto = montoEgreso
                });

                // 🔥 RECARGAR
                await CargarPedidos();

                // 🔥 LIMPIAR
                LimpiarFormulario();

                await MostrarDialogo("Pedido registrado",
                    "El pedido y el egreso se registraron correctamente.");
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
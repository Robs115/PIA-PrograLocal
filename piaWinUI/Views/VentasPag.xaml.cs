using Microsoft.UI.Text;
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace piaWinUI.Views
{
    // ✨ ESTRUCTURA PARA GUARDAR CADA MOVIMIENTO
    public class MovimientoCaja
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }
        public string Concepto { get; set; }
        public decimal Monto { get; set; }
    }

    // ✨ ESTADO GLOBAL DE LA CAJA AÑADIDO AQUÍ ✨
    public static class CajaState
    {
        public static bool IsCajaAbierta { get; set; } = false;
        public static DateTime? HoraApertura { get; set; }
        public static decimal FondoInicial { get; set; } = 0;

        // Aquí se guardará el detalle de cada egreso/pedido y ahora también ingresos
        public static List<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();
    }

    public sealed partial class VentasPag : Page
    {
        // 🔥 1. SERVICIOS ACTUALIZADOS (Inyección de dependencias)
        private ProductService _productoService;
        private DetalleVentasService _detalleVentasService;
        private VentasService _ventaService;

        private List<Producto> todosProductos = new List<Producto>();
        private ObservableCollection<DetalleVentas> carrito = new ObservableCollection<DetalleVentas>();
        private bool _isDialogOpen = false;

        private DispatcherTimer _timer;

        public VentasPag()
        {
            this.InitializeComponent();

            // 🔥 2. INICIALIZACIÓN DE SERVICIOS
            _productoService = new ProductService();
            _detalleVentasService = new DetalleVentasService();
            _ventaService = new VentasService(_productoService, _detalleVentasService);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Cada segundo
            _timer.Tick += Timer_Tick;
            _timer.Start();
            ProductosList.ItemsSource = carrito;
            cargarCatalogo();
            ResultadosProductosList.ItemsSource = todosProductos;

            // ✨ ACTUALIZAR BOTÓN DE CAJA AL CARGAR LA PÁGINA
            ActualizarUIBotonCaja();
        }

        private void SoloNumeros(
            TextBox tb,
            bool allowDecimal = false)
        {
            tb.BeforeTextChanging += (s, e) =>
            {
                if (e.NewText.Contains(" "))
                {
                    e.Cancel = true;
                    return;
                }

                if (string.IsNullOrEmpty(e.NewText))
                    return;

                if (allowDecimal)
                {
                    e.Cancel =
                        !decimal.TryParse(
                            e.NewText,
                            out _);
                }
                else
                {
                    e.Cancel =
                        !int.TryParse(
                            e.NewText,
                            out _);
                }
            };
        }

        private void NumberBoxCantidad_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // 1. Teclas permitidas para navegar y borrar
            bool esTeclaControl = e.Key == Windows.System.VirtualKey.Back ||
                                  e.Key == Windows.System.VirtualKey.Tab ||
                                  e.Key == Windows.System.VirtualKey.Left ||
                                  e.Key == Windows.System.VirtualKey.Right ||
                                  e.Key == Windows.System.VirtualKey.Delete;

            // 2. Teclas numéricas (Teclado superior y Numpad)
            bool esNumeroNormal = e.Key >= Windows.System.VirtualKey.Number0 && e.Key <= Windows.System.VirtualKey.Number9;
            bool esNumeroNumpad = e.Key >= Windows.System.VirtualKey.NumberPad0 && e.Key <= Windows.System.VirtualKey.NumberPad9;

            // Si NO es número y NO es tecla de control -> Bloquear de inmediato
            if (!esTeclaControl && !esNumeroNormal && !esNumeroNumpad)
            {
                e.Handled = true;
                return;
            }

            // 3. BLOQUEO ESTRICTO DE 4 CARACTERES
            if (sender is NumberBox numberBox && !esTeclaControl)
            {
                string textoActual = numberBox.Text ?? ""; // Evita errores si el texto está vacío

                // Si ya hay 4 caracteres, ignoramos cualquier tecla que no sea borrar
                if (textoActual.Length >= 4)
                {
                    e.Handled = true;
                }
            }
        }

        private void NumberBoxCantidad_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is NumberBox numberBox)
            {
                // Buscamos el TextBox oculto dentro del NumberBox
                TextBox textBoxInterno = FindVisualChild<TextBox>(numberBox);

                if (textBoxInterno != null)
                {
                    // Le aplicamos el límite nativo. ¡De aquí no pasará!
                    textBoxInterno.MaxLength = 4;
                }
            }
        }

        // Método auxiliar obligatorio para buscar elementos ocultos en la interfaz
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void Timer_Tick(object sender, object e)
        {
            // Actualiza el TextBlock con la hora actual
            var horaActual = DateTime.Now.ToString("HH:mm:ss"); // Formato 24h

            // Obtenemos el nombre del usuario logueado en la sesión
            string nombreCajero = SessionService.CurrentUser?.Username ?? "Desconocido";

            CajeroHoraTextBlock.Text = $"Cajero: {nombreCajero} | {horaActual}";
        }
        private void BuscadorNombre_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {

            var regex = @"^(?!.* )[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s'-]*$";
            args.Cancel = !System.Text.RegularExpressions.Regex.IsMatch(args.NewText, regex);
        }

        private void BuscadorCodigo_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            // Solo dígitos del 0 al 9
            var regex = @"^[0-9]*$";

            // Si el nuevo texto NO coincide con la expresión, cancelar el cambio
            args.Cancel = !System.Text.RegularExpressions.Regex.IsMatch(args.NewText, regex);
        }

        private void AgregarProductoAlCarrito(Producto producto)
        {
            var existente = carrito.FirstOrDefault(p => p.IdProducto == producto.Id);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                var nuevo = new DetalleVentas
                {
                    IdProducto = producto.Id,
                    NombreProducto = producto.Nombre,
                    PrecioUnitario = producto.PrecioVenta,
                    Cantidad = 1,
                    StockDisponible = producto.Stock
                };

                nuevo.OnError += async (mensaje) =>
                {
                    await ShowDialogAsync("Error", mensaje);
                };

                nuevo.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(DetalleVentas.Cantidad) ||
                        e.PropertyName == nameof(DetalleVentas.PrecioUnitario))
                    {
                        ActualizarTotal();
                    }
                };

                carrito.Add(nuevo);

                // 🔥 forzar cálculo inicial
                nuevo.Cantidad = nuevo.Cantidad;
            }

            ActualizarTotal();
        }

        private async Task ShowDialogAsync(string title, string content)
        {
            // Si ya hay un diálogo abierto, cancelamos la nueva petición para evitar el crash
            if (_isDialogOpen) return;

            _isDialogOpen = true; // Cerramos el candado

            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch
            {
                // Si ocurre un error de dibujado, lo ignoramos para que no afecte la ejecución
            }
            finally
            {
                // Al cerrar el diálogo, liberamos el candado para futuros mensajes
                _isDialogOpen = false;
            }
        }

        // 🔥 TOTAL
        private void ActualizarTotal()
        {
            var total = carrito.Sum(p => p.Subtotal);
            TotalText.Text = $" {total:C}";
        }
        private void limpiarbarrabusqueda()
        {
            CodigoBox.Text = "";
        }

        // 🔥 AGREGAR PRODUCTO
        private async void CargarProductos()
        {
            var productos = await _productoService.GetAllAsync() ?? new List<Producto>();

            string codigo = CodigoBox.Text;

            if (string.IsNullOrWhiteSpace(codigo))
            {
                await ShowDialogAsync("Error", "No hay código.");
                return;
            }

            var producto = productos.FirstOrDefault(p => p.CodigoBarras == codigo);

            if (producto == null)
            {
                await ShowDialogAsync("Error", "Producto no encontrado");
                limpiarbarrabusqueda();
                return;
            }

            if (producto.Stock <= 0)
            {
                await ShowDialogAsync("Error", "No hay stock de este producto");
                limpiarbarrabusqueda();
                return;
            }

            // 🔥 SOLO ESTO
            AgregarProductoAlCarrito(producto);

            limpiarbarrabusqueda();
        }
        private async void EliminarProducto_Click(object sender, RoutedEventArgs e)
        {

            var button = sender as Button;
            var item = button?.Tag as DetalleVentas;

            if (item == null)
                return;

            var confirm = new ContentDialog
            {
                Title = "Eliminar",
                Content = "¿Eliminar producto?",
                PrimaryButtonText = "Sí",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                carrito.Remove(item);
                ActualizarTotal();
            }
        }

        private void EscanearButton_Click(object sender, RoutedEventArgs e)
        {
            CargarProductos();
        }
        private void obtenerproducto_keydown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                CargarProductos();
            }
        }
        private void BotonEfectivo_Click(object sender, RoutedEventArgs e)
        {
            GuardarVenta_Click("Efectivo");
        }

        private void BotonTarjeta_Click(object sender, RoutedEventArgs e)
        {
            GuardarVenta_Click("Tarjeta");
        }
        // GUARDR VENTA
        private async void GuardarVenta_Click(string metododepago)
        {
            // ✨ VALIDACIÓN: VERIFICAR SI LA CAJA ESTÁ ABIERTA
            if (!CajaState.IsCajaAbierta)
            {
                await ShowDialogAsync("Caja Cerrada", "Debes abrir la caja antes de registrar una venta.");
                return;
            }

            var ventas = await _ventaService.GetAllAsync() ?? new List<Venta>();
            decimal cambio = 0;
            if (carrito.Count == 0)
            {
                await ShowDialogAsync("Error", "No hay productos en la venta");
                return;
            }

            // Validación final
            if (carrito.Any(p => p.TieneError))
            {
                await ShowDialogAsync("Error", "Hay productos con cantidades inválidas");
                return;
            }

            if (string.IsNullOrWhiteSpace(metododepago))
                return;

            // --- 1. PROCESO DE PAGO SEGÚN EL MÉTODO ---
            if (metododepago == "Tarjeta")
            {
                var confirm = new ContentDialog
                {
                    Title = "Confirmar Pago",
                    Content = $"Total a pagar: {TotalText.Text}\n¿Confirmar pago con tarjeta?",
                    PrimaryButtonText = "Sí",
                    CloseButtonText = "No",
                    XamlRoot = this.Content.XamlRoot
                };
                if (await confirm.ShowAsync() != ContentDialogResult.Primary)
                    return;

                await ProcesarPagoAnimacion();
            }
            else if (metododepago == "Efectivo")
            {
                decimal total = carrito.Sum(p => p.Subtotal);

                var inputBox = new TextBox
                {
                    Header = "Ingrese cantidad de efectivo recibida",
                    PlaceholderText = total.ToString(),
                    InputScope = new InputScope
                    {
                        Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } }
                    }
                };

                var efectivoDialog = new ContentDialog
                {
                    Title = $"Total a pagar: ${total}",
                    Content = inputBox,
                    PrimaryButtonText = "Cobrar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await efectivoDialog.ShowAsync() != ContentDialogResult.Primary)
                    return;

                if (!decimal.TryParse(inputBox.Text, out decimal efectivoEntregado))
                {
                    await ShowDialogAsync("Error", "Cantidad inválida");
                    return;
                }

                if (efectivoEntregado < total)
                {
                    await ShowDialogAsync("Error", "El efectivo entregado es menor que el total");
                    return;
                }

                // Si necesitas hacer algo con el cambio, la variable está aquí
                 cambio = efectivoEntregado - total;
            }

            // --- 2. GUARDAR LA VENTA EN LA BASE DE DATOS (Aplica para ambos pagos) ---
            var venta = new Venta
            {
                Id = ventas.Any() ? ventas.Max(v => v.Id) + 1 : 1,
                UserName = SessionService.CurrentUser.Username,
                MetodoPago = metododepago,
                Fecha = DateTime.Now,
                Total = carrito.Sum(p => p.Subtotal)
            };

            foreach (var item in carrito)
            {
                item.IdVenta = venta.Id;
            }

            ventas.Add(venta);
            await _ventaService.SaveAllAsync(ventas);

            var detalles = await _detalleVentasService.GetAllAsync() ?? new List<DetalleVentas>();
            detalles.AddRange(carrito);
            await _detalleVentasService.SaveAllAsync(detalles);

            var productos = await _productoService.GetAllAsync() ?? new List<Producto>();
            foreach (var item in carrito)
            {
                var producto = productos.FirstOrDefault(p => p.Id == item.IdProducto);
                if (producto != null)
                {
                    producto.Stock -= item.Cantidad;
                    if (producto.Stock < 0)
                        producto.Stock = 0;
                }
            }

            await _productoService.SaveAllAsync(productos);

            // ✨ REGISTRAR EL INGRESO EN LOS MOVIMIENTOS DE CAJA ✨
            // Creamos un resumen de lo que se vendió (Ej: "2x Coca Cola, 1x Sabritas")
            string articulosResumen = string.Join(", ", carrito.Select(c => $"{c.Cantidad}x {c.NombreProducto}"));

            // Si el resumen es muy largo, lo cortamos para que no desborde la interfaz
            if (articulosResumen.Length > 45)
            {
                articulosResumen = articulosResumen.Substring(0, 42) + "...";
            }

            CajaState.Movimientos.Add(new MovimientoCaja
            {
                Fecha = DateTime.Now,
                Tipo = "Ingreso",
                Concepto = $"Venta #{venta.Id} ({metododepago}): {articulosResumen}",
                Monto = venta.Total
            });

            // --- 3. CONSTRUIR EL TICKET ANTES DE LIMPIAR EL CARRITO ---
            string nombreCajero = SessionService.CurrentUser?.Username ?? "Desconocido";

            var gridTicket = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Producto
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Cantidad
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },  // Subtotal
          // Columna oculta para evitar que el texto se estire demasiado
        },
                RowSpacing = 4,
                Margin = new Thickness(0, 10, 0, 10)
            };

            // Encabezados del ticket
            gridTicket.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var headerProducto = new TextBlock { Text = "Producto", FontWeight = FontWeights.Bold };
            var headerCant = new TextBlock { Text = "Cant.", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right };
            var headerSubtotal = new TextBlock { Text = "Subtotal", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right };
            

            Grid.SetRow(headerProducto, 0); Grid.SetColumn(headerProducto, 0);
            Grid.SetRow(headerCant, 0); Grid.SetColumn(headerCant, 1);
            Grid.SetRow(headerSubtotal, 0); Grid.SetColumn(headerSubtotal, 2);

            gridTicket.Children.Add(headerProducto);
            gridTicket.Children.Add(headerCant);
            gridTicket.Children.Add(headerSubtotal);

            // Filas de productos en el ticket
            int row = 1;
            foreach (var d in carrito)
            {
                gridTicket.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var txtProducto = new TextBlock { Text = d.NombreProducto, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis };
                var txtCantidad = new TextBlock { Text = d.Cantidad.ToString(), HorizontalAlignment = HorizontalAlignment.Right };
                var txtSubtotal = new TextBlock { Text = $"${d.Subtotal}", HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetRow(txtProducto, row); Grid.SetColumn(txtProducto, 0);
                Grid.SetRow(txtCantidad, row); Grid.SetColumn(txtCantidad, 1);
                Grid.SetRow(txtSubtotal, row); Grid.SetColumn(txtSubtotal, 2);

                gridTicket.Children.Add(txtProducto);
                gridTicket.Children.Add(txtCantidad);
                gridTicket.Children.Add(txtSubtotal);

                row++;
            }

            // Fila del total en el ticket
            gridTicket.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // 1. Creamos el control con sus propiedades base
            var txtTotal = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = $"Total: ${venta.Total} | Pago: {venta.MetodoPago}"
            };

            // 2. Si necesitas lógica específica por método en el futuro, 
            // puedes modificar solo la propiedad necesaria aquí:
            if (metododepago == "Efectivo")
            {
                
                txtTotal.Text += $" | Cambio: {cambio}";
            }

            // 3. Configuración del Grid
            Grid.SetRow(txtTotal, row);
            Grid.SetColumnSpan(txtTotal, 3);
            gridTicket.Children.Add(txtTotal);

            // Contenedor principal del ticket
            var panelTicket = new StackPanel { Spacing = 5 };
            panelTicket.Children.Add(new TextBlock { Text = "¡Venta registrada correctamente!", FontWeight = FontWeights.Bold, FontSize = 16, Margin = new Thickness(0, 0, 0, 10), Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green) });
            panelTicket.Children.Add(new TextBlock { Text = $"Folio: {venta.Id} | Fecha: {venta.Fecha:dd/MM/yyyy HH:mm}" });
            panelTicket.Children.Add(new TextBlock { Text = $"Cajero: {nombreCajero}" });
            panelTicket.Children.Add(new MenuFlyoutSeparator { Margin = new Thickness(0, 5, 0, 5) });
            panelTicket.Children.Add(gridTicket);

            // --- 4. LIMPIAR LA INTERFAZ ---
            carrito.Clear();
            CodigoBox.Text = "";
            ActualizarTotal();

            // --- 5. MOSTRAR EL TICKET ---
            if (!_isDialogOpen)
            {
                _isDialogOpen = true;
                try
                {
                    var ticketDialog = new ContentDialog
                    {
                        Title = "Ticket de Venta",
                        Content = new ScrollViewer { Content = panelTicket, MaxHeight = 400, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
                        CloseButtonText = "Cerrar",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.Content.XamlRoot
                    };
                    await ticketDialog.ShowAsync();
                }
                catch { }
                finally { _isDialogOpen = false; }
            }
        }

        private async Task ProcesarPagoAnimacion()
        {
            var procesandoStack = new StackPanel { Spacing = 20, Padding = new Thickness(20) };

            var progressRing = new ProgressRing
            {
                IsActive = true,
                Width = 60,
                Height = 60,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var mensajeText = new TextBlock
            {
                Text = "Comunicando con terminal bancaria...",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            procesandoStack.Children.Add(progressRing);
            procesandoStack.Children.Add(mensajeText);

            var dialogProcesando = new ContentDialog
            {
                Title = "Procesando Pago",
                Content = procesandoStack,
                XamlRoot = this.Content.XamlRoot
                // No ponemos botones para que el usuario no pueda cancelarlo a mitad del proceso
            };

            // Mostramos el diálogo
            var mostrarTask = dialogProcesando.ShowAsync();

            // Esperamos los 5 segundos
            await Task.Delay(5000);

            // Cerramos el diálogo de carga manualmente para que el código siga
            dialogProcesando.Hide();
        }

        private void BuscarProductoBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = BuscarProductoBox.Text.ToLower();

            var filtrados = todosProductos
                .Where(p => (p.Nombre ?? "")
                .ToLower()
                .Contains(texto))
                .ToList();

            ResultadosProductosList.ItemsSource = filtrados;
        }

        private async void cargarCatalogo()
        {

            todosProductos = await _productoService.GetAllAsync() ?? new List<Producto>();

            //imprimir en consola para verificar que se cargaron los productos
            foreach (var item in todosProductos)
            {
                Console.WriteLine($"Producto: {item.Nombre}, Stock: {item.Stock}");
            }

            ResultadosProductosList.ItemsSource = todosProductos;


            ResultadosProductosList.ItemClick += async (sender, e) =>
            {
                var seleccionado = e.ClickedItem as Producto;
                if (seleccionado == null)
                    return;

                // Verificar stock
                if (seleccionado.Stock <= 0)
                {
                    await ShowDialogAsync("Error", "No hay stock de este producto");
                    return;
                }

                AgregarProductoAlCarrito(seleccionado);
            };

            // Asegurarse de que el GridView permita clicks
            ResultadosProductosList.IsItemClickEnabled = true;
        }

        private async void HistorialVentas_Click(object sender, RoutedEventArgs e)
        {
            var ventas = await _ventaService.GetAllAsync();
            var todosDetalles = await _detalleVentasService.GetAllAsync();

            var hoy = DateTime.Today;
            var ventasHoy = ventas
                .Where(v => v.Fecha.Date == hoy)
                .OrderByDescending(v => v.Fecha)
                .ToList();

            // 1. Aseguramos que el XamlRoot sea el de esta página
            var root = this.Content.XamlRoot;

            if (!ventasHoy.Any())
            {
                await new ContentDialog
                {
                    Title = "Ventas de Hoy",
                    Content = "No se han registrado ventas hoy.",
                    CloseButtonText = "Cerrar",
                    XamlRoot = root // Usamos la variable segura
                }.ShowAsync();
                return;
            }

            var stackVentas = new StackPanel { Spacing = 10, Margin = new Thickness(5) };

            foreach (var venta in ventasHoy)
            {
                var detallesDeVenta = todosDetalles
                    .Where(d => d.IdVenta == venta.Id)
                    .ToList();

                stackVentas.Children.Add(CrearVentaExpanderUI(venta, detallesDeVenta));
            }

            // 2. Aplicamos el mismo XamlRoot al diálogo principal
            var ventasDialog = new ContentDialog
            {
                Title = "Ventas de Hoy",
                CloseButtonText = "Cerrar",
                
                Content = new ScrollViewer
                {
                    Content = stackVentas,
                    Height = 400, // Reducido un poco para evitar que se salga de la pantalla en laptops
                    Width = 600, // Ajuste de ancho más estándar
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                XamlRoot = root // CRÍTICO: Referencia al XamlRoot de la página
            };

            await ventasDialog.ShowAsync();
        }

        private Expander CrearVentaExpanderUI(Venta venta, List<DetalleVentas> detalles)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }, // Producto
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Cantidad
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }  // Subtotal
        },
                RowSpacing = 2
            };

            // Encabezados
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var headerProducto = new TextBlock { Text = "Producto", FontWeight = FontWeights.Bold };
            var headerCant = new TextBlock { Text = "Cant.", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right };
            var headerSubtotal = new TextBlock { Text = "Subtotal", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right };

            Grid.SetRow(headerProducto, 0); Grid.SetColumn(headerProducto, 0);
            Grid.SetRow(headerCant, 0); Grid.SetColumn(headerCant, 1);
            Grid.SetRow(headerSubtotal, 0); Grid.SetColumn(headerSubtotal, 2);

            grid.Children.Add(headerProducto);
            grid.Children.Add(headerCant);
            grid.Children.Add(headerSubtotal);

            // Filas de detalle
            int row = 1;
            foreach (var d in detalles)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var txtProducto = new TextBlock { Text = d.NombreProducto, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis };
                var txtCantidad = new TextBlock { Text = d.Cantidad.ToString(), HorizontalAlignment = HorizontalAlignment.Right };
                var txtSubtotal = new TextBlock { Text = $"${d.Subtotal}", HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetRow(txtProducto, row); Grid.SetColumn(txtProducto, 0);
                Grid.SetRow(txtCantidad, row); Grid.SetColumn(txtCantidad, 1);
                Grid.SetRow(txtSubtotal, row); Grid.SetColumn(txtSubtotal, 2);

                grid.Children.Add(txtProducto);
                grid.Children.Add(txtCantidad);
                grid.Children.Add(txtSubtotal);

                row++;
            }

            // Fila total
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var txtTotal = new TextBlock
            {
                Text = $"Total: ${venta.Total} | Pago: {venta.MetodoPago} | Usuario: {venta.UserName}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(txtTotal, row);
            Grid.SetColumnSpan(txtTotal, 3);
            grid.Children.Add(txtTotal);

            // Expander con scroll interno si hay muchas filas
            return new Expander
            {
                Header = $"Folio: {venta.Id} | {venta.Fecha:HH:mm} | Total: ${venta.Total} | Usuario: {venta.UserName}",
                Content = new ScrollViewer
                {
                    Content = grid,
                    MaxHeight = 200,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                IsExpanded = false,
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        // ✨ NUEVOS MÉTODOS DE LA CAJA IMPLEMENTADOS AL FINAL ✨
        private void ActualizarUIBotonCaja()
        {
            if (CajaState.IsCajaAbierta)
            {
                TxtCaja.Text = "Cerrar Caja";
                BtnCaja.Background = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
                BtnCaja.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                IconoCaja.Glyph = "\uE10A"; // Icono de X o Cancelar
            }
            else
            {
                TxtCaja.Text = "Abrir Caja";
                BtnCaja.Background = new SolidColorBrush(Microsoft.UI.Colors.SeaGreen);
                BtnCaja.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                IconoCaja.Glyph = "\uE8C7"; // Icono de Caja
            }
        }

        private async void BtnCaja_Click(object sender, RoutedEventArgs e)
        {
            if (!CajaState.IsCajaAbierta)
            {
                // ✨ ABRIR CAJA (NUEVO CONTROL ESTRICTO DE 5 CARACTERES Y SOLO NÚMEROS) ✨
                var inputBox = new TextBox
                {
                    Header = "¿Con cuánto efectivo inicias la caja?",
                    PlaceholderText = "Fondo de caja (Ej. 500)",
                    MaxLength = 5, // Bloquea todo lo que exceda 5 dígitos
                    InputScope = new InputScope { Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } } }
                };

                // Bloqueamos cualquier tecla que no sea un número (letras, puntos, signos, etc.)
                inputBox.BeforeTextChanging += (s, args) =>
                {
                    args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
                };

                var dialog = new ContentDialog
                {
                    Title = "Abrir Caja",
                    Content = inputBox,
                    PrimaryButtonText = "Abrir",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    decimal fondoInicial = 0;
                    decimal.TryParse(inputBox.Text, out fondoInicial);

                    CajaState.IsCajaAbierta = true;
                    CajaState.HoraApertura = DateTime.Now;
                    CajaState.FondoInicial = fondoInicial;
                    CajaState.Movimientos.Clear(); // Limpiar movimientos anteriores al abrir
                    ActualizarUIBotonCaja();
                    await ShowDialogAsync("Éxito", $"Caja abierta exitosamente a las {CajaState.HoraApertura:HH:mm}.");
                }
            }
            else
            {
                // CERRAR CAJA Y MOSTRAR RESUMEN
                var todasLasVentas = await _ventaService.GetAllAsync() ?? new List<Venta>();

                // Ventas SOLO desde que se abrió la caja
                var ventasCaja = todasLasVentas.Where(v => v.Fecha >= CajaState.HoraApertura).ToList();

                decimal totalEfectivo = ventasCaja.Where(v => v.MetodoPago == "Efectivo").Sum(v => v.Total);
                decimal totalTarjeta = ventasCaja.Where(v => v.MetodoPago == "Tarjeta").Sum(v => v.Total);
                decimal totalVendido = totalEfectivo + totalTarjeta;

                // ✨ CALCULAR EGRESOS
                decimal totalEgresos = CajaState.Movimientos.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);

                // ✨ RESTAR EGRESOS DEL EFECTIVO ESPERADO
                decimal totalEnCajaEfectivo = (CajaState.FondoInicial + totalEfectivo) - totalEgresos;

                // ✨ CONSTRUIR LISTA DE MOVIMIENTOS PARA LA PANTALLA
                var detallesMovimientos = new StringBuilder();
                if (CajaState.Movimientos.Any())
                {
                    detallesMovimientos.AppendLine("\n\n--- DETALLE DE MOVIMIENTOS ---");
                    // Ordenamos cronológicamente
                    foreach (var mov in CajaState.Movimientos.OrderBy(m => m.Fecha))
                    {
                        string signo = mov.Tipo == "Ingreso" ? "+" : "-";
                        detallesMovimientos.AppendLine($"• {mov.Fecha:HH:mm} | {mov.Concepto} | {signo}${mov.Monto}");
                    }
                }

                var resumenTexto = $"Hora Apertura: {CajaState.HoraApertura:HH:mm}\n" +
                                   $"Hora Cierre: {DateTime.Now:HH:mm}\n\n" +
                                   $"Fondo Inicial: ${CajaState.FondoInicial}\n" +
                                   $"Ventas Efectivo: ${totalEfectivo}\n" +
                                   $"Ventas Tarjeta: ${totalTarjeta}\n" +
                                   $"Total Vendido: ${totalVendido}\n" +
                                   $"Egresos (Pedidos): -${totalEgresos}\n" +
                                   $"-----------------------------\n" +
                                   $"EFECTIVO ESPERADO EN CAJA: ${totalEnCajaEfectivo}" +
                                   detallesMovimientos.ToString();

                var dialog = new ContentDialog
                {
                    Title = "Resumen de Cierre de Caja",
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock { Text = resumenTexto, FontWeight = FontWeights.SemiBold },
                        MaxHeight = 400
                    },
                    PrimaryButtonText = "Cerrar Caja y Guardar",
                    SecondaryButtonText = "Exportar a Excel (CSV)",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot
                };

                var resultado = await dialog.ShowAsync();

                if (resultado == ContentDialogResult.Primary || resultado == ContentDialogResult.Secondary)
                {
                    if (resultado == ContentDialogResult.Secondary)
                    {
                        await ExportarVentasAExcel(ventasCaja, totalEfectivo, totalTarjeta, totalVendido, totalEgresos, totalEnCajaEfectivo);
                    }

                    // Reiniciar estado
                    CajaState.IsCajaAbierta = false;
                    CajaState.HoraApertura = null;
                    CajaState.FondoInicial = 0;
                    CajaState.Movimientos.Clear();
                    ActualizarUIBotonCaja();
                    await ShowDialogAsync("Caja Cerrada", "La caja se ha cerrado correctamente.");
                }
            }
        }

        private async Task ExportarVentasAExcel(List<Venta> ventas, decimal totalEf, decimal totalTarj, decimal totalVen, decimal totalEgresos, decimal efectivoEsperado)
        {
            try
            {
                var csv = new StringBuilder();
                csv.AppendLine("REPORTE DE CAJA");
                csv.AppendLine($"Fecha Apertura:,{CajaState.HoraApertura}");
                csv.AppendLine($"Fecha Cierre:,{DateTime.Now}");
                csv.AppendLine($"Fondo Inicial:,${CajaState.FondoInicial}");
                csv.AppendLine($"Ventas Efectivo:,${totalEf}");
                csv.AppendLine($"Ventas Tarjeta:,${totalTarj}");
                csv.AppendLine($"Total Vendido:,${totalVen}");
                csv.AppendLine($"Egresos (Pedidos):,-${totalEgresos}");
                csv.AppendLine($"EFECTIVO ESPERADO EN CAJA:,${efectivoEsperado}");
                csv.AppendLine();

                // ✨ IMPRIMIR CADA MOVIMIENTO / EGRESO EN EL EXCEL
                if (CajaState.Movimientos.Any())
                {
                    csv.AppendLine("MOVIMIENTOS DE CAJA (INGRESOS Y EGRESOS)");
                    csv.AppendLine("Fecha,Tipo,Concepto,Monto");
                    foreach (var mov in CajaState.Movimientos.OrderBy(m => m.Fecha))
                    {
                        string signo = mov.Tipo == "Ingreso" ? "" : "-";
                        // Se encierra el concepto entre comillas para que el Excel no se rompa si hay comas en los artículos
                        csv.AppendLine($"{mov.Fecha:yyyy-MM-dd HH:mm:ss},{mov.Tipo},\"{mov.Concepto}\",{signo}${mov.Monto}");
                    }
                    csv.AppendLine();
                }

                csv.AppendLine("VENTAS REALIZADAS");
                csv.AppendLine("Folio,Fecha,Cajero,Metodo de Pago,Total");
                foreach (var v in ventas)
                {
                    csv.AppendLine($"{v.Id},{v.Fecha:yyyy-MM-dd HH:mm:ss},{v.UserName},{v.MetodoPago},${v.Total}");
                }

                // Guardar en la carpeta Documentos del usuario
                string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fileName = $"CorteDeCaja_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string fullPath = Path.Combine(docsPath, fileName);

                await File.WriteAllTextAsync(fullPath, csv.ToString(), Encoding.UTF8);

                await ShowDialogAsync("Exportado", $"Resumen guardado exitosamente en tus Documentos como:\n{fileName}");
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Error al Exportar", ex.Message);
            }
        }
    }
}
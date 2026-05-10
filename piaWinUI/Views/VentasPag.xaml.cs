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
        public sealed partial class VentasPag : Page
        {
            private ProductService _productoService = new ProductService();
            private VentasService _ventaService = new VentasService();
        private List<Producto> todosProductos = new List<Producto>();
        private List<DetalleVentas> _detalleService = new List<DetalleVentas>();
        private ObservableCollection<DetalleVentas> carrito = new ObservableCollection<DetalleVentas>();

        private DispatcherTimer _timer;

        
        


       public VentasPag()
            {
                this.InitializeComponent();
           
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Cada segundo
            _timer.Tick += Timer_Tick;
            _timer.Start();
            ProductosList.ItemsSource = carrito;
            cargarCatalogo();
            ResultadosProductosList.ItemsSource = todosProductos;

        }
        private void Timer_Tick(object sender, object e)
        {
            // Actualiza el TextBlock con la hora actual
            var horaActual = DateTime.Now.ToString("HH:mm:ss"); // Formato 24h
            CajeroHoraTextBlock.Text = $"Cajero: Ana | {horaActual}";
        }
        private void BuscadorNombre_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {

            var regex = @"^(?!.*  )[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]*$";
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
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();

            }

            // 🔥 TOTAL
            private void ActualizarTotal()
            {
                var total = carrito.Sum(p => p.Subtotal);
                TotalText.Text = $" {total:C}";
            }
            private void limpiarbarrabusqueda() {
            CodigoBox.Text = "";
        }

        // Obtiene el id del usuario logueado desde LocalSettings (clave "UserId").
        // Si no existe devuelve 0.
        

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

            if (!int.TryParse(codigo, out int intBuscado))
            {
                await ShowDialogAsync("Error", "Código inválido");
                limpiarbarrabusqueda();
                return;
            }

            var producto = productos.FirstOrDefault(p => p.Id == intBuscado);

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
            //en caso de que por algun motivo el metodo de pago sea nulo o vacio un returnsillo


            var ventas = await _ventaService.GetAllAsync() ?? new List<Venta>();
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

            if (metododepago == "Tarjeta")
            {
                var confirm = new ContentDialog
                {
                    Title = "Confirmar Pago",
                    Content = $"Total a pagar: {TotalText.Text}\n¿Confirmar pago?",
                    PrimaryButtonText = "Sí",
                    CloseButtonText = "No",
                    XamlRoot = this.Content.XamlRoot
                };
                if (await confirm.ShowAsync() != ContentDialogResult.Primary)
                    return;
            }


            if (metododepago == "Efectivo")
            {
                // Dialogo de confirmación inicial
                var confirm = new ContentDialog
                {
                    Title = "Confirmar Pago",
                    Content = $"Total a pagar: {TotalText.Text}\n¿Confirmar pago?",
                    PrimaryButtonText = "Sí",
                    CloseButtonText = "No",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await confirm.ShowAsync() != ContentDialogResult.Primary)
                    return;

                // Crear dialogo para ingresar efectivo
                var inputBox = new TextBox
                {
                    Header = "Ingrese cantidad de efectivo",
                    PlaceholderText = "0",
                    InputScope = new InputScope
                    {
                        Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } }
                    }
                };

                var efectivoDialog = new ContentDialog
                {
                    Title = "Pago en Efectivo",
                    Content = inputBox,
                    PrimaryButtonText = "Aceptar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot
                };

               
                
                if (await efectivoDialog.ShowAsync() != ContentDialogResult.Primary)
                    return;

                // Validar cantidad ingresada
                if (!decimal.TryParse(inputBox.Text, out decimal efectivoEntregado))
                {
                    await new ContentDialog
                    {
                        Title = "Error",
                        Content = "Cantidad inválida",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    }.ShowAsync();
                    return;
                }

                decimal total = carrito.Sum(p => p.Subtotal); // asumiendo que TotalText.Text tiene solo el número
                if (efectivoEntregado < total)
                {
                    await new ContentDialog
                    {
                        Title = "Error",
                        Content = "El efectivo entregado es menor que el total",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    }.ShowAsync();
                    return;
                }

                decimal cambio = efectivoEntregado - total;

                await new ContentDialog
                {
                    Title = "Pago recibido",
                    Content = $"Efectivo entregado: ${efectivoEntregado}\nCambio: ${cambio}",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.Content.XamlRoot
                }.ShowAsync();
            }
            var venta = new Venta
                {
                    Id = ventas.Any() ? ventas.Max(v => v.Id) + 1 : 1,
                    IdUsuario = 1, // se agrega automáticamente el id del usuario logueado
                    MetodoPago = metododepago, // por ahora fijo, se puede mejorar con un selector
                Fecha = DateTime.Now,
                    Total = carrito.Sum(p => p.Subtotal)
                };

                foreach (var item in carrito)
                {
                    item.IdVenta = venta.Id;
                }

                
                ventas.Add(venta);
                await _ventaService.SaveAllAsync(ventas);
                

            var detalleService = new DetalleVentasService();
                var detalles = await detalleService.GetAllAsync() ?? new List<DetalleVentas>();
                detalles.AddRange(carrito);
                await detalleService.SaveAllAsync(detalles);
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
            carrito.Clear();
            CodigoBox.Text = "";

                ActualizarTotal();

                await ShowDialogAsync("Éxito", "Venta registrada correctamente"); 
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
            foreach (var item in todosProductos) {
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
            var ventas = await _ventaService.GetAllAsync(); // Todas las ventas
            var detalleService = new DetalleVentasService();
            var todosDetalles = await detalleService.GetAllAsync(); // Todos los detalles

            var hoy = DateTime.Today;
            var ventasHoy = ventas
                .Where(v => v.Fecha.Date == hoy)
                .OrderByDescending(v => v.Fecha)
                .ToList();

            if (!ventasHoy.Any())
            {
                await new ContentDialog
                {
                    Title = "Ventas de Hoy",
                    Content = "No se han registrado ventas hoy.",
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.XamlRoot
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

            var ventasDialog = new ContentDialog
            {
                Title = "Ventas de Hoy",
                CloseButtonText = "Cerrar",
                PrimaryButtonText = "Imprimir Copia",
                Content = new ScrollViewer
                {
                    Content = stackVentas,
                    Height = 600,
                    Width = 900,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                XamlRoot = this.XamlRoot
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
                Text = $"Total: ${venta.Total} | Pago: {venta.MetodoPago} | Usuario: {venta.IdUsuario}",
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
                Header = $"Folio: {venta.Id} | {venta.Fecha:HH:mm} | Total: ${venta.Total} | Usuario: {venta.IdUsuario}",
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





    } 
}
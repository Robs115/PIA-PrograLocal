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
            
            var regex = @"^[0-9a-fA-F-]*$";

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
        private int GetLoggedUserId()
        {
            try
            {
                var local = ApplicationData.Current.LocalSettings;
                if (local.Values.TryGetValue("UserId", out object? raw) && raw != null)
                {
                    if (int.TryParse(raw.ToString(), out int id))
                        return id;
                }
            }
            catch
            {
                // ignorar y devolver 0
            }
            return 0;
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

            // 🔥 GUARDAR VENTA
        private async void GuardarVenta_Click(object sender, RoutedEventArgs e)
            {
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

            var venta = new Venta
                {
                    Id = ventas.Any() ? ventas.Max(v => v.Id) + 1 : 1,
                    IdUsuario = GetLoggedUserId(), // se agrega automáticamente el id del usuario logueado

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
                .Where(p => p.Nombre.ToLower().Contains(texto))
                .ToList();

            ResultadosProductosList.ItemsSource = filtrados; 
        }
        private async void cargarCatalogo()
        {

            todosProductos = await _productoService.GetAllAsync();
            //imprimir en consola para verificar que se cargaron los productos
            foreach(var item in todosProductos) {
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

            var stackVentas = new StackPanel { Spacing = 10 };

            foreach (var venta in ventas)
            {
                // Filtrar detalles solo de esta venta
                var detallesDeVenta = todosDetalles
                                      .Where(d => d.IdVenta == venta.Id)
                                      .ToList();

                stackVentas.Children.Add(CrearVentaExpander(venta, detallesDeVenta));
            }

            ContentDialog ventasDialog = new ContentDialog
            {
                Title = "Ventas Recientes",
                CloseButtonText = "Cerrar",
                PrimaryButtonText = "Imprimir Copia",
                Content = new ScrollViewer
                {
                    Content = stackVentas,
                    Height = 400
                },
                XamlRoot = this.XamlRoot
            };

            await ventasDialog.ShowAsync();
        }

        private Expander CrearVentaExpander(Venta venta, List<DetalleVentas> detalles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DETALLE DE VENTA:");

            // Agregar cada detalle
            foreach (var d in detalles)
            {
                sb.AppendLine($"{d.Cantidad}x ProductoID {d.IdProducto} (${d.Subtotal})");
            }

            sb.AppendLine($"Total: ${venta.Total} | Pago: {venta.MetodoPago}");

            return new Expander
            {
                Header = $"Folio: {venta.Id} | {venta.Fecha:HH:mm} | Total: ${venta.Total}",
                Content = new TextBlock
                {
                    Text = sb.ToString(),
                    TextWrapping = TextWrapping.Wrap
                },
                IsExpanded = false
            };
        }







    } 
}
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace piaWinUI.Views
{
    public class ProductoView
    {
        public Producto Model { get; }

        public ProductoView(Producto model)
        {
            Model = model;
        }

        public string Nombre => Model.Nombre;
        public string Descripcion => Model.Descripcion;

        public string CodigoBarras => Model.CodigoBarras;
        public decimal PrecioCompra => Model.PrecioCompra;
        public decimal PrecioVenta => Model.PrecioVenta;
        public int IdProveedor => Model.IdProveedor;
        public string Categoria => Model.Categoria;
        public int Stock => Model.Stock;

        public string ProveedorNombre { get; set; }

        public string ImagenPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Model.ImagenPath))
                    return "";

                return Path.Combine(
                    AppContext.BaseDirectory,
                    Model.ImagenPath);
            }
        }
    }

    public sealed partial class ProductosPag : Page
    {
        private readonly ProductService _service = new ProductService();
        private readonly ProveedorService _proveedorService = new ProveedorService();
        private readonly DetalleVentasService _detalleVentasService = new DetalleVentasService();
        private readonly CategoriaService _categoriaService = new CategoriaService();

        public ObservableCollection<ProductoView> Productos { get; } = new();

        private Dictionary<int, string> _proveedores = new();

        // Esta es la lista que usan los diálogos de Producto
        private List<Categoria> _categorias = new();
        private List<ProductoView> _productosFuente = new();

        private string _ultimaColumnaOrdenada = "";
        private bool _ordenAscendente = true;

        public ProductosPag()
        {
            InitializeComponent();
            Loaded += OnLoaded;

            if (SessionService.CurrentUser?.IsAdmin != true)
            {
                var accionesColumn = ProductosDataGrid.Columns
                    .FirstOrDefault(c => c.Header?.ToString() == "Acciones");

                if (accionesColumn != null)
                    ProductosDataGrid.Columns.Remove(accionesColumn);

                CategoriaButton.Visibility = Visibility.Collapsed;
                ProductoButton.Visibility = Visibility.Collapsed;
            }
        }

        // =========================
        // INIT & REFRESH LOGIC
        // =========================

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Cargamos categorías primero para que los combos tengan datos
            await ActualizarFiltroCategorias();
            await CargarDatos();
        }

        private async Task CargarDatos()
        {
            var productos = await _service.GetAllAsync();
            var proveedores = await _proveedorService.GetAllAsync();

            _proveedores = proveedores.ToDictionary(p => p.Id, p => p.Nombre);

            _productosFuente = productos.Select(p => new ProductoView(p)
            {
                ProveedorNombre = _proveedores.TryGetValue(p.IdProveedor, out var nombre) ? nombre : "Desconocido"
            }).ToList();

            AplicarFiltros();
        }

        // MÉTODO CRÍTICO: Actualiza la lista interna y el combo de la UI
        private async Task ActualizarFiltroCategorias()
        {
            // 1. Refrescar la lista maestra que usan los diálogos de Producto
            _categorias = await _categoriaService.GetAllAsync() ?? new List<Categoria>();

            // 2. Crear la lista para el filtro visual (incluyendo "Todas")
            var listaConTodas = new List<Categoria> { new Categoria { Nombre = "Todas" } };
            listaConTodas.AddRange(_categorias);

            // 3. Asignar al ComboBox de la página
            ComboFiltroCategoria.ItemsSource = null;
            ComboFiltroCategoria.ItemsSource = listaConTodas;
            ComboFiltroCategoria.SelectedIndex = 0;
        }

        // =========================
        // VALIDACIONES
        // =========================

        private void SoloNumeros(TextBox tb, bool allowDecimal = false)
        {
            tb.BeforeTextChanging += (s, e) =>
            {
                if (e.NewText.Contains(" ")) { e.Cancel = true; return; }
                if (string.IsNullOrEmpty(e.NewText)) return;
                if (allowDecimal)
                    e.Cancel = !decimal.TryParse(e.NewText, out _);
                else
                    e.Cancel = !int.TryParse(e.NewText, out _);
            };
        }

        private void ValidarTexto(TextBox tb, bool permitirNumeros = true, bool permitirEspacios = true)
        {
            tb.BeforeTextChanging += (s, e) =>
            {
                string texto = e.NewText;
                if (string.IsNullOrEmpty(texto)) return;
                if (texto.StartsWith(" ")) { e.Cancel = true; return; }
                if (texto.Contains("  ")) { e.Cancel = true; return; }

                foreach (char c in texto)
                {
                    if (char.IsLetter(c)) continue;
                    if (permitirEspacios && c == ' ') continue;
                    if (permitirNumeros && char.IsDigit(c)) continue;
                    e.Cancel = true; return;
                }
            };
        }

        // =========================
        // FILTROS
        // =========================

        // Añade estas variables a nivel de tu clase (arriba de tus métodos)
        private bool _modificandoTexto = false;
        private readonly int MaxCaracteres = 50; // Cambia este número al límite que necesites

        private void OnFilterChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Solo validamos si el cambio lo hizo el usuario (tecleando o pegando texto)
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // Evitar un ciclo infinito cuando cambiamos el texto por código
                if (_modificandoTexto) return;

                string textoOriginal = sender.Text;
                string textoLimpio = textoOriginal;

                // 1. Limitar el número de caracteres
                if (textoLimpio.Length > MaxCaracteres)
                {
                    textoLimpio = textoLimpio.Substring(0, MaxCaracteres);
                }

                // 2. Solo permitir letras, números y espacios
                // El Regex remueve todo lo que NO (^) sea letra, número, espacio, acento o la 'ñ'
                textoLimpio = System.Text.RegularExpressions.Regex.Replace(textoLimpio, @"[^a-zA-Z0-9 áéíóúÁÉÍÓÚñÑ]", "");

                // 3. Reemplazar dos o más espacios consecutivos por un solo espacio
                textoLimpio = System.Text.RegularExpressions.Regex.Replace(textoLimpio, @" {2,}", " ");

                // Si el usuario introdujo algo inválido y tuvimos que limpiar el texto
                if (textoOriginal != textoLimpio)
                {
                    _modificandoTexto = true; // Bloqueamos temporalmente

                    sender.Text = textoLimpio; // Actualizamos el control con el texto limpio

                    _modificandoTexto = false; // Desbloqueamos

                    // Retornamos aquí. Al cambiar "sender.Text", este evento se volverá a 
                    // disparar automáticamente, pero esta vez con el texto limpio, 
                    // cayendo directamente al "AplicarFiltros()" de abajo.
                    return;
                }
            }

            // Si el texto es válido o el cambio vino por código, aplicamos los filtros
            AplicarFiltros();
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboFiltroCategoria?.ItemsSource != null)
                AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            string busqueda = SearchBox.Text.ToLower().Trim();
            var categoriaSeleccionada = ComboFiltroCategoria.SelectedItem as Categoria;

            var filtrados = _productosFuente.Where(p =>
            {
                bool coincideNombre = string.IsNullOrEmpty(busqueda) || p.Nombre.ToLower().Contains(busqueda);
                bool coincideCategoria = categoriaSeleccionada == null ||
                                         categoriaSeleccionada.Nombre == "Todas" ||
                                         p.Categoria == categoriaSeleccionada.Nombre;
                return coincideNombre && coincideCategoria;
            }).ToList();

            Productos.Clear();
            foreach (var p in filtrados) Productos.Add(p);
        }

        private void LimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            ComboFiltroCategoria.SelectedIndex = 0;
            AplicarFiltros();
        }

        // =========================
        // CATEGORÍAS
        // =========================

        private async void OpenCategoriaDialog(object sender, RoutedEventArgs e)
        {
            var nombre = new TextBox { Header = "Nombre de la Categoría", PlaceholderText = "Ej. Lácteos" };
            var error = new TextBlock { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red), Margin = new Thickness(0, 10, 0, 0) };
            var panel = new StackPanel { Spacing = 10, Children = { nombre, error } };

            var dialog = new ContentDialog
            {
                Title = "Nueva Categoría",
                Content = panel,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            dialog.PrimaryButtonClick += async (s, args) =>
            {
                var deferral = args.GetDeferral();
                try
                {
                    if (string.IsNullOrWhiteSpace(nombre.Text))
                    {
                        args.Cancel = true;
                        error.Text = "El nombre es obligatorio.";
                        return;
                    }
                    var nueva = new Categoria { Nombre = nombre.Text.Trim() };
                    await _categoriaService.AddCategoriaAsync(nueva);
                }
                catch (Exception ex) { args.Cancel = true; error.Text = ex.Message; }
                finally { deferral.Complete(); }
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // Refrescamos todo para que aparezca en el ComboBox de productos inmediatamente
                await ActualizarFiltroCategorias();
            }
        }

        // =========================
        // CRUD PRODUCTOS
        // =========================

        private async void OpenAddDialog(object sender, RoutedEventArgs e)
        {
            var lista = await _service.GetAllAsync();
            var producto = new Producto
            {
                Id = lista.Any() ? lista.Max(x => x.Id) + 1 : 1
            };

            var dialog = BuildDialog(producto, false);
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            lista.Add(producto);
            await _service.SaveAllAsync(lista);
            await CargarDatos();
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProductoView view) return;

            var dialog = BuildDialog(view.Model, true);
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var list = await _service.GetAllAsync();
            var index = list.FindIndex(x => x.Id == view.Model.Id);

            if (index != -1)
            {
                list[index] = view.Model;
                await _service.SaveAllAsync(list);
            }
            await CargarDatos();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProductoView view) return;

            var detalles = await _detalleVentasService.GetAllAsync();
            if (detalles.Any(d => d.IdProducto == view.Model.Id))
            {
                await new ContentDialog { Title = "Bloqueado", Content = "Producto ligado a ventas.", CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
                return;
            }

            var productos = await _service.GetAllAsync();
            var item = productos.FirstOrDefault(p => p.Id == view.Model.Id);

            if (item != null)
            {
                if (!string.IsNullOrWhiteSpace(item.ImagenPath))
                {
                    string rutaCompleta = Path.Combine(AppContext.BaseDirectory, item.ImagenPath);
                    if (File.Exists(rutaCompleta)) File.Delete(rutaCompleta);
                }
                productos.Remove(item);
                await _service.SaveAllAsync(productos);
            }
            Productos.Remove(view);
        }

        // =========================
        // DIALOG BUILDER (PRODUCTO)
        // =========================

        private ContentDialog BuildDialog(Producto producto, bool isEdit)
        {
            // 🚨 EL CHEQUEO DE NULOS DEBE ESTAR EN LA PRIMERA LÍNEA 🚨
            if (producto == null)
            {
                producto = new Producto();
            }

            var nombre = new TextBox
            {
                Header = "Nombre",
                Text = producto.Nombre ?? "",
                MaxLength = 50
            };
            ValidarTexto(nombre);

            var codigoBarras = new TextBox
            {
                Header = "Código de barras",
                Text = producto.CodigoBarras,
                MaxLength = 13
            };
            SoloNumeros(codigoBarras);

            var descripcion = new TextBox
            {
                Header = "Descripción",
                Text = producto.Descripcion ?? "",
                MaxLength = 100
            };
            ValidarTexto(descripcion);

            var imagenPath = new TextBox
            {
                Header = "Imagen",
                Text = producto.ImagenPath ?? "",
                IsReadOnly = true
            };

            var seleccionarImagen = new Button
            {
                Content = "Seleccionar imagen"
            };

            var categoria = new ComboBox
            {
                Header = "Categoría",
                ItemsSource = _categorias,
                DisplayMemberPath = "Nombre"
            };

            categoria.SelectedItem = _categorias.FirstOrDefault(c => c.Nombre == producto.Categoria);

            var precioCompra = new TextBox
            {
                Header = "Precio compra",
                PlaceholderText = "0.00",
                Text = producto.PrecioCompra == 0 ? string.Empty : producto.PrecioCompra.ToString(),
                MaxLength = 10
            };
            SoloNumeros(precioCompra, true);

            var precioVenta = new TextBox
            {
                Header = "Precio venta",
                PlaceholderText = "0.00",
                Text = producto.PrecioVenta == 0 ? string.Empty : producto.PrecioVenta.ToString(),
                MaxLength = 10
            };
            SoloNumeros(precioVenta, true);

            var stock = new TextBox
            {
                Header = "Stock",
                PlaceholderText = "0",
                Text = producto.Stock == 0 ? string.Empty : producto.Stock.ToString(),
                MaxLength = 6
            };
            SoloNumeros(stock);

            var proveedor = new ComboBox
            {
                Header = "Proveedor",
                ItemsSource = _proveedores.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList(),
                DisplayMemberPath = "Value",
                SelectedValuePath = "Key"
            };

            if (producto.IdProveedor > 0)
            {
                proveedor.SelectedValue = producto.IdProveedor;
            }

            var error = new TextBlock
            {
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                TextWrapping = TextWrapping.Wrap
            };

            seleccionarImagen.Click += async (s, e) =>
            {
                try
                {
                    var picker = new FileOpenPicker();
                    var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                    InitializeWithWindow.Initialize(picker, hwnd);

                    picker.FileTypeFilter.Add(".png");
                    picker.FileTypeFilter.Add(".jpg");
                    picker.FileTypeFilter.Add(".jpeg");
                    picker.FileTypeFilter.Add(".webp");

                    var file = await picker.PickSingleFileAsync();
                    if (file == null) return;

                    var info = new FileInfo(file.Path);
                    if (info.Length > 5_000_000)
                    {
                        error.Text = "La imagen pesa más de 5MB.";
                        return;
                    }

                    string carpetaImagenes = Path.Combine(AppContext.BaseDirectory, "Data", "Imagenes");
                    Directory.CreateDirectory(carpetaImagenes);

                    string extension = Path.GetExtension(file.Name);
                    string nuevoNombre = $"{producto.Id}{extension}";
                    string destino = Path.Combine(carpetaImagenes, nuevoNombre);

                    File.Copy(file.Path, destino, true);

                    producto.ImagenPath = Path.Combine("Data", "Imagenes", nuevoNombre);
                    imagenPath.Text = producto.ImagenPath;
                }
                catch (Exception ex)
                {
                    error.Text = ex.Message;
                }
            };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    nombre, descripcion, imagenPath, seleccionarImagen, codigoBarras,
                    categoria, precioCompra, precioVenta, stock, proveedor, error
                }
            };

            // 🔥 Envolver el StackPanel en un ScrollViewer
            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, -20, 0),
                Padding = new Thickness(0, 0, 20, 0)
            };

            var dialog = new ContentDialog
            {
                Title = isEdit ? "Editar producto" : "Nuevo producto",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                Content = scrollViewer,
                XamlRoot = this.XamlRoot
            };

            // ✨ LÓGICA DE VALIDACIÓN CON POPUP INCORPORADA AQUÍ ✨
            dialog.PrimaryButtonClick += async (s, args) =>
            {
                var deferral = args.GetDeferral();
                string mensajeError = null;
                decimal pc = 0;
                decimal pv = 0;
                int st = 0;

                if (string.IsNullOrWhiteSpace(nombre.Text.Trim()))
                    mensajeError = "Nombre obligatorio.";

                else if (string.IsNullOrWhiteSpace(codigoBarras.Text.Trim()))
                    mensajeError = "Código de barras obligatorio.";
                else if (string.IsNullOrWhiteSpace(descripcion.Text.Trim()))
                    mensajeError = "Descripción obligatoria.";
                else if (categoria.SelectedItem is null)
                    mensajeError = "Selecciona una categoría.";
                else if (!decimal.TryParse(precioCompra.Text, out pc) || pc <= 0)
                    mensajeError = "Precio compra inválido.";
                else if (!decimal.TryParse(precioVenta.Text, out pv) || pv <= 0)
                    mensajeError = "Precio venta inválido.";
                else if (!int.TryParse(stock.Text, out st) || st < 0)
                    mensajeError = "Stock inválido.";
                else if (pc >= pv)
                    mensajeError = "Venta debe ser mayor que compra.";
                else if (proveedor.SelectedValue is null)
                    mensajeError = "Selecciona proveedor.";

                if (mensajeError != null)
                {
                    args.Cancel = true;
                    deferral.Complete();

                    dialog.Hide();

                    var errorDialog = new ContentDialog
                    {
                        Title = "Error de Validación",
                        Content = mensajeError,
                        CloseButtonText = "Aceptar",
                        XamlRoot = dialog.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    //se vuelve a abrir pero como que sin los datos o idk, o a medio proceso y pues si le das guardar no guarda
                   // await dialog.ShowAsync();
                    return;
                }

                producto.Nombre = nombre.Text.Trim();
                producto.Descripcion = descripcion.Text.Trim();
                producto.CodigoBarras = codigoBarras.Text.Trim();
                producto.Categoria = (categoria.SelectedItem as Categoria)?.Nombre;
                producto.PrecioCompra = pc;
                producto.PrecioVenta = pv;
                producto.Stock = st;
                producto.IdProveedor = (int)proveedor.SelectedValue;
                producto.ImagenPath = imagenPath.Text;

                deferral.Complete();
            };

            return dialog;
        }
    }
}
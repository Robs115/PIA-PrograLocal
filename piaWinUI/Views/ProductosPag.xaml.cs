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

        public string CodigoBarras => Model.CodigoBarras;
        public string Descripcion => Model.Descripcion;
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
        private readonly ProductService _service =
            new ProductService();

        private readonly ProveedorService _proveedorService =
            new ProveedorService();

        private readonly DetalleVentasService _detalleVentasService =
            new DetalleVentasService();

        private readonly CategoriaService _categoriaService =
            new CategoriaService();

        public ObservableCollection<ProductoView> Productos { get; } =
            new();

        private Dictionary<int, string> _proveedores =
            new();

        private List<Categoria> _categorias =
            new();

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
        // INIT
        // =========================

        private async void OnLoaded(
            object sender,
            RoutedEventArgs e)
        {
            await CargarCategorias();

            await CargarDatos();
        }

        private async Task CargarCategorias()
        {
            _categorias =
                await _categoriaService.GetAllAsync()
                ?? new List<Categoria>();
        }

        // =========================
        // VALIDACIONES
        // =========================

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

        private void ValidarTexto(
            TextBox tb,
            bool permitirNumeros = true,
            bool permitirEspacios = true)
        {
            tb.BeforeTextChanging += (s, e) =>
            {
                string texto = e.NewText;

                if (string.IsNullOrEmpty(texto))
                    return;

                if (texto.StartsWith(" "))
                {
                    e.Cancel = true;
                    return;
                }

                if (texto.Contains("  "))
                {
                    e.Cancel = true;
                    return;
                }

                foreach (char c in texto)
                {
                    if (char.IsLetter(c))
                        continue;

                    if (permitirEspacios && c == ' ')
                        continue;

                    if (permitirNumeros && char.IsDigit(c))
                        continue;

                    e.Cancel = true;
                    return;
                }
            };
        }

        // =========================
        // DATA
        // =========================

        private async Task CargarDatos()
        {
            var productos = await _service.GetAllAsync();
            var proveedores = await _proveedorService.GetAllAsync();

            _proveedores = proveedores.ToDictionary(p => p.Id, p => p.Nombre);

            // Guardamos en la lista maestra
            _productosFuente = productos.Select(p => new ProductoView(p)
            {
                ProveedorNombre = _proveedores.TryGetValue(p.IdProveedor, out var nombre) ? nombre : "Desconocido"
            }).ToList();

            // Llenar el ComboBox de filtros (si no se ha hecho)
            ActualizarComboFiltro();

            // Aplicar el filtro inicial (que mostrará todos)
            AplicarFiltros();
        }

        private void ActualizarComboFiltro()
        {
            var listaConTodas = new List<Categoria> { new Categoria { Nombre = "Todas" } };
            listaConTodas.AddRange(_categorias);
            ComboFiltroCategoria.ItemsSource = listaConTodas;
            ComboFiltroCategoria.SelectedIndex = 0;
        }
        private void OnFilterChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            AplicarFiltros();
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // Solo aplicamos el filtro si el evento no se disparó 
            // mientras el combo se estaba llenando inicialmente
            if (ComboFiltroCategoria?.ItemsSource != null)
            {
                AplicarFiltros();
            }
        }

        private void AplicarFiltros()
        {
            string busqueda = SearchBox.Text.ToLower().Trim();
            var categoriaSeleccionada = ComboFiltroCategoria.SelectedItem as Categoria;

            // Realizar el filtrado sobre la lista maestra usando LINQ
            var filtrados = _productosFuente.Where(p =>
            {
                // Filtro de texto
                bool coincideNombre = string.IsNullOrEmpty(busqueda) ||
                                      p.Nombre.ToLower().Contains(busqueda);

                // Filtro de categoría
                bool coincideCategoria = categoriaSeleccionada == null ||
                                         categoriaSeleccionada.Nombre == "Todas" ||
                                         p.Categoria == categoriaSeleccionada.Nombre;

                return coincideNombre && coincideCategoria;
            }).ToList();

            // Actualizar la colección observable de la UI
            Productos.Clear();
            foreach (var p in filtrados)
            {
                Productos.Add(p);
            }
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

        // 1. Modifica tu método de agregar categoría
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
                var deferral = args.GetDeferral(); // Necesario para procesos async dentro del click
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
                catch (Exception ex)
                {
                    args.Cancel = true;
                    error.Text = ex.Message;
                }
                finally
                {
                    deferral.Complete();
                }
            };

            var result = await dialog.ShowAsync();

            // 🔥 LA CLAVE: Si se guardó, refrescamos el filtro de la interfaz
            if (result == ContentDialogResult.Primary)
            {
                await ActualizarFiltroCategorias();
            }
        }

        // 2. Agrega este nuevo método para refrescar el ComboBox del filtro
        private async Task ActualizarFiltroCategorias()
        {
            var listaCategorias = await _categoriaService.GetAllAsync();

            // Agregamos la opción "Todos" en la primera posición (índice 0)
            // El Id = 0 nos ayudará a identificar que no es una categoría real
            listaCategorias.Insert(0, new Categoria { Nombre = "Todos" });

            // Asumimos que tu ComboBox se llama CategoriaFiltro
            ComboFiltroCategoria.ItemsSource = null;
            ComboFiltroCategoria.ItemsSource = listaCategorias;

            // Seleccionamos "Todos" por defecto al cargar
            ComboFiltroCategoria.SelectedIndex = 0;
        }

        private void LimpiarFiltro_Click(object sender, RoutedEventArgs e)
        {
            // Esto seleccionará automáticamente "Todos"
            ComboFiltroCategoria.SelectedIndex = 0;

            // Y luego vuelves a cargar tu lista de productos original sin filtros
            // CargarProductos(); 
        }
        // =========================
        // ADD
        // =========================

        private async void OpenAddDialog(
            object sender,
            RoutedEventArgs e)
        {
            var lista =
                await _service.GetAllAsync();

            var producto = new Producto
            {
                Id = lista.Any()
                    ? lista.Max(x => x.Id) + 1
                    : 1
            };

            var dialog =
                BuildDialog(producto, false);

            if (await dialog.ShowAsync()
                != ContentDialogResult.Primary)
                return;

            lista.Add(producto);

            await _service.SaveAllAsync(lista);

            await CargarDatos();
        }

        // =========================
        // EDIT
        // =========================

        private async void Edit_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button btn
                || btn.Tag is not ProductoView view)
                return;

            var dialog =
                BuildDialog(view.Model, true);

            if (await dialog.ShowAsync()
                != ContentDialogResult.Primary)
                return;

            var list =
                await _service.GetAllAsync();

            var index =
                list.FindIndex(x =>
                    x.Id == view.Model.Id);

            if (index != -1)
            {
                list[index] = view.Model;

                await _service.SaveAllAsync(list);
            }

            await CargarDatos();
        }

        // =========================
        // DELETE
        // =========================

        private async void Delete_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button btn
                || btn.Tag is not ProductoView view)
                return;

            var detalles =
                await _detalleVentasService.GetAllAsync();

            if (detalles.Any(d =>
                d.IdProducto == view.Model.Id))
            {
                await new ContentDialog
                {
                    Title = "Bloqueado",
                    Content = "Producto ligado a ventas.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            var productos =
                await _service.GetAllAsync();

            var item =
                productos.FirstOrDefault(p =>
                    p.Id == view.Model.Id);

            if (item != null)
            {
                if (!string.IsNullOrWhiteSpace(
                    item.ImagenPath))
                {
                    string rutaCompleta =
                        Path.Combine(
                            AppContext.BaseDirectory,
                            item.ImagenPath);

                    if (File.Exists(rutaCompleta))
                    {
                        File.Delete(rutaCompleta);
                    }
                }

                productos.Remove(item);

                await _service.SaveAllAsync(
                    productos);
            }

            Productos.Remove(view);
        }

        // =========================
        // DIALOG
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

            // 🔥 NUEVO: Envolver el StackPanel en un ScrollViewer
            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                // Ajustamos los márgenes para que la barra de desplazamiento no se encime en el contenido
                Margin = new Thickness(0, 0, -20, 0),
                Padding = new Thickness(0, 0, 20, 0)
            };

            var dialog = new ContentDialog
            {
                Title = isEdit ? "Editar producto" : "Nuevo producto",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                Content = scrollViewer, // 🔥 Asignar el ScrollViewer en lugar del panel
                XamlRoot = this.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, e) =>
            {
                error.Text = "";

                if (string.IsNullOrWhiteSpace(nombre.Text.Trim()))
                {
                    e.Cancel = true;
                    error.Text = "Nombre obligatorio.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(codigoBarras.Text.Trim()))
                {
                    e.Cancel = true;
                    error.Text = "Código de barras obligatorio.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(descripcion.Text.Trim()))
                {
                    e.Cancel = true;
                    error.Text = "Descripción obligatoria.";
                    return;
                }

                if (categoria.SelectedItem is null)
                {
                    e.Cancel = true;
                    error.Text = "Selecciona una categoría.";
                    return;
                }

                if (!decimal.TryParse(precioCompra.Text, out decimal pc) || pc <= 0)
                {
                    e.Cancel = true;
                    error.Text = "Precio compra inválido.";
                    return;
                }

                if (!decimal.TryParse(precioVenta.Text, out decimal pv) || pv <= 0)
                {
                    e.Cancel = true;
                    error.Text = "Precio venta inválido.";
                    return;
                }

                if (!int.TryParse(stock.Text, out int st) || st < 0)
                {
                    e.Cancel = true;
                    error.Text = "Stock inválido.";
                    return;
                }

                if (pc >= pv)
                {
                    e.Cancel = true;
                    error.Text = "Venta debe ser mayor que compra.";
                    return;
                }

                if (proveedor.SelectedValue is null)
                {
                    e.Cancel = true;
                    error.Text = "Selecciona proveedor.";
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
            };

            return dialog;
        }


        private async Task<ContentDialog> OpenProductoDialog(Producto producto)
        {
            var nombre = new TextBox { Header = "Nombre", Text = producto.Nombre ?? "" };
            var codigoBarras = new TextBox { Header = "Código de barras", Text = producto.CodigoBarras ?? "" };
            var descripcion = new TextBox { Header = "Descripción", Text = producto.Descripcion ?? "", AcceptsReturn = true };
            var precioCompra = new NumberBox { Header = "Precio Compra", Value = (double)producto.PrecioCompra, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
            var precioVenta = new NumberBox { Header = "Precio Venta", Value = (double)producto.PrecioVenta, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
            var stock = new NumberBox { Header = "Stock", Value = producto.Stock, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };

            // ComboBox para Proveedor
            var proveedores = await _proveedorService.GetAllAsync();
            var proveedor = new ComboBox
            {
                Header = "Proveedor",
                ItemsSource = proveedores,
                DisplayMemberPath = "Nombre",
                SelectedValuePath = "Id",
                SelectedValue = producto.IdProveedor == 0 ? null : (object)producto.IdProveedor,
                HorizontalAlignment = HorizontalAlignment.Stretch
            }; 

            var error = new TextBlock { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red), Margin = new Thickness(0, 10, 0, 0) };

            var panel = new StackPanel { Spacing = 10, Children = { nombre, descripcion, precioCompra, codigoBarras, precioVenta, stock, proveedor, error } };

            // 🔥 NUEVO: Envolver el StackPanel en un ScrollViewer
            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, -20, 0),
                Padding = new Thickness(0, 0, 20, 0)
            };

            var dialog = new ContentDialog
            {
                Title = producto.Id == 0 ? "Nuevo Producto" : "Editar Producto",
                Content = scrollViewer, // 🔥 Asignar el ScrollViewer en lugar del panel
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot // CRÍTICO: Sin esto el programa no sabe dónde mostrar el diálogo
            };

            dialog.PrimaryButtonClick += (s, e) =>
            {
                decimal pc = (decimal)precioCompra.Value;
                decimal pv = (decimal)precioVenta.Value;
                int st = (int)stock.Value;


                if (string.IsNullOrWhiteSpace(nombre.Text))
                {
                    e.Cancel = true;
                    error.Text = "El nombre es obligatorio.";
                    return;
                }

                if (pc >= pv)
                {
                    e.Cancel = true;
                    error.Text = "El precio de venta debe ser mayor al de compra.";
                    return;
                }

                // Asignamos los valores de los controles al objeto producto
                producto.Nombre = nombre.Text.Trim();
                producto.CodigoBarras = codigoBarras.Text.Trim();
                producto.Descripcion = descripcion.Text.Trim();
                producto.PrecioCompra = pc;
                producto.PrecioVenta = pv;
                producto.Stock = st;
                producto.IdProveedor = (int)(proveedor.SelectedValue ?? 0);
            };

            return dialog;
        }


    }
}
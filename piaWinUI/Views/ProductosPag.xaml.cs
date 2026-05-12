using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using piaWinUI.Models;
using piaWinUI.Services;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace piaWinUI.Views
{
    public sealed partial class ProductosPag : Page
    {
        private readonly ProductService _service = new();

        private readonly ObservableCollection<Producto> _productosView = new();

        private List<Producto> _productos = new();
        private List<Categoria> _categoriasMemoria = new();
        private List<Proveedor> _proveedoresMemoria = new();
        private readonly ProveedorService _provService = new();

        private readonly CategoriaService _catService = new();
        // Asumiendo que tienes un ProveedorService similar al de categorías
        // private readonly ProveedorService _provService = new();

        public ProductosPag()
        {
            InitializeComponent();

            Loaded += ProductosPag_Loaded;
        }

        private async void ProductosPag_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await CargarDatos();
        }

        private async Task CargarDatos()
        {
            _productos = await _service.GetAllAsync();
            _proveedoresMemoria = await _provService.GetAllAsync();
            _categoriasMemoria = await _catService.GetAllAsync();
            ProductosGrid.ItemsSource = _productosView;

            ActualizarVista();

            CargarCategorias();
        }

        private void CargarCategorias()
        {
            var categorias = _productos
                .Select(p => p.Categoria)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            categorias.Insert(0, "Todas");

            CategoriaBox.ItemsSource = categorias;

            CategoriaBox.SelectedIndex = 0;
        }

        private void ActualizarVista()
        {
            string texto =
                SearchBox.Text?.Trim() ?? "";

            string categoria =
                CategoriaBox.SelectedItem?.ToString();

            IEnumerable<Producto> query = _productos;

            if (!string.IsNullOrWhiteSpace(texto))
            {
                query = query.Where(p =>
                    p.Nombre?.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase) == true);
            }

            if (!string.IsNullOrWhiteSpace(categoria) &&
                categoria != "Todas")
            {
                query = query.Where(p =>
                    p.Categoria == categoria);
            }

            var resultado = query.ToList();

            _productosView.Clear();

            foreach (var item in resultado)
            {
                _productosView.Add(item);
            }
        }


        private void SearchBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            string nuevoTexto = args.NewText;

   
            if (nuevoTexto.Length > 50)
            {
                args.Cancel = true;
                return;
            }

  
            bool valido = Regex.IsMatch(
                nuevoTexto,
                @"^(?!.* {3,})[a-zA-ZáéíóúÁÉÍÓÚñÑ0-9 ]*$"
            );

            if (!valido)
            {
                args.Cancel = true;
            }
        }
        private void SearchBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ActualizarVista();
        }

        private void CategoriaBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            ActualizarVista();
        }

        private void Limpiar_Click(
            object sender,
            RoutedEventArgs e)
        {
            SearchBox.Text = "";

            CategoriaBox.SelectedIndex = 0;
        }

        private async void Delete_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button btn ||
                btn.Tag is not Producto producto)
                return;

            _productos.Remove(producto);

            ActualizarVista();

            await _service.SaveAllAsync(_productos);
        }

        private async void OpenAddDialog(object sender, RoutedEventArgs e)
        {
            // 1. Preparar datos iniciales
            var listaCategorias = _categoriasMemoria.Select(c => c.Nombre).ToList();
            if (!listaCategorias.Any()) listaCategorias.Add("General");

            // 2. Crear el InfoBar (Aviso visual estable)
            var avisoError = new InfoBar
            {
                Severity = InfoBarSeverity.Error,
                Title = "Validación",
                IsOpen = false,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 3. Definición de controles de entrada
            var nombre = new TextBox { Header = "Nombre del Producto", MaxLength = 20 };
            var descripcion = new TextBox
            {
                Header = "Descripción",
                PlaceholderText = "Breve detalle...",
                AcceptsReturn = true,
                Height = 70,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                MaxLength = 50
            };

            var codigoBarras = new TextBox { Header = "Código de Barras", PlaceholderText = "Solo números", MaxLength = 9 };

            var categoriaCombo = new ComboBox
            {
                Header = "Categoría",
                ItemsSource = listaCategorias,
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
            };

            var proveedorCombo = new ComboBox
            {
                Header = "Proveedor",
                ItemsSource = _proveedoresMemoria.Select(p => p.Nombre).ToList(),
                PlaceholderText = "Selecciona un proveedor",
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
            };

            // Nota: Se mantiene MaxLength según tu snippet original
            var compra = new TextBox { Header = "Precio Compra", MaxLength = 3, PlaceholderText = "0" };
            var venta = new TextBox { Header = "Precio Venta", MaxLength = 3, PlaceholderText = "0" };
            var stock = new TextBox { Header = "Stock Inicial", MaxLength = 3, PlaceholderText = "0" };

            // --- VALIDACIONES EN TIEMPO REAL ---
            nombre.TextChanging += (s, args) => {
                string limpio = new string(nombre.Text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
                if (nombre.Text != limpio)
                {
                    int cursor = nombre.SelectionStart;
                    nombre.Text = limpio;
                    nombre.SelectionStart = Math.Max(0, cursor - 1);
                }
            };

            codigoBarras.TextChanging += (s, args) => {
                string limpio = new string(codigoBarras.Text.Where(char.IsDigit).ToArray());
                if (codigoBarras.Text != limpio)
                {
                    int cursor = codigoBarras.SelectionStart;
                    codigoBarras.Text = limpio;
                    codigoBarras.SelectionStart = Math.Max(0, cursor - 1);
                }
            };

            stock.TextChanging += (s, args) => {
                string limpio = new string(stock.Text.Where(char.IsDigit).ToArray());
                if (stock.Text != limpio)
                {
                    int cursor = stock.SelectionStart;
                    stock.Text = limpio;
                    stock.SelectionStart = Math.Max(0, cursor - 1);
                }
            };

            compra.TextChanging += ValidarDecimal;
            venta.TextChanging += ValidarDecimal;

            // --- GESTIÓN DE IMAGEN ---
            string rutaImagenSeleccionada = "";
            var txtImagen = new TextBlock { Text = "Sin imagen seleccionada", TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.6 };
            var btnImagen = new Button { Content = "Seleccionar Imagen", Margin = new Thickness(0, 5, 0, 0) };

            btnImagen.Click += async (s, args) => {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".png");
                var file = await picker.PickSingleFileAsync();
                if (file != null) { rutaImagenSeleccionada = file.Path; txtImagen.Text = file.Name; }
            };

            // 4. Organización del Layout
            var panel = new StackPanel
            {
                Spacing = 12,
                Width = 440,
                MinHeight = 520,
                Children = {
            avisoError,
            nombre, descripcion, codigoBarras, categoriaCombo, proveedorCombo,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { compra, venta, stock } },
            new StackPanel { Children = { new TextBlock { Text = "Imagen", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }, btnImagen, txtImagen } }
        }
            };

            var scroll = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 600 };

            var dialog = new ContentDialog
            {
                Title = "Nuevo Producto",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                Content = scroll,
                XamlRoot = this.XamlRoot
            };

            // 5. Lógica de Validación Final ESPECÍFICA
            dialog.Closing += (s, args) =>
            {
                if (args.Result == ContentDialogResult.Primary)
                {
                    args.Cancel = true; // Detenemos el cierre para validar
                    string error = "";

                    bool nV = string.IsNullOrWhiteSpace(nombre.Text);
                    bool dV = string.IsNullOrWhiteSpace(descripcion.Text);
                    bool cV = string.IsNullOrWhiteSpace(codigoBarras.Text);
                    bool catV = categoriaCombo.SelectedItem == null;
                    bool provV = proveedorCombo.SelectedItem == null;
                    bool coV = string.IsNullOrWhiteSpace(compra.Text);
                    bool veV = string.IsNullOrWhiteSpace(venta.Text);
                    bool stV = string.IsNullOrWhiteSpace(stock.Text);

                    if (nV && dV && cV && catV && provV && coV && veV && stV)
                        error = "Por favor, completa todos los campos del formulario.";
                    else if (nV) error = "Falta el nombre del producto.";
                    else if (dV) error = "Falta la descripción del producto.";
                    else if (cV) error = "Falta el código de barras.";
                    else if (catV) error = "Debes seleccionar una categoría.";
                    else if (provV) error = "Debes seleccionar un proveedor.";
                    else if (coV) error = "Falta el precio de compra.";
                    else if (veV) error = "Falta el precio de venta.";
                    else if (stV) error = "Falta el stock inicial.";
                    else
                    {
                        decimal.TryParse(compra.Text, out decimal pc);
                        decimal.TryParse(venta.Text, out decimal pv);
                        int.TryParse(stock.Text, out int st);

                        if (pc <= 0) error = "El precio de compra debe ser mayor a 0.";
                        else if (pv <= 0) error = "El precio de venta debe ser mayor a 0.";
                        // --- NUEVA REGLA: PRECIO VENTA > PRECIO COMPRA ---
                        else if (pv <= pc) error = "El precio de venta debe ser mayor al precio de compra.";
                        else if (st <= 0) error = "El stock inicial debe ser mayor a 0.";
                        else
                        {
                            string nbLimpio = nombre.Text.Trim();
                            string cbTexto = codigoBarras.Text.Trim();
                            bool existe = _productos.Any(p => nbLimpio.Equals(p.Nombre.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                          (!string.IsNullOrEmpty(p.CodigoBarras) && p.CodigoBarras == cbTexto));

                            if (existe)
                                error = "El nombre o código de barras ya están registrados.";
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        avisoError.Message = error;
                        avisoError.IsOpen = true;
                    }
                    else
                    {
                        args.Cancel = false;
                    }
                }
            };

            // 6. Ejecución y Persistencia
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                decimal.TryParse(compra.Text, out decimal pcFinal);
                decimal.TryParse(venta.Text, out decimal pvFinal);
                int.TryParse(stock.Text, out int stFinal);

                var provObj = _proveedoresMemoria.FirstOrDefault(p => p.Nombre == proveedorCombo.SelectedItem.ToString());

                var nuevo = new Producto
                {
                    Id = _productos.Any() ? _productos.Max(x => x.Id) + 1 : 1,
                    Nombre = nombre.Text.Trim(),
                    Descripcion = descripcion.Text.Trim(),
                    CodigoBarras = codigoBarras.Text.Trim(),
                    Categoria = categoriaCombo.SelectedItem.ToString(),
                    IdProveedor = provObj?.Id ?? 0,
                    PrecioCompra = pcFinal,
                    PrecioVenta = pvFinal,
                    Stock = stFinal,
                    ImagenPath = rutaImagenSeleccionada
                };

                _productos.Add(nuevo);
                ActualizarVista();
                await _service.SaveAllAsync(_productos);
                CargarCategorias();
            }
        }

        void ValidarDecimal(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            // Permite números y el punto decimal
            string limpio = new string(sender.Text.Where(c => char.IsDigit(c) || c == '.').ToArray());

            // Evita que pongan más de un punto (ej. "9.9.9")
            if (limpio.Count(c => c == '.') > 1)
            {
                limpio = limpio.Remove(limpio.LastIndexOf('.'), 1);
            }

            if (sender.Text != limpio)
            {
                int cursor = sender.SelectionStart;
                sender.Text = limpio;
                sender.SelectionStart = Math.Max(0, cursor - 1);
            }
        }

        private async void OpenAddCategoryDialog(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Crear el TextBox
                var nombreCat = new TextBox
                {
                    Header = "Nombre de la Nueva Categoría",
                    PlaceholderText = "Ej: Lácteos",
                    MaxLength = 20
                };

                // Validación en tiempo real: Solo letras y espacios
                nombreCat.TextChanging += (s, args) =>
                {
                    string limpio = new string(nombreCat.Text.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
                    if (nombreCat.Text != limpio)
                    {
                        int cursor = nombreCat.SelectionStart;
                        nombreCat.Text = limpio;
                        nombreCat.SelectionStart = Math.Max(0, cursor - 1);
                    }
                };

                // 2. Configurar el Diálogo
                var dialog = new ContentDialog
                {
                    Title = "Nueva Categoría",
                    PrimaryButtonText = "Guardar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = nombreCat,
                    XamlRoot = this.XamlRoot // VITAL: Sin esto WinUI 3 crashea
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    string nuevaCatNombre = nombreCat.Text.Trim();

                    if (string.IsNullOrWhiteSpace(nuevaCatNombre)) return;

                    // Validar duplicados en la lista de memoria
                    bool existe = _categoriasMemoria.Any(c => c.Nombre.Equals(nuevaCatNombre, StringComparison.OrdinalIgnoreCase));

                    if (existe)
                    {
                        // En lugar de otro diálogo (que puede causar crash), podrías usar un aviso visual 
                        // o simplemente ignorar la acción para mantenerlo simple.
                        await MostrarAvisoError("Categoría existente", $"La categoría '{nuevaCatNombre}' ya está registrada.");
                        return;
                    }

                    // 3. Guardar
                    var nuevaCat = new Categoria { Nombre = nuevaCatNombre };

                    // Guardar en el archivo JSON
                    await _catService.AddCategoriaAsync(nuevaCat);

                    // Actualizar la lista en RAM
                    _categoriasMemoria.Add(nuevaCat);

                    // Refrescar los filtros de la página
                    CargarCategorias();
                }
            }
            catch (Exception ex)
            {
                // Si hay un error, lo enviamos a la consola de depuración en lugar de cerrar la app
                System.Diagnostics.Debug.WriteLine($"Error al agregar categoría: {ex.Message}");
            }
        }

        private async Task MostrarAvisoError(string titulo, string mensaje)
        {
            var errorDialog = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                CloseButtonText = "Entendido",
                XamlRoot = this.XamlRoot
            };

            await errorDialog.ShowAsync();
        }


        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validación de entrada
                if (sender is not Button btn || btn.Tag is not Producto producto)
                    return;

                // 2. Preparar listas de memoria
                var listaCategorias = _categoriasMemoria.Select(c => c.Nombre).ToList();
                if (!listaCategorias.Any()) listaCategorias.Add("General");

                // 3. Crear el InfoBar (Aviso visual estable)
                var avisoError = new InfoBar
                {
                    Severity = InfoBarSeverity.Error,
                    Title = "Validación",
                    IsOpen = false,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                // 4. Inicializar Controles con los datos actuales del producto
                var nombre = new TextBox { Header = "Nombre del Producto", MaxLength = 20, Text = producto.Nombre ?? "" };

                var descripcion = new TextBox
                {
                    Header = "Descripción",
                    Text = producto.Descripcion ?? "",
                    AcceptsReturn = true,
                    Height = 70,
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                    MaxLength = 50
                };

                var codigoBarras = new TextBox
                {
                    Header = "Código de Barras",
                    Text = producto.CodigoBarras ?? "",
                    MaxLength = 9
                };

                var categoriaCombo = new ComboBox
                {
                    Header = "Categoría",
                    ItemsSource = listaCategorias,
                    SelectedItem = producto.Categoria,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
                };

                var proveedorCombo = new ComboBox
                {
                    Header = "Proveedor",
                    ItemsSource = _proveedoresMemoria.Select(p => p.Nombre).ToList(),
                    SelectedItem = _proveedoresMemoria.FirstOrDefault(p => p.Id == producto.IdProveedor)?.Nombre,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
                };

                var compra = new TextBox { Header = "Precio Compra", MaxLength = 6, Text = producto.PrecioCompra.ToString() };
                var venta = new TextBox { Header = "Precio Venta", MaxLength = 6, Text = producto.PrecioVenta.ToString() };
                var stock = new TextBox { Header = "Stock Inicial", MaxLength = 3, Text = producto.Stock.ToString() };

                // --- VALIDACIONES EN TIEMPO REAL ---
                nombre.TextChanging += (s, args) => {
                    string limpio = new string(nombre.Text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
                    if (nombre.Text != limpio)
                    {
                        int cursor = nombre.SelectionStart;
                        nombre.Text = limpio;
                        nombre.SelectionStart = Math.Max(0, cursor - 1);
                    }
                };

                codigoBarras.TextChanging += (s, args) => {
                    string limpio = new string(codigoBarras.Text.Where(char.IsDigit).ToArray());
                    if (codigoBarras.Text != limpio)
                    {
                        int cursor = codigoBarras.SelectionStart;
                        codigoBarras.Text = limpio;
                        codigoBarras.SelectionStart = Math.Max(0, cursor - 1);
                    }
                };

                stock.TextChanging += (s, args) => {
                    string limpio = new string(stock.Text.Where(char.IsDigit).ToArray());
                    if (stock.Text != limpio)
                    {
                        int cursor = stock.SelectionStart;
                        stock.Text = limpio;
                        stock.SelectionStart = Math.Max(0, cursor - 1);
                    }
                };

                compra.TextChanging += ValidarDecimal;
                venta.TextChanging += ValidarDecimal;

                // --- GESTIÓN DE IMAGEN ---
                string rutaImagenSeleccionada = producto.ImagenPath;
                var txtImagen = new TextBlock
                {
                    Text = string.IsNullOrEmpty(producto.ImagenPath) ? "Sin imagen seleccionada" : System.IO.Path.GetFileName(producto.ImagenPath),
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                    FontSize = 12,
                    Opacity = 0.6
                };
                var btnImagen = new Button { Content = "Cambiar Imagen", Margin = new Thickness(0, 5, 0, 0) };

                btnImagen.Click += async (s, args) => {
                    var picker = new Windows.Storage.Pickers.FileOpenPicker();
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                    picker.FileTypeFilter.Add(".jpg");
                    picker.FileTypeFilter.Add(".png");
                    var file = await picker.PickSingleFileAsync();
                    if (file != null) { rutaImagenSeleccionada = file.Path; txtImagen.Text = file.Name; }
                };

                // 5. Organización del Layout (Estable y con Scroll)
                var panel = new StackPanel
                {
                    Spacing = 12,
                    Width = 440,
                    MinHeight = 520,
                    Children = {
                avisoError,
                nombre, descripcion, codigoBarras, categoriaCombo, proveedorCombo,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { compra, venta, stock } },
                new StackPanel { Children = { new TextBlock { Text = "Imagen", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }, btnImagen, txtImagen } }
            }
                };

                var scroll = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 600 };

                var dialog = new ContentDialog
                {
                    Title = "Editar Producto",
                    PrimaryButtonText = "Guardar cambios",
                    CloseButtonText = "Cancelar",
                    Content = scroll,
                    XamlRoot = this.XamlRoot
                };

                // 6. Lógica de Validación Final ESPECÍFICA (No cierra si hay error)
                dialog.Closing += (s, args) =>
                {
                    if (args.Result == ContentDialogResult.Primary)
                    {
                        args.Cancel = true; // Detenemos el cierre para validar
                        string error = "";

                        // Variables de control
                        bool nV = string.IsNullOrWhiteSpace(nombre.Text);
                        bool dV = string.IsNullOrWhiteSpace(descripcion.Text);
                        bool cV = string.IsNullOrWhiteSpace(codigoBarras.Text);
                        bool catV = categoriaCombo.SelectedItem == null;
                        bool provV = proveedorCombo.SelectedItem == null;
                        bool coV = string.IsNullOrWhiteSpace(compra.Text);
                        bool veV = string.IsNullOrWhiteSpace(venta.Text);
                        bool stV = string.IsNullOrWhiteSpace(stock.Text);

                        // Cascada de mensajes específicos
                        if (nV && dV && cV && catV && provV && coV && veV && stV)
                            error = "Por favor, completa todos los campos del formulario.";
                        else if (nV) error = "Falta el nombre del producto.";
                        else if (dV) error = "Falta la descripción del producto.";
                        else if (cV) error = "Falta el código de barras.";
                        else if (catV) error = "Debes seleccionar una categoría.";
                        else if (provV) error = "Debes seleccionar un proveedor.";
                        else if (coV) error = "Falta el precio de compra.";
                        else if (veV) error = "Falta el precio de venta.";
                        else if (stV) error = "Falta el stock inicial.";
                        else
                        {
                            decimal.TryParse(compra.Text, out decimal pc);
                            decimal.TryParse(venta.Text, out decimal pv);
                            int.TryParse(stock.Text, out int st);

                            if (pc <= 0) error = "El precio de compra debe ser mayor a 0.";
                            else if (pv <= 0) error = "El precio de venta debe ser mayor a 0.";
                            // REGLA DE MARGEN: Venta > Compra
                            else if (pv <= pc) error = "El precio de venta debe ser mayor al precio de compra.";
                            else if (st <= 0) error = "El stock inicial debe ser mayor a 0.";
                            else
                            {
                                // Validación de Duplicados (excluyendo el ID actual)
                                string nbLimpio = nombre.Text.Trim();
                                string cbTexto = codigoBarras.Text.Trim();
                                bool existe = _productos.Any(p => p.Id != producto.Id &&
                                              (nbLimpio.Equals(p.Nombre.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                              (!string.IsNullOrEmpty(p.CodigoBarras) && p.CodigoBarras == cbTexto)));

                                if (existe) error = "El nombre o código de barras ya están en uso por otro producto.";
                            }
                        }

                        if (!string.IsNullOrEmpty(error))
                        {
                            avisoError.Message = error;
                            avisoError.IsOpen = true;
                        }
                        else
                        {
                            args.Cancel = false; // Todo correcto, permitir cierre
                        }
                    }
                };

                // 7. Ejecución y Persistencia
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    decimal.TryParse(compra.Text, out decimal pcFinal);
                    decimal.TryParse(venta.Text, out decimal pvFinal);
                    int.TryParse(stock.Text, out int stFinal);

                    var provSel = proveedorCombo.SelectedItem?.ToString();
                    var provObj = _proveedoresMemoria.FirstOrDefault(p => p.Nombre == provSel);

                    // Actualizar el objeto producto original
                    producto.Nombre = nombre.Text.Trim();
                    producto.Descripcion = descripcion.Text.Trim();
                    producto.CodigoBarras = codigoBarras.Text.Trim();
                    producto.Categoria = categoriaCombo.SelectedItem?.ToString() ?? "Sin Categoría";
                    producto.IdProveedor = provObj?.Id ?? 0;
                    producto.PrecioCompra = pcFinal;
                    producto.PrecioVenta = pvFinal;
                    producto.Stock = stFinal;
                    producto.ImagenPath = rutaImagenSeleccionada;

                    ActualizarVista();
                    await _service.SaveAllAsync(_productos);
                    CargarCategorias();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Edit_Click: {ex.Message}");
            }
        }
    }
}
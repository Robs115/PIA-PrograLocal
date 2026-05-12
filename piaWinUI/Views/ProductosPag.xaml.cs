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
            // 1. Usar datos en memoria y convertirlos a Lista (soluciona que no se vean)
            var listaCategorias = _categoriasMemoria.Select(c => c.Nombre).ToList();

            // Si el JSON está vacío temporalmente, le damos una opción por defecto
            if (!listaCategorias.Any()) listaCategorias.Add("General");

            // 2. Crear Controles
            var nombre = new TextBox
            {
                Header = "Nombre del Producto",
                MaxLength = 20 // Límite de 20 caracteres
            };

            // Evento para limpiar caracteres especiales en tiempo real
            nombre.TextChanging += (s, args) =>
            {
                // Filtra el texto permitiendo solo letras, números y espacios
                string textoLimpio = new string(nombre.Text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());

                // Si el texto original tenía un caracter especial, lo reemplaza por el texto limpio
                if (nombre.Text != textoLimpio)
                {
                    int cursor = nombre.SelectionStart; // Guarda la posición del cursor
                    nombre.Text = textoLimpio;
                    nombre.SelectionStart = Math.Max(0, cursor - 1); // Restaura el cursor
                }
            };

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

            var compra = new TextBox { Header = "Precio Compra", MaxLength = 3, Text = "", PlaceholderText = "0" };
            var venta = new TextBox { Header = "Precio Venta", MaxLength = 3, Text = "", PlaceholderText = "0" };
            var stock = new TextBox { Header = "Stock Inicial", MaxLength = 3, Text = "", PlaceholderText = "0" };

            stock.TextChanging += (s, args) =>
            {
                // Solo permite dígitos (automáticamente bloquea el signo '-' de los negativos)
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

            // 3. Selección de Imagen
            string rutaImagenSeleccionada = "";
            var txtImagen = new TextBlock { Text = "Sin imagen seleccionada", TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.6 };
            var btnImagen = new Button { Content = "Seleccionar Imagen", Margin = new Thickness(0, 5, 0, 0) };

            btnImagen.Click += async (s, args) => {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                // Obtener el HWND de la ventana actual (necesario en WinUI 3)
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    rutaImagenSeleccionada = file.Path;
                    txtImagen.Text = file.Name;
                }
            };

            // 4. Panel de Diseño
            var panel = new StackPanel
            {
                Spacing = 12,
                Children = {
            nombre,
            categoriaCombo,
            proveedorCombo,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { compra, venta, stock } },
            new StackPanel { Children = { new TextBlock { Text = "Imagen", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }, btnImagen, txtImagen } }
        }
            };

            var dialog = new ContentDialog
            {
                Title = "Nuevo Producto",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                Content = panel,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            decimal.TryParse(compra.Text, out decimal pc);
            decimal.TryParse(venta.Text, out decimal pv);
            int.TryParse(stock.Text, out int st);


            var nombreSeleccionado = proveedorCombo.SelectedItem?.ToString();
            var proveedorEncontrado = _proveedoresMemoria.FirstOrDefault(p => p.Nombre == nombreSeleccionado);


            // 5. Crear objeto y guardar
            var nuevoProducto = new Producto
            {
                Id = _productos.Any() ? _productos.Max(x => x.Id) + 1 : 1,
                Nombre = nombre.Text,
                Categoria = categoriaCombo.SelectedItem?.ToString() ?? "Sin Categoría",
                IdProveedor = proveedorEncontrado?.Id ?? 0,
                PrecioCompra = pc,
                PrecioVenta = pv,
                Stock = st,
                ImagenPath = rutaImagenSeleccionada // Aquí guardas la ruta obtenida
            };

            _productos.Add(nuevoProducto);
            ActualizarVista();
            await _service.SaveAllAsync(_productos);
            CargarCategorias(); // Actualiza el filtro de la página principal
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


        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validar que el objeto seleccionado sea un Producto
            if (sender is not Button btn || btn.Tag is not Producto producto)
                return;

            // 2. Preparar datos en memoria (Igual que en Agregar)
            var listaCategorias = _categoriasMemoria.Select(c => c.Nombre).ToList();
            if (!listaCategorias.Any()) listaCategorias.Add("General");

            // 3. Crear Controles y Pre-llenarlos con los datos actuales
            var nombre = new TextBox
            {
                Header = "Nombre del Producto",
                MaxLength = 20,
                Text = producto.Nombre ?? ""
            };

            // Validación de nombre (Sin caracteres especiales)
            nombre.TextChanging += (s, args) =>
            {
                string textoLimpio = new string(nombre.Text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
                if (nombre.Text != textoLimpio)
                {
                    int cursor = nombre.SelectionStart;
                    nombre.Text = textoLimpio;
                    nombre.SelectionStart = Math.Max(0, cursor - 1);
                }
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

            var compra = new TextBox { Header = "Precio Compra", MaxLength = 3, Text = producto.PrecioCompra.ToString() };
            var venta = new TextBox { Header = "Precio Venta", MaxLength = 3, Text = producto.PrecioVenta.ToString() };
            var stock = new TextBox { Header = "Stock Inicial", MaxLength = 3, Text = producto.Stock.ToString() };

            // Validaciones numéricas
            stock.TextChanging += (s, args) =>
            {
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

            // 4. Selección de Imagen (con la ruta actual)
            string rutaImagenSeleccionada = producto.ImagenPath;
            var txtImagen = new TextBlock
            {
                Text = string.IsNullOrEmpty(producto.ImagenPath) ? "Sin imagen seleccionada" : System.IO.Path.GetFileName(producto.ImagenPath),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Opacity = 0.6
            };

            var btnImagen = new Button { Content = "Cambiar Imagen", Margin = new Thickness(0, 5, 0, 0) };

            btnImagen.Click += async (s, args) => {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    rutaImagenSeleccionada = file.Path;
                    txtImagen.Text = file.Name;
                }
            };

            // 5. Panel de Diseño
            var panel = new StackPanel
            {
                Spacing = 12,
                Children = {
            nombre,
            categoriaCombo,
            proveedorCombo,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { compra, venta, stock } },
            new StackPanel { Children = { new TextBlock { Text = "Imagen", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }, btnImagen, txtImagen } }
        }
            };

            var dialog = new ContentDialog
            {
                Title = "Editar Producto",
                PrimaryButtonText = "Guardar cambios",
                CloseButtonText = "Cancelar",
                Content = panel,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            // 6. Procesar datos y actualizar el objeto
            decimal.TryParse(compra.Text, out decimal pc);
            decimal.TryParse(venta.Text, out decimal pv);
            int.TryParse(stock.Text, out int st);

            var nombreProv = proveedorCombo.SelectedItem?.ToString();
            var proveedorEncontrado = _proveedoresMemoria.FirstOrDefault(p => p.Nombre == nombreProv);

            // Actualizamos las propiedades del objeto original (producto)
            producto.Nombre = nombre.Text;
            producto.Categoria = categoriaCombo.SelectedItem?.ToString() ?? "Sin Categoría";
            producto.IdProveedor = proveedorEncontrado?.Id ?? 0;
            producto.PrecioCompra = pc;
            producto.PrecioVenta = pv;
            producto.Stock = st;
            producto.ImagenPath = rutaImagenSeleccionada;

            // 7. Refrescar interfaz y persistencia
            ActualizarVista();
            await _service.SaveAllAsync(_productos);
            CargarCategorias(); // Por si cambió la categoría del producto
        }
    }
}
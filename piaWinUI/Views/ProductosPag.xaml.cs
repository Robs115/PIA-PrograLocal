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
                // ItemsSource = _proveedoresMemoria.Select(p => p.Nombre).ToList(),
                PlaceholderText = "Selecciona un proveedor",
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
            };

            var compra = new NumberBox
            {
                Header = "Precio Compra",
                Value = 0,
                Minimum = 0,
                Maximum = 999,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            var venta = new NumberBox
            {
                Header = "Precio Venta",
                Value = 0,
                Minimum = 0,
                Maximum = 999,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            var stock = new NumberBox
            {
                Header = "Stock Inicial",
                Value = 0,
                Minimum = 0,
                Maximum = 999,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

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

            // 5. Crear objeto y guardar
            var nuevoProducto = new Producto
            {
                Id = _productos.Any() ? _productos.Max(x => x.Id) + 1 : 1,
                Nombre = nombre.Text,
                Categoria = categoriaCombo.SelectedItem?.ToString() ?? "Sin Categoría",
                PrecioCompra = (decimal)compra.Value,
                PrecioVenta = (decimal)venta.Value,
                Stock = (int)stock.Value,
                ImagenPath = rutaImagenSeleccionada // Aquí guardas la ruta obtenida
            };

            _productos.Add(nuevoProducto);
            ActualizarVista();
            await _service.SaveAllAsync(_productos);
            CargarCategorias(); // Actualiza el filtro de la página principal
        }

        private async void Edit_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button btn ||
                btn.Tag is not Producto producto)
                return;

            var nombre = new TextBox
            {
                Header = "Nombre",
                Text = producto.Nombre
            };

            var categoria = new TextBox
            {
                Header = "Categoría",
                Text = producto.Categoria
            };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    nombre,
                    categoria
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Editar",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                Content = panel,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() !=
                ContentDialogResult.Primary)
                return;

            producto.Nombre = nombre.Text;
            producto.Categoria = categoria.Text;

            ActualizarVista();

            await _service.SaveAllAsync(_productos);
        }
    }
}
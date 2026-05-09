using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
        public decimal PrecioCompra => Model.PrecioCompra;
        public decimal PrecioVenta => Model.PrecioVenta;
        public int IdProveedor => Model.IdProveedor;
        public string Categoria => Model.Categoria;
        public int Stock => Model.Stock;

        public string ProveedorNombre { get; set; }
    }

    public sealed partial class ProductosPag : Page
    {
        private readonly ProductService _service = new ProductService();
        private readonly ProveedorService _proveedorService = new ProveedorService();
        private readonly DetalleVentasService _detalleVentasService = new DetalleVentasService();
        private readonly CategoriaService _categoriaService = new CategoriaService();
        private Dictionary<int, string> _proveedorLookup = new();

        public ObservableCollection<ProductoView> Productos { get; } = new();

        private Dictionary<int, string> _proveedores = new();
        private List<Categoria> _categorias = new();

        public ProductosPag()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        // =========================
        // INIT
        // =========================
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await CargarCategorias();
            await CargarDatos();
        }

        private async Task CargarCategorias()
        {
            _categorias = await _categoriaService.GetAllAsync()
                         ?? new List<Categoria>();
        }

        private void SoloNumeros(TextBox tb, bool allowDecimal = false)
        {
            tb.BeforeTextChanging += (s, e) =>
            {

                if (e.NewText.Contains(" "))
                {
                    e.Cancel = true;
                    return;
                }

                // Permitir vacío SOLO mientras escriben
                if (string.IsNullOrEmpty(e.NewText))
                    return;

                // Validar entero o decimal
                if (allowDecimal)
                {
                    e.Cancel = !decimal.TryParse(e.NewText, out _);
                }
                else
                {
                    e.Cancel = !int.TryParse(e.NewText, out _);
                }
            };
        }

        private void ValidarTexto(TextBox tb,bool permitirNumeros = false,bool permitirEspacios = true)
        {
            tb.BeforeTextChanging += (s, e) =>
            {
                string texto = e.NewText;

                // Permitir vacío mientras escribe
                if (string.IsNullOrEmpty(texto))
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

                foreach (char c in texto)
                {
                    // Letras
                    if (char.IsLetter(c))
                        continue;

                    // Espacios
                    if (permitirEspacios && c == ' ')
                        continue;

                    // Números opcionales
                    if (permitirNumeros && char.IsDigit(c))
                        continue;

                    // ❌ Todo lo demás bloqueado
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

            Productos.Clear();

            foreach (var p in productos)
            {
                Productos.Add(new ProductoView(p)
                {
                    ProveedorNombre = _proveedores.TryGetValue(p.IdProveedor, out var nombre)
                        ? nombre
                        : "Desconocido"
                });
            }
        }

        // =========================
        // CATEGORÍAS
        // =========================
        private async void OpenCategoriaDialog(object sender, RoutedEventArgs e)
        {
            var input = new TextBox
            {
                Header = "Nueva categoría",
                PlaceholderText = "Ej. Bebidas",
                MaxLength = 20
            };

            ValidarTexto(input);

            var dialog = new ContentDialog
            {
                Title = "Categoría",
                Content = input,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            string nombreCategoria = input.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombreCategoria))
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = "La categoría es obligatoria.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            if (nombreCategoria.Length < 3)
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = "La categoría debe tener mínimo 3 letras.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            if (nombreCategoria.Length > 30)
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = "La categoría es demasiado larga.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            // ❌ Evitar duplicados
            bool existe = _categorias.Any(c =>
                c.Nombre.Trim().Equals(
                    nombreCategoria,
                    StringComparison.OrdinalIgnoreCase));

            if (existe)
            {
                await new ContentDialog
                {
                    Title = "Duplicado",
                    Content = "La categoría ya existe.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                return;
            }

            // Primera letra mayúscula
            nombreCategoria =
                char.ToUpper(nombreCategoria[0]) +
                nombreCategoria.Substring(1).ToLower();

            await _categoriaService.AddCategoriaAsync(new Categoria
            {
                Nombre = nombreCategoria
            });

            await CargarCategorias();
        }

        // =========================
        // ADD
        // =========================
        private async void OpenAddDialog(object sender, RoutedEventArgs e)
        {
            var producto = new Producto();

            var dialog = BuildDialog(producto, false);

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var list = await _service.GetAllAsync();

            producto.Id = list.Any() ? list.Max(p => p.Id) + 1 : 1;
            list.Add(producto);

            await _service.SaveAllAsync(list);

            await CargarDatos();
        }

        // =========================
        // EDIT
        // =========================
        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProductoView view)
                return;

            var dialog = BuildDialog(view.Model, true);

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var list = await _service.GetAllAsync();

            var index = list.FindIndex(x => x.Id == view.Model.Id);

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
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProductoView view)
                return;

            var detalles = await _detalleVentasService.GetAllAsync();

            if (detalles.Any(d => d.IdProducto == view.Model.Id))
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

            var productos = await _service.GetAllAsync();

            var item = productos.FirstOrDefault(p => p.Id == view.Model.Id);

            if (item != null)
            {
                productos.Remove(item);
                await _service.SaveAllAsync(productos);
            }

            Productos.Remove(view);
        }

        // =========================
        // DIALOG
        // =========================
        private ContentDialog BuildDialog(Producto producto, bool isEdit)
        {
            var nombre = new TextBox
            {
                Header = "Nombre",
                Text = producto.Nombre ?? "",
                MaxLength = 50
            };

            ValidarTexto(nombre);

            var descripcion = new TextBox
            {
                Header = "Descripción",
                Text = producto.Descripcion ?? "",
                MaxLength = 100
            };

            ValidarTexto(descripcion);

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
                Text = producto.PrecioCompra.ToString(),
                MaxLength = 10
            };
            SoloNumeros(precioCompra, true);

            var precioVenta = new TextBox
            {
                Header = "Precio venta",
                Text = producto.PrecioVenta.ToString(),
                MaxLength = 10
            };
            SoloNumeros(precioVenta, true);

            var stock = new TextBox
            {
                Header = "Stock",
                Text = producto.Stock.ToString(),
                MaxLength = 6
            };
            SoloNumeros(stock, false);

            var proveedor = new ComboBox
            {
                Header = "Proveedor",
                ItemsSource = _proveedores.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList(),
                DisplayMemberPath = "Value",
                SelectedValuePath = "Key"
            };

            if (producto.IdProveedor != 0)
                proveedor.SelectedValue = producto.IdProveedor;

            var error = new TextBlock
            {
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                TextWrapping = TextWrapping.Wrap
            };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    nombre,
                    descripcion,
                    categoria,
                    precioCompra,
                    precioVenta,
                    stock,
                    proveedor,
                    error
                }
            };

            var dialog = new ContentDialog
            {
                Title = isEdit ? "Editar producto" : "Nuevo producto",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                Content = panel,
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

                if (nombre.Text.Trim().Length < 3)
                {
                    e.Cancel = true;
                    error.Text = "Nombre demasiado corto.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(descripcion.Text.Trim()))
                {
                    e.Cancel = true;
                    error.Text = "Descripción obligatoria.";
                    return;
                }

                if (descripcion.Text.Trim().Length < 5)
                {
                    e.Cancel = true;
                    error.Text = "Descripción demasiado corta.";
                    return;
                }

                if (categoria.SelectedItem is null)
                {
                    e.Cancel = true;
                    error.Text = "Selecciona una categoría.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(precioCompra.Text))
                {
                    e.Cancel = true;
                    error.Text = "Precio compra obligatorio.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(precioVenta.Text))
                {
                    e.Cancel = true;
                    error.Text = "Precio venta obligatorio.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(stock.Text))
                {
                    e.Cancel = true;
                    error.Text = "Stock obligatorio.";
                    return;
                }



                if (!decimal.TryParse(precioCompra.Text, out decimal pc))
                {
                    e.Cancel = true;
                    error.Text = "Precio compra inválido.";
                    return;
                }

                if (!decimal.TryParse(precioVenta.Text, out decimal pv))
                {
                    e.Cancel = true;
                    error.Text = "Precio venta inválido.";
                    return;
                }

                if (pc < 0 || pv < 0)
                {
                    e.Cancel = true;
                    error.Text = "Precios no pueden ser negativos.";
                    return;
                }

                if (pc >= pv)
                {
                    e.Cancel = true;
                    error.Text = "Venta debe ser mayor que compra.";
                    return;
                }

                if (!int.TryParse(stock.Text, out int st))
                {
                    e.Cancel = true;
                    error.Text = "Stock inválido.";
                    return;
                }

                if (st < 0)
                {
                    e.Cancel = true;
                    error.Text = "Stock no puede ser negativo.";
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
                producto.Categoria =
                    (categoria.SelectedItem as Categoria)?.Nombre;

                producto.PrecioCompra = pc;
                producto.PrecioVenta = pv;
                producto.Stock = st;

                producto.IdProveedor = (int)proveedor.SelectedValue;
            };

            return dialog;
        }
    }
}
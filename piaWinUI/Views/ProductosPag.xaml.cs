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
        public Guid IdProveedor => Model.IdProveedor;
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

        public ObservableCollection<ProductoView> Productos { get; } = new();

        private Dictionary<Guid, string> _proveedores = new();
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

        // =========================
        // DATA
        // =========================
        private async Task CargarDatos()
        {
            var productos = await _service.GetAllAsync();
            var proveedores = await _proveedorService.GetAllAsync();

            _proveedores = proveedores.ToDictionary(p => p.IdProveedor, p => p.Nombre);

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
                PlaceholderText = "Ej. Bebidas"
            };

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

            if (string.IsNullOrWhiteSpace(input.Text))
                return;

            await _categoriaService.AddCategoriaAsync(new Categoria
            {
                Nombre = input.Text.Trim()
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

            producto.Id = Guid.NewGuid();
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
        private ContentDialog BuildDialog(Producto producto, bool edit)
        {
            var nombre = new TextBox { Header = "Nombre", Text = producto.Nombre ?? "" };
            var descripcion = new TextBox { Header = "Descripción", Text = producto.Descripcion ?? "" };

            var categoria = new ComboBox
            {
                Header = "Categoría",
                ItemsSource = _categorias,
                DisplayMemberPath = "Nombre"
            };

            if (!string.IsNullOrWhiteSpace(producto.Categoria))
            {
                categoria.SelectedItem = _categorias
                    .FirstOrDefault(c => c.Nombre == producto.Categoria);
            }

            var precioCompra = new TextBox { Header = "Compra", Text = producto.PrecioCompra.ToString() };
            var precioVenta = new TextBox { Header = "Venta", Text = producto.PrecioVenta.ToString() };
            var stock = new TextBox { Header = "Stock", Text = producto.Stock.ToString() };

            var proveedor = new ComboBox
            {
                Header = "Proveedor",
                ItemsSource = _proveedores,
                DisplayMemberPath = "Value",
                SelectedValuePath = "Key"
            };

            if (producto.IdProveedor != Guid.Empty)
                proveedor.SelectedValue = producto.IdProveedor;

            var error = new TextBlock
            {
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red)
            };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    nombre, descripcion, categoria,
                    precioCompra, precioVenta, stock,
                    proveedor, error
                }
            };

            var dialog = new ContentDialog
            {
                Title = edit ? "Editar" : "Nuevo",
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                Content = panel,
                XamlRoot = this.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, e) =>
            {
                error.Text = "";

                if (string.IsNullOrWhiteSpace(nombre.Text))
                {
                    e.Cancel = true;
                    error.Text = "Nombre requerido";
                    return;
                }

                if (!decimal.TryParse(precioCompra.Text, out var pc) ||
                    !decimal.TryParse(precioVenta.Text, out var pv))
                {
                    e.Cancel = true;
                    error.Text = "Precios inválidos";
                    return;
                }

                if (pc >= pv)
                {
                    e.Cancel = true;
                    error.Text = "Venta debe ser mayor";
                    return;
                }

                if (!int.TryParse(stock.Text, out var st))
                {
                    e.Cancel = true;
                    error.Text = "Stock inválido";
                    return;
                }

                if (proveedor.SelectedValue == null)
                {
                    e.Cancel = true;
                    error.Text = "Proveedor requerido";
                    return;
                }

                producto.Nombre = nombre.Text.Trim();
                producto.Descripcion = descripcion.Text.Trim();
                producto.PrecioCompra = pc;
                producto.PrecioVenta = pv;
                producto.Stock = st;
                producto.IdProveedor = (Guid)proveedor.SelectedValue;

                if (categoria.SelectedItem is Categoria cat)
                    producto.Categoria = cat.Nombre;
            };

            return dialog;
        }
    }
}
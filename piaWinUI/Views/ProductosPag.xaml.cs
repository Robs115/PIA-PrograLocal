using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using piaWinUI.Models;
using piaWinUI.Services;
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

        private async void OpenAddDialog(
            object sender,
            RoutedEventArgs e)
        {
            var nombre = new TextBox
            {
                Header = "Nombre"
            };

            var categoria = new TextBox
            {
                Header = "Categoría"
            };

            var compra = new TextBox
            {
                Header = "Compra"
            };

            var venta = new TextBox
            {
                Header = "Venta"
            };

            var stock = new TextBox
            {
                Header = "Stock"
            };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    nombre,
                    categoria,
                    compra,
                    venta,
                    stock
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

            if (await dialog.ShowAsync() !=
                ContentDialogResult.Primary)
                return;

            decimal.TryParse(compra.Text, out decimal pc);
            decimal.TryParse(venta.Text, out decimal pv);
            int.TryParse(stock.Text, out int st);

            var producto = new Producto
            {
                Id = _productos.Any()
                    ? _productos.Max(x => x.Id) + 1
                    : 1,

                Nombre = nombre.Text,
                Categoria = categoria.Text,
                PrecioCompra = pc,
                PrecioVenta = pv,
                Stock = st
            };

            _productos.Add(producto);

            ActualizarVista();

            await _service.SaveAllAsync(_productos);

            CargarCategorias();
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
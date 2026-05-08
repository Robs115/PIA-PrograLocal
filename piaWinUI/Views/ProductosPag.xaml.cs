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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Collections.ObjectModel;


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

        public ObservableCollection<ProductoView> Productos { get; } = new();

        private Dictionary<Guid, string> _proveedorLookup = new();

        public ProductosPag()
        {
            InitializeComponent();
            Loaded += ProductosPag_Loaded;
        }

        private async void ProductosPag_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        // =========================
        // BLOQUEO DE INPUT NUMÉRICO
        // =========================
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

        // =========================
        // CARGAR DATOS
        // =========================
        private async Task LoadData()
        {
            try
            {
                var productos = await _service.GetAllAsync();
                var proveedores = await _proveedorService.GetAllAsync();

                _proveedorLookup = proveedores.ToDictionary(p => p.IdProveedor, p => p.Nombre);

                Productos.Clear();

                foreach (var p in productos)
                {
                    Productos.Add(new ProductoView(p)
                    {
                        ProveedorNombre = _proveedorLookup.TryGetValue(p.IdProveedor, out var nombre)
                            ? nombre
                            : "Desconocido"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        // =========================
        // AGREGAR
        // =========================
        private async void OpenAddDialog(object sender, RoutedEventArgs e)
        {
            var producto = new Producto();

            var dialog = BuildDialog(producto, false);

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var list = await _service.GetAllAsync();

                producto.Id = Guid.NewGuid();
                list.Add(producto);

                await _service.SaveAllAsync(list);

                await LoadData();
            }
        }

        // =========================
        // EDITAR
        // =========================
        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProductoView view)
                return;

            var dialog = BuildDialog(view.Model, true);

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var list = await _service.GetAllAsync();

                var index = list.FindIndex(x => x.Id == view.Model.Id);

                if (index != -1)
                {
                    list[index] = view.Model;
                    await _service.SaveAllAsync(list);
                }

                await LoadData();
            }
        }

        // =========================
        // ELIMINAR
        // =========================
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ProductoView view)
                return;

            var list = await _service.GetAllAsync();

            var item = list.FirstOrDefault(x => x.Id == view.Model.Id);

            if (item != null)
            {
                list.Remove(item);
                await _service.SaveAllAsync(list);
            }

            Productos.Remove(view);
        }

        // =========================
        // MODAL ADD / EDIT
        // =========================
        private ContentDialog BuildDialog(Producto producto, bool isEdit)
        {
            var nombre = new TextBox
            {
                Header = "Nombre",
                Text = producto.Nombre ?? "",
                MaxLength = 50
            };

            var descripcion = new TextBox
            {
                Header = "Descripción",
                Text = producto.Descripcion ?? "",
                MaxLength = 100
            };

            var categoria = new ComboBox
            {
                Header = "Categoría",
                Items =
                {
                    "Bebidas","Comida","Lácteos","Panadería",
                    "Congelados","Snacks","Limpieza",
                    "Higiene personal","Electrónica","Otros"
                },
                SelectedItem = producto.Categoria
            };

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
                ItemsSource = _proveedorLookup.Select(x => new KeyValuePair<Guid, string>(x.Key, x.Value)).ToList(),
                DisplayMemberPath = "Value",
                SelectedValuePath = "Key"
            };

            if (producto.IdProveedor != Guid.Empty)
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

                if (string.IsNullOrWhiteSpace(nombre.Text))
                {
                    e.Cancel = true;
                    error.Text = "Nombre obligatorio.";
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
                producto.Categoria = categoria.SelectedItem?.ToString();

                producto.PrecioCompra = pc;
                producto.PrecioVenta = pv;
                producto.Stock = st;

                producto.IdProveedor = (Guid)proveedor.SelectedValue;
            };

            return dialog;
        }
    }
}
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI;

namespace piaWinUI.Views
{
    public sealed partial class ReporteVentasPag : Page
    {
        private VentasService _ventaService;

        private readonly ProductService _productService = new();
        private readonly DetalleVentasService _detalleService = new();

        // 🔥 THIS IS WHAT THE GRID BINDS TO
        public ObservableCollection<Venta> Ventas { get; set; } = new();

        public ReporteVentasPag()
        {
            _ventaService = new VentasService(
                _productService,
                _detalleService);

            InitializeComponent();

            DataContext = this;

            CargarDatos();
        }

        private async void CargarDatos()
        {
            var ventas = await _ventaService.GetAllAsync();

            Ventas.Clear();

            foreach (var venta in ventas.OrderByDescending(v => v.Fecha))
            {
                Ventas.Add(venta);
            }
        }

        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Reportes));
        }

        private async void VerDetalle_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            var venta = (Venta)button.Tag;

            var productos = await _productService.GetAllAsync();
            var todosDetalles = await _detalleService.GetAllAsync();

            var detallesDeVenta = todosDetalles
                .Where(d => d.IdVenta == venta.Id)
                .ToList();

            var stack = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(5)
            };

            // INFO GENERAL
            stack.Children.Add(new TextBlock
            {
                Text = $"Venta #{venta.Id}",
                FontSize = 22,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Usuario: {venta.UserName}"
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Fecha: {venta.Fecha:g}"
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Método de pago: {venta.MetodoPago}"
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Total: ${venta.Total:F2}",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });

            // SEPARADOR
            stack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(0, 10, 0, 10)
            });

            // DETALLES
            stack.Children.Add(new TextBlock
            {
                Text = "Productos",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });

            foreach (var detalle in detallesDeVenta)
            {
                var producto = productos
                .FirstOrDefault(p => p.Id == detalle.IdProducto);

                var nombreProducto = producto?.Nombre ?? "Desconocido";


                var border = new Border
                {
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var detalleStack = new StackPanel();

                detalleStack.Children.Add(new TextBlock
                {
                    Text = $"Producto: {nombreProducto} (ID: {detalle.IdProducto})",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });

                detalleStack.Children.Add(new TextBlock
                {
                    Text = $"Cantidad: {detalle.Cantidad}"
                });

                detalleStack.Children.Add(new TextBlock
                {
                    Text = $"Subtotal: ${detalle.Subtotal:F2}"
                });

                border.Child = detalleStack;

                stack.Children.Add(border);
            }

            var dialog = new ContentDialog
            {
                Title = "Detalle de Venta",
                CloseButtonText = "Cerrar",
                Content = new ScrollViewer
                {
                    Content = stack,
                    Height = 500
                },
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
using ClosedXML.Excel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace piaWinUI.Views
{
    public sealed partial class ReporteVentasPag : Page
    {
        private VentasService _ventaService;

        private readonly ProductService _productService = new();
        private readonly DetalleVentasService _detalleService = new();

        private bool _uiBusy = false;

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


            if (_uiBusy) return;
            _uiBusy = true;

            try
            {
                await dialog.ShowAsync();
            }
            finally
            {
                _uiBusy = false;
            }
        }

        private async void ExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            var productos = await _productService.GetAllAsync();
            var detalles = await _detalleService.GetAllAsync();

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("ReporteVentas");

            // Headers
            ws.Cell(1, 1).Value = "Venta ID";
            ws.Cell(1, 2).Value = "Fecha";
            ws.Cell(1, 3).Value = "Usuario";
            ws.Cell(1, 4).Value = "Producto";
            ws.Cell(1, 5).Value = "Cantidad";
            ws.Cell(1, 6).Value = "Subtotal";
            ws.Cell(1, 7).Value = "Total Venta";
            ws.Cell(1, 8).Value = "Metodo Pago";

            int row = 2;

            foreach (var venta in Ventas)
            {
                var detallesVenta = detalles.Where(d => d.IdVenta == venta.Id);

                foreach (var d in detallesVenta)
                {
                    var producto = productos.FirstOrDefault(p => p.Id == d.IdProducto);

                    ws.Cell(row, 1).Value = venta.Id;
                    ws.Cell(row, 2).Value = venta.Fecha;
                    ws.Cell(row, 3).Value = venta.UserName;
                    ws.Cell(row, 4).Value = producto?.Nombre ?? "Desconocido";
                    ws.Cell(row, 5).Value = d.Cantidad;
                    ws.Cell(row, 6).Value = d.Subtotal;
                    ws.Cell(row, 7).Value = venta.Total;
                    ws.Cell(row, 8).Value = venta.MetodoPago;

                    row++;
                }
            }

            ws.Columns().AdjustToContents();


            if (_uiBusy) return;
            _uiBusy = true;

            try
            {
           
            // Save picker
            var picker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedFileName = "ReporteVentas";
            picker.FileTypeChoices.Add("Excel", new List<string> { ".xlsx" });

            StorageFile file = await picker.PickSaveFileAsync();
            if (file == null) return;

            wb.SaveAs(file.Path);
            }
            finally
            {
                _uiBusy = false;
            }
        }

        private async void ExportarExcelFullDetalle_Click(object sender, RoutedEventArgs e)
        {
            var productos = await _productService.GetAllAsync();
            var detalles = await _detalleService.GetAllAsync();

            // 🔥 optimize lookups
            var productosDict = productos.ToDictionary(p => p.Id);
            var detallesPorVenta = detalles
                .GroupBy(d => d.IdVenta)
                .ToDictionary(g => g.Key, g => g.ToList());

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("VentasDetalleCompleto");

            // Headers
            ws.Cell(1, 1).Value = "Venta ID";
            ws.Cell(1, 2).Value = "Fecha";
            ws.Cell(1, 3).Value = "Usuario";
            ws.Cell(1, 4).Value = "Metodo Pago";
            ws.Cell(1, 5).Value = "Producto";
            ws.Cell(1, 6).Value = "Cantidad";
            ws.Cell(1, 7).Value = "Precio Unitario";
            ws.Cell(1, 8).Value = "Subtotal";
            ws.Cell(1, 9).Value = "Total Venta";

            int row = 2;

            foreach (var venta in Ventas)
            {
                if (!detallesPorVenta.TryGetValue(venta.Id, out var detallesVenta))
                    continue;

                foreach (var d in detallesVenta)
                {
                    productosDict.TryGetValue(d.IdProducto, out var producto);

                    ws.Cell(row, 1).Value = venta.Id;
                    ws.Cell(row, 2).Value = venta.Fecha;
                    ws.Cell(row, 3).Value = venta.UserName;
                    ws.Cell(row, 4).Value = venta.MetodoPago;

                    ws.Cell(row, 5).Value = producto?.Nombre ?? "Desconocido";
                    ws.Cell(row, 6).Value = d.Cantidad;

                    // optional if you store unit price
                    ws.Cell(row, 7).Value = d.Subtotal / Math.Max(d.Cantidad, 1);

                    ws.Cell(row, 8).Value = d.Subtotal;
                    ws.Cell(row, 9).Value = venta.Total;

                    row++;
                }
            }

            ws.Columns().AdjustToContents();

            var picker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedFileName = "Ventas_Detalle_Completo";
            picker.FileTypeChoices.Add("Excel", new List<string> { ".xlsx" });

            var file = await picker.PickSaveFileAsync();
            if (file == null) return;

            wb.SaveAs(file.Path);
        }




    }

}
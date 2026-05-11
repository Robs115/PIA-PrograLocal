using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using piaWinUI.Services;
using Microsoft.UI.Xaml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;



namespace piaWinUI.Views
{
    public sealed partial class ReporteProductosPag : Page
    {
        public ISeries[] CategoriaSeries { get; set; }
        public ISeries[] StockSeries { get; set; }
        public Axis[] XAxes { get; set; }

        private readonly ProductService _service = new ProductService();

        private bool _uiBusy = false;

        public ReporteProductosPag()
        {
            InitializeComponent();
            DataContext = this;
            CargarDatos();
        }

        private async void CargarDatos()
        {
            var productos = await _service.GetAllAsync();

            // PIE
            CategoriaSeries = productos
                .GroupBy(p => p.Categoria)
                .Select(g => new PieSeries<double>
                {
                    Values = new double[] { g.Count() },
                    Name = g.Key
                }).ToArray();

            // STOCK
            StockSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = productos.Select(p => (double)p.Stock).ToArray(),
                    Name = "Stock"
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = productos.Select(p => p.Nombre).ToArray(),
                    LabelsRotation = 20
                }
            };

            // KPI
            var total = productos.Sum(p => p.ValorInventario);
            InventarioText.Text = $"${total:F2}";

            DataContext = null;
            DataContext = this;
        }

        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Reportes));
        }

        private async void ExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_uiBusy) return;

            _uiBusy = true;

            var btn = (Button)sender;
            btn.IsEnabled = false;

            try
            {
                _uiBusy = true;


                var productos = await _service.GetAllAsync();
                var totalInventario = productos.Sum(p => p.ValorInventario);

                var porCategoria = productos
                    .GroupBy(p => p.Categoria)
                    .Select(g => new
                    {
                        Categoria = g.Key,
                        Cantidad = g.Count()
                    })
                    .OrderByDescending(x => x.Cantidad)
                    .ToList();

                var picker = new FileSavePicker();
                var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedFileName = "ReporteProductos";
                picker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });

                StorageFile file = await picker.PickSaveFileAsync();
                if (file == null) return;

                string path = file.Path;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);

                        page.Header()
                            .Text("Reporte de Productos")
                            .FontSize(20)
                            .Bold();

                        page.Content().Column(col =>
                        {
                            col.Spacing(12);

                            // =====================
                            // KPI
                            // =====================
                            col.Item().Text($"Valor total inventario: ${totalInventario:F2}")
                                .FontSize(14)
                                .Bold();

                            // =====================
                            // RESUMEN CATEGORÍAS
                            // =====================
                            col.Item().Text("Resumen por categoría")
                                .FontSize(16)
                                .Bold();

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(120);
                                });

                                // HEADER
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Border(1).Padding(5)
                                        .Text("Categoría").Bold();

                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Border(1).Padding(5).AlignCenter()
                                        .Text("Cantidad").Bold();
                                });

                                // ROWS
                                foreach (var c in porCategoria)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                        .Text(c.Categoria ?? "Sin categoría");

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .AlignCenter().Padding(5)
                                        .Text(c.Cantidad.ToString());
                                }
                            });

                            // =====================
                            // PRODUCTOS
                            // =====================
                            col.Item().Text("Detalle de productos")
                                .FontSize(16)
                                .Bold();

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(100);
                                });

                                // HEADER
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Border(1).Padding(5)
                                        .Text("Nombre").Bold();

                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Border(1).Padding(5)
                                        .Text("Categoría").Bold();

                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Border(1).Padding(5)
                                        .AlignCenter()
                                        .Text("Stock").Bold();

                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Border(1).Padding(5)
                                        .AlignCenter()
                                        .Text("Valor").Bold();
                                });

                                // ROWS
                                foreach (var p in productos)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(5)
                                        .Text(p.Nombre);

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(5)
                                        .Text(p.Categoria);

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .AlignCenter()
                                        .Padding(5)
                                        .Text(p.Stock.ToString());

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .AlignRight()
                                        .Padding(5)
                                        .Text(p.ValorInventario.ToString("F2"));
                                }
                            });
                        });

                        page.Footer()
                            .AlignCenter()
                            .Text($"Generado: {DateTime.Now:g}");
                    });
                })
              .GeneratePdf(path);
            }

            finally
            {
                _uiBusy = false;
                btn.IsEnabled = true;
            }
        }

    }
}
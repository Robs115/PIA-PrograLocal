using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
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
        public Axis[] YAxes { get; set; }

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
            var productos = await _service.GetAllAsync() ?? new List<Models.Producto>();

            CategoriaSeries = productos
                .GroupBy(p => p.Categoria)
                .Select(g => new PieSeries<double>
                {
                    Values = new double[] { g.Count() },
                    Name = g.Key,
                    InnerRadius = 60,
                    HoverPushout = 15,
                    Stroke = new SolidColorPaint(SKColor.Parse("#2C2C2C")) { StrokeThickness = 3 },
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                    // ✨ CORRECCIÓN: Usar .Model en lugar de .PrimaryValue
                    DataLabelsFormatter = point => $"{point.Context.Series.Name} ({point.Model})"
                }).ToArray();

            StockSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = productos.Select(p => (double)p.Stock).ToArray(),
                    Name = "Stock",
                    Fill = new LinearGradientPaint(new[] { SKColor.Parse("#81C784"), SKColor.Parse("#2E7D32") }, new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                    MaxBarWidth = 40,
                    Rx = 10,
                    Ry = 10,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                    // ✨ CORRECCIÓN: Usar .Model
                    DataLabelsFormatter = point => $"{point.Model}"
                }
            };

            XAxes = new Axis[] { new Axis { Labels = productos.Select(p => p.Nombre).ToArray(), LabelsPaint = new SolidColorPaint(SKColor.Parse("#B0B0B0")) } };
            YAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColor.Parse("#B0B0B0")), SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#404040")) { StrokeThickness = 1 } } };

            var valorTotal = productos.Sum(p => p.Stock * p.PrecioCompra);
            InventarioText.Text = valorTotal.ToString("C");
        }

        private void Volver_Click(object sender, RoutedEventArgs e) => Frame.GoBack();

        private async void ExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_uiBusy) return;
            _uiBusy = true;

            try
            {
                var picker = new FileSavePicker { SuggestedFileName = $"Reporte_Productos_{DateTime.Now:yyyyMMdd}" };
                picker.FileTypeChoices.Add("PDF", new List<string>() { ".pdf" });
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

                StorageFile file = await picker.PickSaveFileAsync();
                if (file == null) return;

                var productos = await _service.GetAllAsync();
                QuestPDF.Settings.License = LicenseType.Community;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(50);
                        page.Header().Column(col =>
                        {
                            col.Item().Text("Reporte de Productos").FontSize(24).SemiBold();
                            // ✨ CORRECCIÓN: El Padding va ANTES del Text
                            col.Item().PaddingBottom(10).Text($"Valor Total: {productos.Sum(p => p.Stock * p.PrecioCompra):C}");
                        });
                        page.Content().Table(table => {
                            table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); });
                            foreach (var p in productos)
                            {
                                table.Cell().Text(p.Nombre);
                                table.Cell().AlignRight().Text(p.Stock.ToString());
                            }
                        });
                    });
                }).GeneratePdf(file.Path);
            }
            finally { _uiBusy = false; }
        }
    }
}
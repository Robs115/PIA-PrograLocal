using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using piaWinUI.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace piaWinUI
{
    public sealed partial class Reportes : Page
    {
        public ISeries[] TendenciaSeries { get; set; }
        public ISeries[] ProductosSeries { get; set; }
        public Axis[] TendenciaXAxes { get; set; }
        public Axis[] TendenciaYAxes { get; set; }

        private VentasService _ventaService;
        private readonly ProductService _productService = new ProductService();
        private readonly DetalleVentasService _detalleService = new DetalleVentasService();
        private bool _uiBusy = false;

        public Reportes()
        {
            this.InitializeComponent();
            _ventaService = new VentasService(new ProductService(), new DetalleVentasService());
            DataContext = this;
            Loaded += async (s, e) => await CargarDatosReales();
        }

        private async Task CargarDatosReales()
        {
            var ventas = await _ventaService.GetAllAsync() ?? new List<Models.Venta>();
            var detalles = await _detalleService.GetAllAsync() ?? new List<Models.DetalleVentas>();
            var productos = await _productService.GetAllAsync() ?? new List<Models.Producto>();

            // KPIs
            TotalVentasText.Text = ventas.Count.ToString();
            IngresosText.Text = ventas.Sum(v => v.Total).ToString("C");
            ProdVendidosText.Text = detalles.Sum(d => d.Cantidad).ToString();

            var porDia = ventas.GroupBy(v => v.Fecha.Date)
                .Select(g => new { Fecha = g.Key.ToString("dd/MM"), Total = (double)g.Sum(x => x.Total) })
                .OrderBy(x => x.Fecha).ToList();

            TendenciaSeries = new ISeries[] {
                new ColumnSeries<double> {
                    Values = porDia.Select(x => x.Total).ToArray(),
                    Fill = new LinearGradientPaint(new[] { SKColor.Parse("#64B5F6"), SKColor.Parse("#1565C0") }, new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                    Rx = 8, Ry = 8,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    // ✨ CORRECCIÓN: .Model
                    DataLabelsFormatter = point => $"${point.Model}"
                }
            };

            TendenciaXAxes = new Axis[] { new Axis { Labels = porDia.Select(x => x.Fecha).ToArray(), LabelsPaint = new SolidColorPaint(SKColor.Parse("#B0B0B0")) } };
            TendenciaYAxes = new Axis[] { new Axis { LabelsPaint = new SolidColorPaint(SKColor.Parse("#B0B0B0")), SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#404040")) } };

            ProductosSeries = productos.GroupBy(p => p.Categoria)
                .Select(g => new PieSeries<double>
                {
                    Values = new double[] { g.Count() },
                    Name = g.Key,
                    InnerRadius = 60,
                    // ✨ CORRECCIÓN: .Model
                    DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.Model}"
                }).ToArray();
        }

        private void GoProductosReport(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(Views.ReporteProductosPag));
        private void GoVentasReport(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(Views.ReporteVentasPag));

        private async void ExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_uiBusy) return;
            _uiBusy = true;
            try
            {
                var picker = new FileSavePicker { SuggestedFileName = "Reporte_General" };
                picker.FileTypeChoices.Add("PDF", new List<string>() { ".pdf" });
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

                StorageFile file = await picker.PickSaveFileAsync();
                if (file == null) return;

                QuestPDF.Settings.License = LicenseType.Community;
                Document.Create(container => {
                    container.Page(page => {
                        page.Margin(50);
                        page.Header().Column(col => {
                            col.Item().Text("Dashboard General").FontSize(24).SemiBold();
                            // ✨ CORRECCIÓN: El Padding va ANTES del Text
                            col.Item().PaddingBottom(10).Text($"Generado el: {DateTime.Now:d}");
                        });
                        page.Content().Text("Resumen de actividad del sistema.");
                    });
                }).GeneratePdf(file.Path);
            }
            finally { _uiBusy = false; }
        }
    }
}
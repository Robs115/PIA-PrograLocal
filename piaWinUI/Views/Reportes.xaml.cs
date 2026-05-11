using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using piaWinUI.Models;
using piaWinUI.Services;
using piaWinUI.Views;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace piaWinUI
{
    public sealed partial class Reportes : Page
    {
        public ISeries[] VentasSeries { get; set; }
        public ISeries[] TendenciaSeries { get; set; }
        public ISeries[] ProductosSeries { get; set; }

        public string[] VentasLabels { get; set; }

        public Axis[] VentasXAxes { get; set; }
        public Axis[] TendenciaXAxes { get; set; }

        private VentasService _ventaService;
        private readonly ProductService _productService = new ProductService();
        private readonly DetalleVentasService _detalleService = new();

        private bool _uiBusy = false;

        public Reportes()
        {
            this.InitializeComponent();
            this.DataContext = this;

            _ventaService =
            new VentasService(
            _productService,
            _detalleService);

            CargarDatos();
        }

        private void GoProductosReport(object sender, RoutedEventArgs e)
        {
            if (_uiBusy) return;

            Frame.Navigate(typeof(ReporteProductosPag));
        }

        private void GoVentasReport(object sender, RoutedEventArgs e)
        {
            if (_uiBusy) return;

            Frame.Navigate(typeof(ReporteVentasPag));
        }

        private async void CargarDatos()
        {
            var ventas = await _ventaService.GetAllAsync();
            var productos = await _productService.GetAllAsync();
            double totalVentas = ventas.Sum(v => (double)v.Total);
            double promedioVentas = ventas.Count > 0 ? ventas.Average(v => (double)v.Total) : 0;
            int cantidadVentas = ventas.Count;

            TotalVentasText.Text = $"${totalVentas:F2}";
            PromedioVentasText.Text = $"${promedioVentas:F2}";
            CantidadVentasText.Text = $"{cantidadVentas} ventas";

            if (ventas.Count == 0)
            {
                VentasSeries = new ISeries[]
                {
                    new ColumnSeries<double> { Values = new double[] { 0 } }
                };

                TendenciaSeries = new ISeries[]
                {
                    new LineSeries<double> { Values = new double[] { 0 } }
                };
            }
            else
            {
          
                var ventasPorDia = ventas
                    .GroupBy(v => v.Fecha.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Total = g.Sum(v => v.Total)
                    })
                    .OrderBy(x => x.Fecha)
                    .ToList();


                VentasLabels = ventasPorDia
                    .Select(x => x.Fecha.ToString("dd/MM"))
                    .ToArray();

                VentasXAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = VentasLabels
                    }
                };

                TendenciaXAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = VentasLabels
                    }
                };

                VentasSeries = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = ventasPorDia.Select(x => (double)x.Total).ToArray()
                    }
                };

                TendenciaSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = ventasPorDia.Select(x => (double)x.Total).ToArray()
                    }
                };
            }


            if (productos.Count == 0)
            {
                ProductosSeries = new ISeries[]
                {
                    new PieSeries<double> { Values = new double[] { 1 }, Name = "Sin datos" }
                };
            }
            else
            {
                var porCategoria = productos
                    .GroupBy(p => p.Categoria)
                    .Select(g => new
                    {
                        Categoria = g.Key,
                        Cantidad = g.Count()
                    })
                    .ToList();

                ProductosSeries = porCategoria.Select(x =>
                    new PieSeries<double>
                    {
                        Values = new double[] { x.Cantidad },
                        Name = x.Categoria
                    }).ToArray();
            }

            this.DataContext = null;
            this.DataContext = this;
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


                var ventas = await _ventaService.GetAllAsync();
                var productos = await _productService.GetAllAsync();

                double totalVentas = ventas.Sum(v => (double)v.Total);
                double promedioVentas = ventas.Count > 0 ? ventas.Average(v => (double)v.Total) : 0;
                int cantidadVentas = ventas.Count;

                var ventasPorDia = ventas
                    .GroupBy(v => v.Fecha.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Total = g.Sum(v => v.Total)
                    })
                    .OrderBy(x => x.Fecha)
                    .ToList();

                // =========================
                // EXPORTAR GRAFICA TENDENCIA (CORRECTO)
                // =========================
                string pathImg = Path.Combine(Path.GetTempPath(), "tendencia.png");

                var renderTarget = new RenderTargetBitmap();
                await renderTarget.RenderAsync(TendenciaChart);

                var pixels = await renderTarget.GetPixelsAsync();

                byte[] buffer = pixels.ToArray();

                var file = Path.Combine(Path.GetTempPath(), "tendencia.png");

                using (var stream = new FileStream(file, FileMode.Create))
                {
                    var encoder = await BitmapEncoder.CreateAsync(
                        BitmapEncoder.PngEncoderId,
                        stream.AsRandomAccessStream());

                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        (uint)renderTarget.PixelWidth,
                        (uint)renderTarget.PixelHeight,
                        96,
                        96,
                        buffer);

                    await encoder.FlushAsync();
                }

                // =========================
                // FILE PICKER
                // =========================
                var picker = new FileSavePicker();
                var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedFileName = "ReporteGeneral";
                picker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });

                var selectedFile = await picker.PickSaveFileAsync();

                if (selectedFile is null || string.IsNullOrWhiteSpace(selectedFile.Path))
                    return;

                string path = selectedFile.Path;

                // =========================
                // PDF
                // =========================
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);

                        page.Header()
                            .Text("Reporte General")
                            .FontSize(20)
                            .Bold();

                        page.Content().Column(col =>
                        {
                            col.Spacing(10);

                            // KPI
                            col.Item().Text($"Total ventas: ${totalVentas:F2}");
                            col.Item().Text($"Promedio: ${promedioVentas:F2}");
                            col.Item().Text($"Cantidad: {cantidadVentas}");

                            // TABLA VENTAS
                            col.Item().Column(block =>
                            {
                                // HEADER DE SECCIÓN
                                block.Item()
                                    .Background(Colors.Blue.Darken2)
                                    .Padding(8)
                                    .Text("VENTAS POR DÍA")
                                    .FontColor(Colors.White)
                                    .Bold()
                                    .FontSize(12);

                                block.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn();
                                        c.ConstantColumn(100);
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Background(Colors.Blue.Lighten2)
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Darken2)
                                            .Padding(5)
                                            .Text("Fecha").Bold();

                                        h.Cell().Background(Colors.Blue.Lighten2)
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Darken2)
                                            .Padding(5)
                                            .AlignRight()
                                            .Text("Total").Bold();
                                    });

                                    foreach (var v in ventasPorDia)
                                    {
                                        table.Cell().Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(v.Fecha.ToString("dd/MM/yyyy"));

                                        table.Cell().Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .AlignRight()
                                            .Text(v.Total.ToString("F2"));
                                    }
                                });
                            });

                            // GRAFICA
                            col.Item().PaddingTop(10)
                                .Text("Tendencia de ventas")
                                .FontSize(16)
                                .Bold();

                            col.Item()
                                .Image(pathImg)
                                .FitWidth();

                            var porCategoria = productos
                                .GroupBy(p => p.Categoria)
                                .Select(g => new
                                {
                                    Categoria = g.Key,
                                    Cantidad = g.Count()
                                });

                            // PRODUCTOS
                            col.Item().Column(block =>
                            {
                                block.Item()
                                    .Background(Colors.Green.Darken2)
                                    .Padding(8)
                                    .Text("PRODUCTOS POR CATEGORÍA")
                                    .FontColor(Colors.White)
                                    .Bold()
                                    .FontSize(12);

                                block.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn();
                                        c.ConstantColumn(100);
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Background(Colors.Green.Lighten2)
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Darken2)
                                            .Padding(5)
                                            .Text("Categoría").Bold();

                                        h.Cell().Background(Colors.Green.Lighten2)
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Darken2)
                                            .Padding(5)
                                            .AlignCenter()
                                            .Text("Cantidad").Bold();
                                    });

                                    foreach (var c in porCategoria)
                                    {
                                        table.Cell().Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(c.Categoria ?? "Sin categoría");

                                        table.Cell().Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .AlignCenter()
                                            .Text(c.Cantidad.ToString());
                                    }
                                });
                            });
                        });

                        page.Footer()
                            .AlignCenter()
                            .Text($"Generado: {DateTime.Now}");
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
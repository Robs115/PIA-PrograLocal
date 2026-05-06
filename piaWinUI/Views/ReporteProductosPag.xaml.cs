using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using piaWinUI.Services;
using Microsoft.UI.Xaml;



namespace piaWinUI.Views
{
    public sealed partial class ReporteProductosPag : Page
    {
        public ISeries[] CategoriaSeries { get; set; }
        public ISeries[] StockSeries { get; set; }
        public Axis[] XAxes { get; set; }

        private readonly ProductService _service = new ProductService();

        public ReporteProductosPag()
        {
            InitializeComponent();
            DataContext = this;
            CargarDatos();
        }

        private async void CargarDatos()
        {
            var productos = await _service.GetProductsAsync();

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
    }
}
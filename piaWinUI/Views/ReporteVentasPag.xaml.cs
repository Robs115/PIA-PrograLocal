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
    public sealed partial class ReporteVentasPag : Page
    {
        public ISeries[] VentasSeries { get; set; }
        public ISeries[] TendenciaSeries { get; set; }
        public ISeries[] VentasMesSeries { get; set; }

        public Axis[] XAxesDias { get; set; }
        public Axis[] XAxesMeses { get; set; }
        public Axis[] YAxes { get; set; }

        private readonly VentaService _ventaService = new VentaService();

        public ReporteVentasPag()
        {
            InitializeComponent();
            DataContext = this;
            CargarDatos();
        }

        private async void CargarDatos()
        {
            var ventas = await _ventaService.GetVentasAsync();


            var ventasPorDia = ventas
                .GroupBy(v => v.Fecha.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(v => v.Total) })
                .OrderBy(x => x.Fecha)
                .ToList();

            // 🔥 VALORES Y LABELS (SIEMPRE SINCRONIZADOS)
            var valores = ventasPorDia.Select(x => (double)x.Total).ToList();
            var labelsDias = ventasPorDia.Select(x => x.Fecha.ToString("dd/MM")).ToList();

            // SERIES
            VentasSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = valores,
                    Name = "Ventas ($)"
                }
            };

            TendenciaSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = valores,
                    Name = "Tendencia",
                    GeometrySize = 10
                }
            };

            // 🔥 POR MES
            var ventasPorMes = ventas
                .GroupBy(v => new { v.Fecha.Year, v.Fecha.Month })
                .Select(g => new
                {
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Total = g.Sum(v => v.Total)
                })
                .ToList();

            var valoresMes = ventasPorMes.Select(x => (double)x.Total).ToList();
            var labelsMes = ventasPorMes.Select(x => x.Label).ToList();

            VentasMesSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = valoresMes,
                    Name = "Ventas mensuales"
                }
            };

            // 🔥 EJES CORRECTOS
            XAxesDias = new Axis[]
            {
                new Axis
                {
                    Labels = labelsDias,
                    LabelsRotation = 25,
                    Name = "Fecha"
                }
            };

            XAxesMeses = new Axis[]
            {
                new Axis
                {
                    Labels = labelsMes,
                    Name = "Mes"
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Ingresos ($)",
                    Labeler = value => $"${value:N0}"
                }
            };

            // KPIs
            var mejorDia = ventasPorDia.OrderByDescending(x => x.Total).First();

            TotalVentasText.Text = $"${ventas.Sum(v => v.Total):F2}";
            PromedioVentasText.Text = $"${ventas.Average(v => v.Total):F2}";
            MejorDiaText.Text = $"{mejorDia.Fecha:dd/MM}";

            // 🔁 REFRESCAR UI
            DataContext = null;
            DataContext = this;
        }


        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Reportes));
        }
    }
}
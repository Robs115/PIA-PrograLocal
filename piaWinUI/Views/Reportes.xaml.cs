using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using piaWinUI.Models;
using piaWinUI.Services;
using piaWinUI.Views;
using System;
using System.Collections.Generic;
using System.Linq;

namespace piaWinUI
{
    public sealed partial class Reportes : Page
    {
        public ISeries[] VentasSeries { get; set; }
        public ISeries[] TendenciaSeries { get; set; }
        public ISeries[] ProductosSeries { get; set; }

        private VentasService _ventaService;
        private readonly ProductService _productService = new ProductService();
        private readonly DetalleVentasService _detalleService = new();

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
        private void GoClientesReport(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ReporteClientesPag));
        }

        private void GoProductosReport(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ReporteProductosPag));
        }

        private void GoVentasReport(object sender, RoutedEventArgs e)
        {
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
    }
}
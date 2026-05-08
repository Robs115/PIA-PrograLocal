using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using piaWinUI.Services;


namespace piaWinUI.Views
{
    public sealed partial class ReporteClientesPag : Page
    {
        public ISeries[] EdadSeries { get; set; }
        public ISeries[] ClientesAnioSeries { get; set; }
        public Axis[] XAxes { get; set; }

        private readonly ClienteService _service = new ClienteService();

        public ReporteClientesPag()
        {
            InitializeComponent();
            DataContext = this;
            CargarDatos();
        }

        private async void CargarDatos()
        {
            var clientes = await _service.GetAllAsync();
            var hoy = DateTime.Now;

            // 🔥 PIE EDADES
            EdadSeries = new ISeries[]
            {
                new PieSeries<double>{ Values = new[]{ (double)clientes.Count(c=> hoy.Year - c.FechaNacimiento.Year <18)}, Name="<18"},
                new PieSeries<double>{ Values = new[]{ (double)clientes.Count(c=> hoy.Year - c.FechaNacimiento.Year >=18 && hoy.Year - c.FechaNacimiento.Year<=25)}, Name="18-25"},
                new PieSeries<double>{ Values = new[]{ (double)clientes.Count(c=> hoy.Year - c.FechaNacimiento.Year >=26 && hoy.Year - c.FechaNacimiento.Year<=40)}, Name="26-40"},
                new PieSeries<double>{ Values = new[]{ (double)clientes.Count(c=> hoy.Year - c.FechaNacimiento.Year >40)}, Name="40+"}
            };

            // 🔥 CLIENTES POR AÑO
            var porAnio = clientes
                .GroupBy(c => c.FechaNacimiento.Year)
                .Select(g => new { Año = g.Key, Cantidad = g.Count() })
                .OrderBy(x => x.Año)
                .ToList();

            ClientesAnioSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = porAnio.Select(x => (double)x.Cantidad).ToArray(),
                    Name = "Clientes"
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = porAnio.Select(x => x.Año.ToString()).ToArray(),
                    Name = "Año"
                }
            };

            // KPI
            if (clientes.Count > 0)
            {
                var edades = clientes.Select(c => hoy.Year - c.FechaNacimiento.Year);
                PromedioEdadText.Text = $"{edades.Average():F1} años";
            }

            DataContext = null;
            DataContext = this;
        }

        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Reportes));
        }
    }
}
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
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace piaWinUI
{
    public sealed partial class Reportes : Page
    {
        public ISeries[] VentasSeries { get; set; }
        public ISeries[] TendenciaSeries { get; set; }
        public ISeries[] ProductosSeries { get; set; }

        public Reportes()
        {
            this.InitializeComponent();

            // 📊 BARRAS
            VentasSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = new double[] { 200, 450, 300, 600, 500 }
                }
            };

            // 📈 LÍNEA
            TendenciaSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new double[] { 100, 300, 250, 400, 550 }
                }
            };

            // 🥧 PASTEL
            ProductosSeries = new ISeries[]
            {
                new PieSeries<double> { Values = new double[] { 40 } },
                new PieSeries<double> { Values = new double[] { 30 } },
                new PieSeries<double> { Values = new double[] { 20 } }
            };

            // 🔥 ESTO ES LO QUE TE FALTABA
            this.DataContext = this;
        }
    }
}
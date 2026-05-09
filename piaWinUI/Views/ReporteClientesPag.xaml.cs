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
        
        public ReporteClientesPag()
        {
            InitializeComponent();
            DataContext = this;
            
        }

   

        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Reportes));
        }
    }
}
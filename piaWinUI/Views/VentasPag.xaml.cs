using piaWinUI.Models;
using piaWinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace piaWinUI.Views
{
    public sealed partial class VentasPag : Page


    {

        private VentaService _ventaService = new VentaService();
        private List<Venta> listaVentas = new List<Venta>();

        public VentasPag()
        {
            
            this.InitializeComponent();
            
        }

        private async void CargarVentas()
        {
            
        }

        private async void GuardarVenta_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private async void BuscarProducto_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LimpiarFormulario()
        {
            
        }
    }
}
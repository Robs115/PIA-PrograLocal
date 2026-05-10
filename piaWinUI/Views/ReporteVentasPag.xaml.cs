using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using piaWinUI.Models;
using piaWinUI.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace piaWinUI.Views
{
    public sealed partial class ReporteVentasPag : Page
    {
        private VentasService _ventaService;

        private readonly ProductService _productService = new();
        private readonly DetalleVentasService _detalleService = new();

        // 🔥 THIS IS WHAT THE GRID BINDS TO
        public ObservableCollection<Venta> Ventas { get; set; } = new();

        public ReporteVentasPag()
        {
            _ventaService = new VentasService(
                _productService,
                _detalleService);

            InitializeComponent();

            DataContext = this;

            CargarDatos();
        }

        private async void CargarDatos()
        {
            var ventas = await _ventaService.GetAllAsync();

            Ventas.Clear();

            foreach (var venta in ventas.OrderByDescending(v => v.Fecha))
            {
                Ventas.Add(venta);
            }
        }

        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Reportes));
        }

        private void VerDetalle_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            var venta = (Venta)button.Tag;

            // navegar a detalle
            //Frame.Navigate(typeof(DetalleVentaPag), venta);
        }
    }
}
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace piaWinUI.Views
{
    public sealed partial class InicioPag : Page
    {
        private readonly ProductService _productService = new();
        private VentasService _ventaService;
        private readonly DetalleVentasService _detalleService = new();
        private readonly PedidoService _pedidoService = new();

        public InicioPag()
        {
            InitializeComponent();

            _ventaService =
            new VentasService(
            _productService,
            _detalleService);

            Loaded += InicioPag_Loaded;
        }

        private async void InicioPag_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboard();
        }

        private async Task LoadDashboard()
        {
            var productos =
                await _productService.GetAllAsync();

            var ventas =
                await _ventaService.GetAllAsync();

            var detalles =
                await _detalleService.GetAllAsync();

            var pedidos =
                await _pedidoService.GetAllAsync();

            // =========================
            // VENTAS DEL DÍA
            // =========================

            var hoy = DateTime.Now.Date;

            var ventasHoy = ventas
                .Where(v => v.Fecha.Date == hoy)
                .Sum(v => v.Total);

            txtVentasDia.Text =
                $"{ventasHoy:C}";

            // =========================
            // GANANCIA MENSUAL
            // =========================

            var inicioMes =
                new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    1);

            var ventasMes = ventas
                .Where(v => v.Fecha >= inicioMes)
                .Sum(v => v.Total);

            txtGananciaMensual.Text =
                $"{ventasMes:C}";

            // =========================
            // PRODUCTOS BAJOS
            // =========================

            var bajos =
                productos.Count(p =>
                    p.Stock > 0 &&
                    p.Stock <= 5);

            txtStockBajo.Text =
                bajos.ToString();

            // =========================
            // PRODUCTOS AGOTADOS
            // =========================

            var agotados =
                productos.Count(p =>
                    p.Stock == 0);

            txtAgotados.Text =
                agotados.ToString();

            // =========================
            // PEDIDOS RECIENTES
            // =========================

            var pedidosRecientes =
                pedidos.Count(p =>
                    p.Fecha.Date >=
                    DateTime.Now.Date.AddDays(-7));

            txtPedidos.Text =
                pedidosRecientes.ToString();

            // =========================
            // ALERTAS
            // =========================

            AlertasPanel.Children.Clear();

            if (bajos > 0)
            {
                AlertasPanel.Children.Add(
                    CreateAlert(
                        $"⚠️ {bajos} productos necesitan reabastecimiento"));
            }

            if (agotados > 0)
            {
                AlertasPanel.Children.Add(
                    CreateAlert(
                        $"🛑 {agotados} productos están agotados"));
            }

            if (ventasHoy > 0)
            {
                AlertasPanel.Children.Add(
                    CreateAlert(
                        $"📈 Las ventas de hoy generan {ventasHoy:C}"));
            }

            if (pedidosRecientes > 0)
            {
                AlertasPanel.Children.Add(
                    CreateAlert(
                        $"📦 {pedidosRecientes} pedidos recientes registrados"));
            }

            // =========================
            // GRAFICA VENTAS POR MES
            // =========================

            var mesesTexto = new[]
            {
                "Ene",
                "Feb",
                "Mar",
                "Abr",
                "May",
                "Jun",
                "Jul",
                "Ago",
                "Sep",
                "Oct",
                "Nov",
                "Dic"
            };

            var ventasPorMes =
                Enumerable.Range(1, 12)
                .Select(m =>
                    ventas.Count(v =>
                        v.Fecha.Month == m))
                .ToArray();

            VentasChart.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = ventasPorMes,

                    DataLabelsSize = 14,

                    DataLabelsPosition =
                        DataLabelsPosition.Top
                }
            };

            VentasChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = mesesTexto,

                    LabelsRotation = 0,

                    TextSize = 14,

                    Name = "Meses"
                }
            };

            VentasChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Cantidad de ventas",

                    TextSize = 14,

                    MinLimit = 0
                }
            };

            // =========================
            // PRODUCTOS MÁS VENDIDOS
            // =========================

            var topProductos =
                detalles
                .GroupBy(d => d.NombreProducto)
                .Select(g => new
                {
                    Nombre = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToList();

            ProductosChart.Series =
                topProductos
                .Select(x =>
                    new PieSeries<int>
                    {
                        Values = new[] { x.Cantidad },
                        Name = x.Nombre
                    })
                .ToArray();

            // =========================
            // PEDIDOS RECIENTES
            // =========================

            PedidosPanel.Children.Clear();

            var ultimosPedidos =
                pedidos
                .OrderByDescending(p => p.Fecha)
                .Take(5)
                .ToList();

            foreach (var pedido in ultimosPedidos)
            {
                PedidosPanel.Children.Add(
                    CreatePedidoCard(pedido));
            }
        }

        // =========================
        // ALERTAS
        // =========================

        private Border CreateAlert(string text)
        {
            return new Border
            {
                CornerRadius = new CornerRadius(12),

                Padding = new Thickness(14),

                Margin = new Thickness(0, 0, 0, 8),

                Background =
                    new SolidColorBrush(
                        Microsoft.UI.ColorHelper.FromArgb(
                            25,
                            255,
                            193,
                            7)),

                Child = new TextBlock
                {
                    Text = text,

                    FontSize = 15
                }
            };
        }

        // =========================
        // TARJETA PEDIDO
        // =========================

        private Border CreatePedidoCard(Pedidos pedido)
        {
            return new Border
            {
                CornerRadius = new CornerRadius(14),

                Padding = new Thickness(16),

                Margin = new Thickness(0, 0, 0, 10),

                Background =
                    new SolidColorBrush(
                        Microsoft.UI.ColorHelper.FromArgb(
                            20,
                            99,
                            102,
                            241)),

                Child = new StackPanel
                {
                    Spacing = 6,

                    Children =
                    {
                        new TextBlock
                        {
                            Text = pedido.NombreProducto,

                            FontSize = 18,

                            FontWeight =
                                Microsoft.UI.Text.FontWeights.SemiBold
                        },

                        new TextBlock
                        {
                            Text =
                                $"Proveedor: {pedido.NombreProveedor}",

                            Opacity = 0.7
                        },

                        new TextBlock
                        {
                            Text =
                                $"Cantidad: {pedido.Cantidad}",

                            Opacity = 0.7
                        },

                        new TextBlock
                        {
                            Text =
                                pedido.Fecha.ToString(
                                    "dd/MM/yyyy hh:mm tt"),

                            Opacity = 0.5
                        }
                    }
                }
            };
        }
    }
}

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using piaWinUI.Services;
using System;
using System.Threading.Tasks;

namespace piaWinUI
{
    public sealed partial class CajaPag : Page
    {
        private CajaService _cajaService = AppServices.Caja;

        public CajaPag()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await _cajaService.LoadCajaAsync();

            SincronizarUI();
        }

        private void SincronizarUI()
        {
            txtEstado.Text = _cajaService.CajaAbierta
                ? "Caja abierta"
                : "Caja cerrada";

            txtEstado.Foreground = _cajaService.CajaAbierta
                ? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Red);

            txtSaldo.Text = $"Saldo: {_cajaService.Saldo:C}";

            gridMovimientos.ItemsSource = null;
            gridMovimientos.ItemsSource =
                _cajaService.ObtenerMovimientos();

            gridCortes.ItemsSource = null;
            gridCortes.ItemsSource =
                _cajaService.ObtenerHistorialCortes();
        }

        private async void AbrirCaja_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                await _cajaService.AbrirCaja(1000);

                SincronizarUI();
            }
            catch (Exception ex)
            {
                await MostrarDialogo("Error", ex.Message);
            }
        }

        private async void CerrarCaja_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                await _cajaService.CerrarCaja();

                SincronizarUI();
            }
            catch (Exception ex)
            {
                await MostrarDialogo("Error", ex.Message);
            }
        }

        private async void RegistrarMovimiento_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var item =
                    cmbTipo.SelectedItem as ComboBoxItem;

                if (item == null)
                    throw new Exception(
                        "Selecciona tipo");

                string tipo =
                    item.Content.ToString();

                string concepto =
                    txtConcepto.Text.Trim();

                if (!decimal.TryParse(
                    txtMonto.Text,
                    out decimal monto))
                {
                    throw new Exception(
                        "Monto inválido");
                }

                await _cajaService.RegistrarMovimiento(
                    tipo,
                    monto,
                    concepto);

                txtConcepto.Text = "";
                txtMonto.Text = "";

                SincronizarUI();
            }
            catch (Exception ex)
            {
                await MostrarDialogo(
                    "Error",
                    ex.Message);
            }
        }

        private async void CalcularCorte_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (!_cajaService.CajaAbierta)
                {
                    throw new Exception(
                        "La caja está cerrada");
                }

                if (!decimal.TryParse(
                    txtConteo.Text,
                    out decimal conteo))
                {
                    throw new Exception(
                        "Conteo inválido");
                }

                var corte =
                    _cajaService.CalcularCorte(conteo);

                txtIngresos.Text =
                    $"Ingresos: {corte.ingresos:C}";

                txtEgresos.Text =
                    $"Egresos: {corte.egresos:C}";

                await MostrarDialogo(
                    "Corte de Caja",
                    $"Esperado: {corte.esperado:C}\n" +
                    $"Contado: {conteo:C}\n" +
                    $"Diferencia: {corte.diferencia:C}");
            }
            catch (Exception ex)
            {
                await MostrarDialogo(
                    "Error",
                    ex.Message);
            }
        }

        private async void GuardarCorte_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(
                    txtConteo.Text,
                    out decimal conteo))
                {
                    throw new Exception(
                        "Conteo inválido");
                }

                var mensaje =
                    await _cajaService
                        .GuardarCorte(conteo);

                txtConteo.Text = "";

                SincronizarUI();

                await MostrarDialogo(
                    "Resultado",
                    mensaje);
            }
            catch (Exception ex)
            {
                await MostrarDialogo(
                    "Error",
                    ex.Message);
            }
        }

        private async Task MostrarDialogo(
            string titulo,
            string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
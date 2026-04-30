using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace piaWinUI
{
    public sealed partial class CajaPag : Page
    {
        private CajaService _cajaService = AppServices.Caja;

        public CajaPag()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SincronizarUI();
        }

        private void SincronizarUI()
        {
            txtEstado.Text = _cajaService.CajaAbierta ? "Caja abierta" : "Caja cerrada";
            txtSaldo.Text = $"Saldo: ${_cajaService.Saldo}";

            gridMovimientos.ItemsSource = null;
            gridMovimientos.ItemsSource = _cajaService.ObtenerMovimientos();

            gridCortes.ItemsSource = null;
            gridCortes.ItemsSource = _cajaService.ObtenerHistorialCortes();
        }

        private async void AbrirCaja_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cajaService.AbrirCaja(1000);
                txtEstado.Text = "Caja abierta";
                ActualizarUI();
            }
            catch (Exception ex)
            {
                await MostrarError(ex.Message);
            }
        }

        private async void CerrarCaja_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cajaService.CerrarCaja();
                txtEstado.Text = "Caja cerrada";
            }
            catch (Exception ex)
            {
                await MostrarError(ex.Message);
            }
        }

        private async void RegistrarMovimiento_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tipoItem = cmbTipo.SelectedItem as ComboBoxItem;
                if (tipoItem == null) throw new Exception("Selecciona tipo");

                string tipo = tipoItem.Content.ToString();
                string concepto = txtConcepto.Text;

                if (!decimal.TryParse(txtMonto.Text, out decimal monto))
                    throw new Exception("Monto inválido");

                _cajaService.RegistrarMovimiento(tipo, monto, concepto);

                txtConcepto.Text = "";
                txtMonto.Text = "";

                ActualizarUI();
            }
            catch (Exception ex)
            {
                await MostrarError(ex.Message);
            }
        }

        private async void CalcularCorte_Click(object sender, RoutedEventArgs e)
        {
            if (!_cajaService.CajaAbierta)
            {
                await MostrarError("La caja está cerrada");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtConteo.Text))
            {
                await MostrarError("Ingresa el conteo real");
                return;
            }

            if (!decimal.TryParse(txtConteo.Text, out decimal conteo))
            {
                await MostrarError("Formato inválido");
                return;
            }

            var corte = _cajaService.CalcularCorte(conteo);

            txtIngresos.Text = $"Ingresos: ${corte.ingresos}";
            txtEgresos.Text = $"Egresos: ${corte.egresos}";

            var dialog = new ContentDialog
            {
                Title = "Corte de Caja",
                Content =
                    $"Esperado: {corte.esperado}\n" +
                    $"Contado: {conteo}\n" +
                    $"Diferencia: {corte.diferencia}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void GuardarCorte_Click(object sender, RoutedEventArgs e)
        {
            if (!_cajaService.CajaAbierta)
            {
                await MostrarError("La caja está cerrada");
                return;
            }

            if (!decimal.TryParse(txtConteo.Text, out decimal conteo))
            {
                await MostrarError("Ingresa un conteo válido");
                return;
            }

            var mensaje = _cajaService.GuardarCorte(conteo);

            gridCortes.ItemsSource = null;
            gridCortes.ItemsSource = _cajaService.ObtenerHistorialCortes();

            var dialog = new ContentDialog
            {
                Title = "Resultado del Corte",
                Content = mensaje,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void ActualizarUI()
        {
            txtSaldo.Text = $"Saldo: ${_cajaService.Saldo}";
            gridMovimientos.ItemsSource = null;
            gridMovimientos.ItemsSource = _cajaService.ObtenerMovimientos();
        }

        // 🔥 AHORA CORRECTO
        private async Task MostrarError(string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = mensaje,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        public void RegistrarVenta(decimal total)
        {
            _cajaService.RegistrarMovimiento("Ingreso", total, "Venta");
            ActualizarUI();
        }
    }
}
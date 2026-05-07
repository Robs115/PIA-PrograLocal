using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Helpers;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ClientesPag : Page
    {
        private readonly ClienteService _service = new ClienteService();
        public ClientesPag()
        {
            InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        public DateTimeOffset MaxFechaNacimiento => DateTime.Now.AddYears(-15);
        private void SetStatus(string text, bool isError = true)
        {
            StatusTextBlock.Text = text;
            StatusTextBlock.Foreground = isError ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }

        private async Task DialogError(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Aceptar",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }


        private async void Guardar_Click(object sender, RoutedEventArgs e)

        {

            string nombre = Nombre.Text;
            var telefono = Telefono.Text;
            var fechanacimiento = FechaNacimiento.Date.DateTime;
            var email = Email.Text;

            // validaciones basicas

            string error;

            if (!ValidationHelper.ValidarNombre(nombre, out error))
            {
                await DialogError("Error", error);
                return;
            }

            if (!ValidationHelper.ValidarTelefono(telefono, out error))
            {
                await DialogError("Error", error);
                return;
            }

            if (!ValidationHelper.ValidarEmail(email, out error))
            {
                await DialogError("Error", error);
                return;
            }
            if (!ValidationHelper.ValidarFechaNacimiento(
        FechaNacimiento.Date.DateTime,
        out error))
            {
                await DialogError("Error", error);
                return;
            }
            // debe tener al menos 15 años
            DateTime fechaMinimaPermitida = DateTime.Now.AddYears(-15);

            if (fechanacimiento > fechaMinimaPermitida)
            {
                SetStatus("El cliente debe tener al menos 15 años.");
                return;
            }

            var clientes = await _service.GetClientesAsync();
            //ver si ya existe un tlefono registrado
            var existetelefono = clientes.FirstOrDefault(c => c.Telefono == telefono);

            if (existetelefono != null)
            {
                await DialogError("Error", "El teléfono ya está registrado.");
                return;
            }

           
            
            
            try
            {
               
                var nuevo = new Cliente
                {
                    
                    Id = Guid.NewGuid(),
                    Nombre = nombre,
                    Telefono = telefono,
                    FechaNacimiento = fechanacimiento,
                    Email = email
                };

                clientes.Add(nuevo);

                await _service.SaveClienteAsync(clientes);

                //limpiar campos
                Nombre.Text = string.Empty;
                Telefono.Text = string.Empty;
                FechaNacimiento.Date = DateTime.Now;
                Email.Text = string.Empty;

                //mostrar mensaje de exito
                SetStatus("Cliente guardado exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void ReporteClientes_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ReporteClientesPag));
        }

        private void BuscarClientes_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(BuscarClientesPag));


        }
    }

}
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Models;
using piaWinUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
        }


        private void SetStatus(string text, bool isError = true)
        {
            StatusTextBlock.Text = text;
            StatusTextBlock.Foreground = isError ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }




        private async void Guardar_Click(object sender, RoutedEventArgs e)

        {

            string nombre = Nombre.Text;
            var telefono = Telefono.Text;
            var fechanacimiento = FechaNacimiento.Date.DateTime;
            var email = Email.Text;

            // validaciones basicas

            if (string.IsNullOrWhiteSpace(nombre))
            {
                SetStatus("El campo nombre no debe estar vacio.");
            }

            if (string.IsNullOrWhiteSpace(telefono))
            {
                SetStatus("El campo telfono no debe estar vacio.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                SetStatus("El campo email no debe estar vacio.");
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                SetStatus($"El campo email debe contener un {"@"} y un {"."}.");
                return;
            }

            if (!telefono.All(char.IsDigit))
            {
                SetStatus("El campo telefono ddebe tener el formato adecuado.");
                return;
            }

            if (telefono.Length != 10)
            {
                SetStatus("El campo telefono debe tener 10 digitos.");
                return;
            }


            if (fechanacimiento > DateTime.Now)
            {
                SetStatus("El campo Fecha de Nacimiento ser menor a la fecha actual.");
                return;
            }

            try
            {
                var clientes = await _service.GetClientesAsync();
                var nuevo = new Cliente
                {
                    //algun dia el id tiene que sacar el ultimo de la lista del json
                    Id = clientes.Count > 0 ? clientes.Max(c => c.Id) + 1 : 1,
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
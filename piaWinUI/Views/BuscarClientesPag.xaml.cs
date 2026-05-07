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
    public sealed partial class BuscarClientesPag : Page
    {
        private ClienteService _service = new ClienteService();
        private List<Cliente> listaClientes = new List<Cliente>();
        private VentaService _ventaService = new VentaService();
        private List<Venta> listaVentas = new List<Venta>();
        public BuscarClientesPag()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            listaClientes = await _service.GetClientesAsync();

            // Mostrar últimos primero
            ClientesList.ItemsSource = listaClientes
                .OrderByDescending(c => c.Id)
                .Take(10)
                .ToList();
        }
        private void Buscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = buscador.Text.ToLower();

            var filtrados = listaClientes
                .Where(c => c.Nombre.ToLower().Contains(texto))
                .ToList();

            ClientesList.ItemsSource = filtrados;
        }

        public async void DialogError(string title, string content)
        {
            var errorDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Aceptar",
                XamlRoot = this.Content.XamlRoot
            };

            await errorDialog.ShowAsync();
        }

        private async void EditarCliente_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button?.Tag is not Guid idCliente)
                return;

            // Obtener clientes
            var clientes = await _service.GetClientesAsync();

            // Buscar cliente
            var clienteSeleccionado = clientes.FirstOrDefault(c => c.Id == idCliente);

            if (clienteSeleccionado == null)
                return;

            // 🔥 Cargar datos al dialog
            nombreeditar.Text = clienteSeleccionado.Nombre;
            telefonoeditar.Text = clienteSeleccionado.Telefono;
            emaileditar.Text = clienteSeleccionado.Email;


           
            // 🔥 Mostrar dialog
            var result = await EditarProductoDialog.ShowAsync();

            // Si cancela
            if (result != ContentDialogResult.Primary)
                return;
            var nombre = nombreeditar.Text;
            var telefono = telefonoeditar.Text;
            var email = emaileditar.Text;
            //validar 


            if (string.IsNullOrWhiteSpace(nombre))
            {
                DialogError("Error", "El campo nombre no debe estar vacio.");
                return;
            }

            if (string.IsNullOrWhiteSpace(telefono))
            {
                DialogError("Error", "El campo telefono no debe estar vacio.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                DialogError("Error", "El campo email no debe estar vacio.");
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                DialogError("Error", "El campo email debe tener el formato adecuado.");
                return;
            }

            if (!telefono.All(char.IsDigit))
            {
                DialogError("Error", "El campo telefono debe contener digitos numericos.");
                return;
            }

            if (telefono.Length != 10)
            {
                DialogError("Error", "El telefono debe tener 10 caracteres.");
                return;
            }




            // 🔥 Guardar cambios
            clienteSeleccionado.Nombre = nombre;
            clienteSeleccionado.Telefono =  telefono;
            clienteSeleccionado.Email = email;

            // 🔥 Guardar JSON
            await _service.SaveClienteAsync(clientes);

            // 🔥 Refrescar lista
            listaClientes = clientes;

            ClientesList.ItemsSource = null;
            ClientesList.ItemsSource = listaClientes;
        }
        private async void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 convertir correctamente a Guid
            var button = sender as Button;

            if (button?.Tag == null)
                return;

            // 🔥 convertir correctamente a Guid
            Guid idCliente = (Guid)button.Tag;

            // Obtener datos
            var clientes = await _service.GetClientesAsync();
            var ventas = await _ventaService.GetVentasAsync();


            // Buscar cliente
            var clienteSeleccionado = clientes.FirstOrDefault(c => c.Id == idCliente);
            
            if (clienteSeleccionado == null)
                return;

            // 🔥 VALIDACIÓN
            bool tieneVentas = ventas.Any(v => v.IdCliente == idCliente);
            if (tieneVentas)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "No se puede eliminar",
                    Content = "Este cliente tiene ventas asociadas y no se puede eliminar.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }
            var dialog = new ContentDialog
            {
                Title = "Eliminar cliente",
                Content = "¿Seguro que quieres eliminar este cliente?",
                PrimaryButtonText = "Sí",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            // Eliminar
            clientes.RemoveAll(c => c.Id == idCliente);

            // Guardar cambios
            await _service.SaveClienteAsync(clientes);

            // Refrescar lista en pantalla
            listaClientes = clientes;
            ClientesList.ItemsSource = null;
            ClientesList.ItemsSource = listaClientes;
        }



        public void regresar_click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ClientesPag));
        }
    }
}

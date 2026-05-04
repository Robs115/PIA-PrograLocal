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

        public void EditarCliente_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private async void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var id = (int)(sender as Button).Tag;

            if (id == 0)
                return;

            // Obtener lista actual
            var clientes = await _service.GetClientesAsync();

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
            clientes.RemoveAll(c => c.Id == id);

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

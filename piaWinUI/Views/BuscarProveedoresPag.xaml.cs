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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BuscarProveedoresPag : Page
    {
        private ProveedorService _service = new ProveedorService();
        private List<Proveedor> listaProveedores = new List<Proveedor>();
        private ProductService _ProductService = new ProductService();
        private List<Producto> listaProductos = new List<Producto>();
        public BuscarProveedoresPag()
        {
            InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            listaProveedores = await _service.GetAllAsync();

            // Mostrar últimos primero
            ProveedoresList.ItemsSource = listaProveedores
                .OrderByDescending(c => c.IdProveedor)
                .Take(10)
                .ToList();
        }
        private void Buscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = buscador.Text.ToLower();

            var filtrados = listaProveedores
                .Where(c => c.Nombre.ToLower().Contains(texto))
                .ToList();

            ProveedoresList.ItemsSource = filtrados;
        }

        private async Task DialogErrorValidaciones(string title, string content)
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

        private async void EditarProveedor_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button?.Tag is not Guid idProveedor)
                return;

            // Obtener proveedores
            var proveedores = await _service.GetAllAsync();   
            // Buscar proveedor
            var proveedorSeleccionado = proveedores.FirstOrDefault(c => c.IdProveedor == idProveedor);

            if (proveedorSeleccionado == null)
                return;

            // 🔥 Cargar datos al dialog
            nombreeditar.Text = proveedorSeleccionado.Nombre;
            telefonoeditar.Text = proveedorSeleccionado.Telefono;
            emaileditar.Text = proveedorSeleccionado.Email;


            //variables para validar

            

            // 🔥 Mostrar dialog
            var result = await EditarProveedorDialog.ShowAsync();   
            // Si cancela
            if (result != ContentDialogResult.Primary)
                return;

            var nombre = nombreeditar.Text;
            var telefono = telefonoeditar.Text;
            var email = emaileditar.Text;
            //validar 
            string error;

            if (!ValidationHelper.ValidarNombre(nombre, out  error))
            {
                await DialogErrorValidaciones("Error", error);
                return;
            }

            if (!ValidationHelper.ValidarTelefono(telefono, out error))
            {
                await DialogErrorValidaciones("Error", error);
                return;
            }

            if (!ValidationHelper.ValidarEmail(email, out error))
            {
               await DialogErrorValidaciones("Error", error);
                return;
            }

            
            //ver si ya existe un tlefono registrado
            var existetelefono =   proveedores.FirstOrDefault(c => c.Telefono == telefono);

            if (existetelefono != null)
            {
                await DialogErrorValidaciones("Error", "El teléfono ya está registrado.");
                return;
            }





            // 🔥 Guardar cambios
            proveedorSeleccionado.Nombre = nombre;
            proveedorSeleccionado.Telefono = telefono;
            proveedorSeleccionado.Email = email;

            // 🔥 Guardar JSON
            await _service.SaveAllAsync(proveedores);

            // 🔥 Refrescar lista
            listaProveedores = proveedores;

            ProveedoresList.ItemsSource = null;
            ProveedoresList.ItemsSource = listaProveedores;
        }

        private async void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 convertir correctamente a Guid
            var button = sender as Button;

            if (button?.Tag == null)
                return;

            // 🔥 convertir correctamente a Guid
            Guid idProveedor = (Guid)button.Tag;

            // Obtener datos
            var proveedores = await _service.GetAllAsync();
            var productos = await _ProductService.GetAllAsync();


            // Buscar proveedor
            var proveedorSeleccionado = proveedores.FirstOrDefault(c => c.IdProveedor    == idProveedor);

            if (proveedorSeleccionado == null)
                return;

            // 🔥 VALIDACIÓN
            bool tieneProductos = productos.Any(p => p.IdProveedor == idProveedor);
            if (tieneProductos)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "No se puede eliminar",
                    Content = "Este proveedor tiene productos asociados y no se puede eliminar.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }
            var dialog = new ContentDialog
            {
                Title = "Eliminar proveedor",
                Content = "¿Seguro que quieres eliminar este proveedor?",
                PrimaryButtonText = "Sí",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };


            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            // Eliminar
            proveedores.RemoveAll(c => c.IdProveedor == idProveedor);

            // Guardar cambios
            await _service.SaveAllAsync(proveedores);

            // Refrescar lista en pantalla
            listaProveedores = proveedores;
            ProveedoresList.ItemsSource = null;
            ProveedoresList.ItemsSource = listaProveedores;
        }



        public void regresar_click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ProveedoresPag));
        }
    }
}
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using piaWinUI.Models;
using piaWinUI.Services;
using piaWinUI.Helpers;
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
    public sealed partial class ProveedoresPag : Page
    {
        private readonly ProveedorService _service = new ProveedorService();
        public ProveedoresPag()
        {
            InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        
        


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
            
            var email = Email.Text;

            // validaciones basicas
            string error;

            if(!ValidationHelper.ValidarNombre(nombre, out error))
            {
                await DialogError("Error", error);
                return;
            }   
            if (!ValidationHelper.ValidarTelefono(telefono, out error))
                {
                    await DialogError("Error", error);
                    return;
                }
    
            if(!ValidationHelper.ValidarEmail(email, out error))
                {
                    await DialogError("Error", error);
                    return;
            }
            
            var provs = await _service.GetAllAsync();
             
            //ver si ya existe un tlefono registrado
            var existetelefono = provs.FirstOrDefault(c => c.Telefono == telefono);

            if (existetelefono != null)
            {
                await DialogError("Error", "El teléfono ya está registrado.");
                return;
            }
            try
            {
                var proveedores = await _service.GetAllAsync();

                var nuevo = new Proveedor
                {

                    IdProveedor = Guid.NewGuid(),
                    Nombre = nombre,
                    Telefono = telefono,
                    Email = email
                };

               proveedores.Add(nuevo);

                await _service.SaveAllAsync(proveedores);

                //limpiar campos
                Nombre.Text = string.Empty;
                Telefono.Text = string.Empty;
           
                Email.Text = string.Empty;

                //mostrar mensaje de exito
                SetStatus("Proveedor guardado exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void ReporteProveedores_Click(object sender, RoutedEventArgs e)
        {
           // Frame.Navigate(typeof(ReporteProveedoresPag));
        }

        private void BuscarProveedores_Click(object sender, RoutedEventArgs e)
        {
           Frame.Navigate(typeof(BuscarProveedoresPag));


        }
    }

}


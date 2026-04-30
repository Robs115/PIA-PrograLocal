using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.WindowsAppSDK.Runtime.Packages;
using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace piaWinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InventarioPag : Page
    {
        public InventarioPag    ()
        {
            InitializeComponent();
        }

        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            /*
            try
            {
                var clientes = await _service.GetClientesAsync();
                var nuevo = new Cliente
                {
                    //algun dia el id tiene que sacar el ultimo de la lista del json
                    Id = new Random().Next(1, 100000),
                    Nombre = Nombre.Text,
                    Telefono = Telefono.Text,
                    FechaNacimiento = FechaNacimiento.Date.DateTime,
                    Email = Email.Text
                };

                clientes.Add(nuevo);

                await _service.SaveClienteAsync(clientes);

                //limpiar campos
                Nombre.Text = string.Empty;
                Telefono.Text = string.Empty;
                FechaNacimiento.Date = DateTime.Now;
                Email.Text = string.Empty;

                //mostrar mensaje de exito
                toast.Text = "Cliente guardado exitosamente";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }


        }
            */
        private void ReportePedidos_Click(object sender, RoutedEventArgs e)
        {
           // Frame.Navigate(typeof(ReportePedidos));
        }
    }
}

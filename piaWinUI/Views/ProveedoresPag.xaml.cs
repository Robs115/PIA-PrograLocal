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
    public sealed partial class ProveedoresPag : Page
    {
        private readonly ProveedorService _service = new ProveedorService();
        private readonly ProductService _productService = new ProductService();

        private List<Proveedor> listaProveedores = new List<Proveedor>();

        public ProveedoresPag()
        {
            InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            Loaded += ProveedoresPag_Loaded;
        }

        private async void ProveedoresPag_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarProveedores();
        }

        private void Nombre_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {

            var regex = @"^(?!.*  )[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]*$";
            args.Cancel = !System.Text.RegularExpressions.Regex.IsMatch(args.NewText, regex);
        }

        private void Telefono_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            // Solo permitir números
            args.Cancel = !args.NewText.All(char.IsDigit);
        }

        private async Task CargarProveedores()
        {
            listaProveedores = await _service.GetAllAsync();
            DataGridProveedores.ItemsSource = listaProveedores;
        }

        private async Task MostrarDialog(string titulo, string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = mensaje,
                CloseButtonText = "Aceptar",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        // Guardar nuevo proveedor
        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = Nombre.Text.Trim();
            string telefono = Telefono.Text.Trim();
            string email = Email.Text.Trim();

            string error;

            // Validaciones
            if (!ValidationHelper.ValidarNombre(nombre, out error))
            {
                await MostrarDialog("Error", error);
                return;
            }

            if (!ValidationHelper.ValidarTelefono(telefono, out error))
            {
                await MostrarDialog("Error", error);
                return;
            }

            if (!ValidationHelper.ValidarEmail(email, out error))
            {
                await MostrarDialog("Error", error);
                return;
            }

            // Revisar teléfono duplicado
            var proveedoresExistentes = await _service.GetAllAsync();
            if (proveedoresExistentes.Any(p => p.Telefono == telefono))
            {
                await MostrarDialog("Error", "El teléfono ya está registrado.");
                return;
            }

            try
            {
                var nuevoProveedor = new Proveedor
                {
                    IdProveedor = Guid.NewGuid(),
                    Nombre = nombre,
                    Telefono = telefono,
                    Email = email
                };

                proveedoresExistentes.Add(nuevoProveedor);
                await _service.SaveAllAsync(proveedoresExistentes);

                toast.Text = "El proveedor se registró correctamente";

                // Limpiar campos
                Nombre.Text = string.Empty;
                Telefono.Text = string.Empty;
                Email.Text = string.Empty;

                // Refrescar lista
                await CargarProveedores();
            }
            catch (Exception ex)
            {
                await MostrarDialog("Error", $"Ocurrió un error al guardar: {ex.Message}");
            }
        }

        // Buscar proveedores
        private void Buscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = buscador.Text.Trim().ToLower();

            var filtrados = listaProveedores
                .Where(p => p.Nombre.ToLower().Contains(texto))
                .ToList();

            DataGridProveedores.ItemsSource = filtrados;
        }

        // Editar proveedor
        private async void EditarProveedor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                
                return;

            // Intentar convertir el Tag a Proveedor
            var proveedorSeleccionado = button.Tag as Proveedor;
            if (proveedorSeleccionado == null)
                
            return;

            // Dialogo de edición
            var nombreDialog = new TextBox { Text = proveedorSeleccionado.Nombre };
            nombreDialog.BeforeTextChanging += (s, args) =>
            {
                var regex = @"^(?!.*  )[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]*$";
                args.Cancel = !System.Text.RegularExpressions.Regex.IsMatch(args.NewText, regex);
            };
            var telefonoDialog = new TextBox { Text = proveedorSeleccionado.Telefono };
            telefonoDialog.BeforeTextChanging += (s, args) =>
            {
                args.Cancel = !args.NewText.All(char.IsDigit);
            };

            var emailDialog = new TextBox { Text = proveedorSeleccionado.Email };

            var dialogPanel = new StackPanel
            {
                Children = { new TextBlock { Text = "Nombre" }, nombreDialog, 
                             new TextBlock { Text = "Teléfono" }, telefonoDialog,  
                             new TextBlock { Text = "Email" }, emailDialog }
            };

            var dialog = new ContentDialog
            {
                Title = "Editar Proveedor",
                Content = dialogPanel,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            string error;
            if (!ValidationHelper.ValidarNombre(nombreDialog.Text.Trim(), out error) ||
                !ValidationHelper.ValidarTelefono(telefonoDialog.Text.Trim(), out error) ||
                !ValidationHelper.ValidarEmail(emailDialog.Text.Trim(), out error))
            {
                await MostrarDialog("Error", error);
                return;
            }

            var proveedoresExistentes = await _service.GetAllAsync();

            // Revisar duplicado
            var index = proveedoresExistentes.FindIndex(p => p.IdProveedor == proveedorSeleccionado.IdProveedor);
            if (index == -1)
            {
                await MostrarDialog("Error", "No se encontró el proveedor para actualizar.");
                return;
            }

            // Reemplazar los datos en la lista
            proveedoresExistentes[index].Nombre = nombreDialog.Text.Trim();
            proveedoresExistentes[index].Telefono = telefonoDialog.Text.Trim();
            proveedoresExistentes[index].Email = emailDialog.Text.Trim();

            await _service.SaveAllAsync(proveedoresExistentes);

            await CargarProveedores();

            await MostrarDialog("Éxito", "Proveedor actualizado correctamente.");
        }

        // Eliminar proveedor
        private async void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Proveedor proveedorSeleccionado)
                return;

            var productos = await _productService.GetAllAsync();
            if (productos.Any(p => p.IdProveedor == proveedorSeleccionado.IdProveedor))
            {
                await MostrarDialog("No se puede eliminar", "Este proveedor tiene productos asociados.");
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Eliminar proveedor",
                Content = $"¿Seguro que quieres eliminar a {proveedorSeleccionado.Nombre}?",
                PrimaryButtonText = "Sí",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var proveedoresExistentes = await _service.GetAllAsync();
            proveedoresExistentes.RemoveAll(p => p.IdProveedor == proveedorSeleccionado.IdProveedor);
            await _service.SaveAllAsync(proveedoresExistentes);

            await CargarProveedores();
        }
    }
}

















/* Codigo viejo , se dejo para referencia de validaciones y estructura de edicion, se recomienda revisar el codigo de clientes para una mejor referencia 
namespace piaWinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProveedoresPag : Page
    {
        private readonly ProveedorService _service = new ProveedorService();
        private List<Proveedor> listaProveedores = new List<Proveedor>();
        private ProductService _ProductService = new ProductService();
        private List<Producto> listaProductos = new List<Producto>();
        public ProveedoresPag()
        {
            InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }



        private async void PedidosPag_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarTodo();
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarPedidos(txtBuscar.Text);
        }

        private void FiltrarPedidos(string texto)
        {
            if (_pedidos == null) return;

            var filtrados = _pedidos
                .Where(p =>
                    p.NombreProducto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    p.NombreProveedor.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    p.Cantidad.ToString().Contains(texto)
                )
                .Select(p => new PedidoView
                {
                    NombreProducto = p.NombreProducto,
                    NombreProveedor = p.NombreProveedor,
                    Cantidad = p.Cantidad,
                    Fecha = p.Fecha // 👈 AQUÍ YA NO FORMATEAS
                })
                .ToList();

            gridPedidos.ItemsSource = filtrados;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            listaProveedores = await _service.GetAllAsync();

            // Mostrar últimos primero
           
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
                toast.Text = "El cliente se registro";

                await _service.SaveAllAsync(proveedores);

                //limpiar campos
                Nombre.Text = string.Empty;
                Telefono.Text = string.Empty;
           
                Email.Text = string.Empty;

               
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }


        private void Buscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = buscador.Text.ToLower();

            var filtrados = listaProveedores
                .Where(c => c.Nombre.ToLower().Contains(texto))
                .ToList();

           
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
        

        private async void EditarProveedor_Click(object sender, RoutedEventArgs e)
        {
          /*  var button = sender as Button;

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

            if (!ValidationHelper.ValidarNombre(nombre, out error))
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
            var existetelefono = proveedores.FirstOrDefault(c => c.Telefono == telefono);

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
            var proveedorSeleccionado = proveedores.FirstOrDefault(c => c.IdProveedor == idProveedor);

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
            
        }



        

        private void BuscarProveedores_Click(object sender, RoutedEventArgs e)
        {
           Frame.Navigate(typeof(BuscarProveedoresPag));


        }
    }

}

*/
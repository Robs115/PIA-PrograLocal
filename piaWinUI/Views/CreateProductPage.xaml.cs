using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Text.Json;
using piaWinUI.Models;
using piaWinUI.Services;

namespace piaWinUI.Views
{
    public sealed partial class CreateProductPage : Page
    {
        private readonly ProductService _service = new ProductService();

        public CreateProductPage()
        {
            InitializeComponent();
        }

        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productos = await _service.GetProductsAsync();

                var nuevo = new Producto
                {
                    Id = new Random().Next(1, 100000),
                    Nombre = txtNombre.Text,
                    Precio = decimal.Parse(txtPrecio.Text),
                    Stock = int.Parse(txtStock.Text)
                };

                productos.Add(nuevo);

                await _service.SaveProductsAsync(productos);

                Frame.GoBack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
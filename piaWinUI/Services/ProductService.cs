using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using piaWinUI.Models;
using System.Text.Json;
using System.IO;

namespace piaWinUI.Services
{
    public class ProductService
    {
        public async Task<List<Producto>> GetProductsAsync()
        {
            if (!File.Exists(App.ProductsFilePath))
                return new List<Producto>();

            using var stream = File.OpenRead(App.ProductsFilePath);
            return await JsonSerializer.DeserializeAsync<List<Producto>>(stream)
                   ?? new List<Producto>();
        }

        public async Task SaveProductsAsync(List<Producto> productos)
        {
            Directory.CreateDirectory(App.DataFolder);

            using var stream = File.Create(App.ProductsFilePath);
            await JsonSerializer.SerializeAsync(stream, productos);
        }

        public async Task DeleteProductAsync(Guid id)
        {
            var productos = await GetProductsAsync();

            var producto = productos.FirstOrDefault(p => p.Id == id);

            if (producto != null)
            {
                productos.Remove(producto);

                await SaveProductsAsync(productos);
            }
        }
    }
}

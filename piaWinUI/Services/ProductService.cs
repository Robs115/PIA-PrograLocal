using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using piaWinUI.Models;
using System.Text.Json;
using System.IO;
using piaWinUI.Helpers;

namespace piaWinUI.Services
{
    public class ProductService
        : BaseJsonService<Producto>
    {
        public ProductService()
            : base(FilePaths.Productos)
        {
        }

        public async Task AddProductoAsync(Producto producto)
        {
            var productos = await GetAllAsync();

            bool duplicado = productos.Any(p =>
                p.Nombre.Trim().ToLower() ==
                producto.Nombre.Trim().ToLower());

            if (duplicado)
                throw new Exception(
                    "Producto duplicado");

            if (producto.PrecioCompra <= 0)
                throw new Exception(
                    "Precio compra inválido");

            if (producto.PrecioVenta <= 0)
                throw new Exception(
                    "Precio venta inválido");

            if (producto.Stock < 0)
                throw new Exception(
                    "Stock inválido");

            producto.Id = Guid.NewGuid();

            productos.Add(producto);

            await SaveAllAsync(productos);
        }

        public async Task DeleteProductoAsync(Guid id)
        {
            var productos = await GetAllAsync();

            var producto =
                productos.FirstOrDefault(p => p.Id == id);

            if (producto != null)
            {
                productos.Remove(producto);

                await SaveAllAsync(productos);
            }
        }

        public async Task<List<Producto>> BuscarAsync(string texto)
        {
            var productos = await GetAllAsync();

            return productos
                .Where(p =>
                    p.Nombre.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
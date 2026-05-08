using piaWinUI.Helpers;
using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace piaWinUI.Services
{
    public class VentasService
        : BaseJsonService<Venta>
    {
        private readonly ProductService productoService;
        private readonly DetalleVentaService detalleService;

        public VentasService()
            : base(FilePaths.Ventas)
        {
            productoService = new ProductService();
            detalleService = new DetalleVentaService();
        }

        public async Task RegistrarVentaAsync(
            Venta venta,
            List<DetalleVentas> detalles)
        {
            if (detalles.Count == 0)
                throw new Exception(
                    "La venta no tiene productos");

            var productos =
                await productoService.GetAllAsync();

            foreach (var detalle in detalles)
            {
                var producto =
                    productos.FirstOrDefault(p =>
                        p.Id == detalle.IdProducto);

                if (producto == null)
                    throw new Exception(
                        $"Producto no encontrado");

                if (detalle.Cantidad <= 0)
                    throw new Exception(
                        "Cantidad inválida");

                if (detalle.Cantidad > producto.Stock)
                    throw new Exception(
                        $"Stock insuficiente para {producto.Nombre}");

                producto.Stock -= detalle.Cantidad;
            }

            venta.IdVenta = Guid.NewGuid();
            venta.Fecha = DateTime.Now;
            venta.Total = detalles.Sum(d => d.Subtotal);

            var ventas = await GetAllAsync();

            ventas.Add(venta);

            await SaveAllAsync(ventas);

            foreach (var detalle in detalles)
            {
                detalle.IdVenta = venta.IdVenta;
            }

            var todosDetalles =
                await detalleService.GetAllAsync();

            todosDetalles.AddRange(detalles);

            await detalleService.SaveAllAsync(
                todosDetalles);

            await productoService.SaveAllAsync(
                productos);
        }
    }
}
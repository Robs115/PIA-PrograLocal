using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using piaWinUI.Models;


namespace piaWinUI.Services
{
    public class PedidoService
    {
        private List<Pedidos> _pedidos = new();

        public void RegistrarPedido(string producto, string proveedor, int cantidad)
        {
            var pedido = new Pedidos
            {
                Producto = producto,     // aquí se validan automáticamente
                Proveedor = proveedor,
                Cantidad = cantidad,
                Fecha = DateTime.Now
            };

            _pedidos.Add(pedido);
        }

        public List<Pedidos> ObtenerPedidos()
        {
            return _pedidos;
        }
    }
}
using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using piaWinUI.Helpers;


namespace piaWinUI.Services
{
    public class PedidoService
        : BaseJsonService<Pedidos>
    {
        public PedidoService()
            : base(FilePaths.Pedidos)
        {
        }

        public async Task AddPedidoAsync(
            Pedidos pedido)
        {
            var pedidos = await GetAllAsync();

            pedido.Id = GenerarId(pedidos, p => p.Id);

            pedidos.Add(pedido);

            await SaveAllAsync(pedidos);
        }
    }
}
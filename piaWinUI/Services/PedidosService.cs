using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using piaWinUI.Models;
using System.Text.Json;

namespace piaWinUI.Services
{
    public class PedidoService
    {
        public async Task<List<Pedidos>> GetPedidosAsync()
        {
            if (!File.Exists(App.PedidosFilePath))
                return new List<Pedidos>();

            using var stream = File.OpenRead(App.PedidosFilePath);

            return await JsonSerializer.DeserializeAsync<List<Pedidos>>(stream)
                   ?? new List<Pedidos>();
        }

        public async Task SavePedidosAsync(List<Pedidos> pedidos)
        {
            Directory.CreateDirectory(App.DataFolder);

            var json = JsonSerializer.Serialize(pedidos, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(App.PedidosFilePath, json);
        }
    }
}
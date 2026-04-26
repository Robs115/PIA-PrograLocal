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
    public class ClienteService
    {
        public async Task<List<Cliente>> GetClientesAsync()
        {
            if (!File.Exists(App.ClientesFilePath))
                return new List<Cliente>();

            using var stream = File.OpenRead(App.ClientesFilePath);
            return await JsonSerializer.DeserializeAsync<List<Cliente>>(stream)
                   ?? new List<Cliente>();
        }

        public async Task SaveClienteAsync(List<Cliente> clientes)
        {
            Directory.CreateDirectory(App.DataFolder);

            using var stream = File.Create(App.ClientesFilePath);
            await JsonSerializer.SerializeAsync(stream, clientes);
        }
    }
}

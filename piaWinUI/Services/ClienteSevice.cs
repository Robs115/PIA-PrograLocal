using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace piaWinUI.Services
{
    public class ClienteService
    {
        public async Task<List<Cliente>> GetClientesAsync()
        {
            if (!File.Exists(App.ClientesFilePath))
                return new List<Cliente>();

            using var stream = File.OpenRead(App.ClientesFilePath);
            System.Diagnostics.Debug.WriteLine(App.ClientesFilePath);
            return await JsonSerializer.DeserializeAsync<List<Cliente>>(stream)
                   ?? new List<Cliente>();
        }

        public async Task SaveClienteAsync(List<Cliente> clientes)
        {
            Debug.WriteLine("PATH: " + App.ClientesFilePath);
            Debug.WriteLine("EXISTE: " + File.Exists(App.ClientesFilePath));
            Directory.CreateDirectory(App.DataFolder);

            var json = JsonSerializer.Serialize(clientes, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(App.ClientesFilePath, json);

            Debug.WriteLine(File.Exists(App.ClientesFilePath));
            Debug.WriteLine(File.ReadAllText(App.ClientesFilePath));
            Debug.WriteLine(">" + App.ClientesFilePath + "<");

            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/select,\"" + App.ClientesFilePath + "\"",
                UseShellExecute = true
            });

        }
    }
}

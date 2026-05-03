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
    public class ProveedorService
    {
        public async Task<List<Proveedor>> GetProveedorAsync()
        {
            if (!File.Exists(App.ProveedorFilePath))
                return new List<Proveedor>();

            using var stream = File.OpenRead(App.ProveedorFilePath);
            System.Diagnostics.Debug.WriteLine(App.ProveedorFilePath);
            return await JsonSerializer.DeserializeAsync<List<Proveedor>>(stream)
                   ?? new List<Proveedor>();
        }

        public async Task SaveProveedorAsync(List<Proveedor> proveedores)
        {
            Debug.WriteLine("PATH: " + App.ProveedorFilePath);
            Debug.WriteLine("EXISTE: " + File.Exists(App.ProveedorFilePath));
            Directory.CreateDirectory(App.DataFolder);

            var json = JsonSerializer.Serialize(proveedores, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(App.ProveedorFilePath, json);

            Debug.WriteLine(File.Exists(App.ProveedorFilePath));
            Debug.WriteLine(File.ReadAllText(App.ProveedorFilePath));
            Debug.WriteLine(">" + App.ProveedorFilePath + "<");
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/select,\"" + App.ProveedorFilePath + "\"",
                UseShellExecute = true
            });

        }
    }
}


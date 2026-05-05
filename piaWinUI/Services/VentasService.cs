using piaWinUI.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace piaWinUI.Services
{
    public class VentaService
    {
        
        public async Task<List<Venta>> GetVentasAsync()
        {
            if (!File.Exists(App.VentasFilePath))
                return new List<Venta>();

            using var stream = File.OpenRead(App.VentasFilePath);

            return await JsonSerializer.DeserializeAsync<List<Venta>>(stream)
                   ?? new List<Venta>();
        }

        public async Task SaveVentasAsync(List<Venta> ventas)
        {
            Directory.CreateDirectory(App.DataFolder);

            var json = JsonSerializer.Serialize(ventas, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(App.VentasFilePath, json);

            Debug.WriteLine("Ventas guardadas en: " + App.VentasFilePath);
        }
    }

    
}
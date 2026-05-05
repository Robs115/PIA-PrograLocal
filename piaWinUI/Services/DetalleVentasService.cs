using piaWinUI.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace piaWinUI.Services
{
    class DetalleVentasService
    {

        public async Task<List<DetalleVentas>> GetDetalleVentasAsync()
        {
            if (!File.Exists(App.DetalleVentasFilePath))
                return new List<DetalleVentas>();

            using var stream = File.OpenRead(App.DetalleVentasFilePath);

            return await JsonSerializer.DeserializeAsync<List<DetalleVentas>>(stream)
                   ?? new List<DetalleVentas>();
        }

        public async Task SaveDetalleVentasAsync(List<DetalleVentas> ventas)
        {
            Directory.CreateDirectory(App.DataFolder);

            var json = JsonSerializer.Serialize(ventas, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(App.DetalleVentasFilePath, json);

            Debug.WriteLine("Detalle guardadas en: " + App.DetalleVentasFilePath);
        }
    }
}

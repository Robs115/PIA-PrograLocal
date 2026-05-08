using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace piaWinUI.Services
{
    public abstract class BaseJsonService<T>
    {
        protected readonly string filePath;

        protected BaseJsonService(string path)
        {
            filePath = path;
        }

        public async Task<List<T>> GetAllAsync()
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<T>();

                using var stream = File.OpenRead(filePath);

                return await JsonSerializer.DeserializeAsync<List<T>>(stream)
                       ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        public async Task SaveAllAsync(List<T> data)
        {
            try
            {
                string? folder =
                    Path.GetDirectoryName(filePath);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder!);

                using var stream = File.Create(filePath);

                await JsonSerializer.SerializeAsync(
                    stream,
                    data,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error guardando datos: {ex.Message}");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace piaWinUI.Services
{
    public abstract class BaseJsonService<T>
    {
        protected readonly string filePath;
        // Candado para evitar colisiones de archivos
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        protected BaseJsonService(string path)
        {
            filePath = path;
        }

        protected int GenerarId<K>(List<K> lista, Func<K, int> selector)
        {
            return lista.Any() ? lista.Max(selector) + 1 : 1;
        }

        public async Task<List<T>> GetAllAsync()
        {
            await _semaphore.WaitAsync(); // Espera su turno
            try
            {
                if (!File.Exists(filePath)) return new List<T>();

                using var stream = File.OpenRead(filePath);
                return await JsonSerializer.DeserializeAsync<List<T>>(stream) ?? new List<T>();
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine(ex);

                throw;
            }
            finally
            {
                _semaphore.Release(); // Libera el archivo para otros
            }
        }

        public async Task SaveAllAsync(List<T> data)
        {
            await _semaphore.WaitAsync();
            try
            {
                string folder = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                // Usamos un FileStream con opciones de escritura compartida segura
                using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error de persistencia: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
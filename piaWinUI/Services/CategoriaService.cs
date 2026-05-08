using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using piaWinUI.Helpers;
using piaWinUI.Models;

namespace piaWinUI.Services
{
    public class CategoriaService
        : BaseJsonService<Categoria>
    {
        public CategoriaService()
            : base(FilePaths.Categorias)
        {
        }

        public async Task AddCategoriaAsync(Categoria categoria)
        {
            var lista = await GetAllAsync();

            bool existe = lista.Any(c =>
                c.Nombre.Trim().ToLower() ==
                categoria.Nombre.Trim().ToLower());

            if (existe)
                throw new Exception("La categoría ya existe");

            lista.Add(categoria);

            await SaveAllAsync(lista);
        }
    }
}
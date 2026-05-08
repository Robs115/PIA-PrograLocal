using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using piaWinUI.Helpers;

namespace piaWinUI.Services
{
    public class ProveedorService
        : BaseJsonService<Proveedor>
    {
        public ProveedorService()
            : base(FilePaths.Proveedores)
        {
        }

        

        public async Task AddProveedorAsync(
            Proveedor proveedor)
        {
            var proveedores = await GetAllAsync();

            bool existe = proveedores.Any(p =>
                p.Email.Trim().ToLower() ==
                proveedor.Email.Trim().ToLower());

            if (existe)
                throw new Exception(
                    "Proveedor ya registrado");

            proveedor.IdProveedor = Guid.NewGuid();

            proveedores.Add(proveedor);

            await SaveAllAsync(proveedores);
        }


    }
}
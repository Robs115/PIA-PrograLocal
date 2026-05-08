using piaWinUI.Helpers;
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
        : BaseJsonService<Cliente>
    {
        public ClienteService()
            : base(FilePaths.Clientes)
        {
        }

        public async Task AddClienteAsync(Cliente cliente)
        {
            var clientes = await GetAllAsync();

            bool existe = clientes.Any(c =>
                c.Email.Trim().ToLower() ==
                cliente.Email.Trim().ToLower());

            if (existe)
                throw new Exception(
                    "Ya existe un cliente con ese correo");

            cliente.Id = Guid.NewGuid();

            clientes.Add(cliente);

            await SaveAllAsync(clientes);
        }
    }
}
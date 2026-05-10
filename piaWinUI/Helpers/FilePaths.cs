using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace piaWinUI.Helpers
{
    public static class FilePaths
    {
        public static readonly string DataFolder =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data");

        public static readonly string Clientes =
            Path.Combine(DataFolder, "clientes.json");

        public static readonly string Productos =
            Path.Combine(DataFolder, "productos.json");

        public static readonly string Proveedores =
            Path.Combine(DataFolder, "proveedores.json");

        public static readonly string Ventas =
            Path.Combine(DataFolder, "ventas.json");

        public static readonly string DetalleVentas =
            Path.Combine(DataFolder, "detalleventas.json");

        public static readonly string Pedidos =
            Path.Combine(DataFolder, "pedidos.json");

        public static readonly string Categorias =
            Path.Combine(DataFolder, "categorias.json");

        public static readonly string Users =
            Path.Combine(DataFolder, "users.json");
    }
}
using piaWinUI.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using piaWinUI.Helpers;


namespace piaWinUI.Services
{
    public class DetalleVentaService
        : BaseJsonService<DetalleVentas>
    {
        public DetalleVentaService()
            : base(FilePaths.DetalleVentas)
        {
        }
    }
}

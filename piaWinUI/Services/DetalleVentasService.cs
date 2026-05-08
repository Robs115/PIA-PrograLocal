using piaWinUI.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using piaWinUI.Helpers;


namespace piaWinUI.Services
{
    public class DetalleVentasService
        : BaseJsonService<DetalleVentas>
    {
        public DetalleVentasService()
            : base(FilePaths.DetalleVentas)
        {
        }
    }
}

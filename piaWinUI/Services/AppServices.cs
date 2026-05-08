using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace piaWinUI.Services
{
    public static class AppServices
    {
        public static PedidoService Pedido { get; } = new PedidoService();
    }
}

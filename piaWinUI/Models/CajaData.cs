using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace piaWinUI.Models
{
    public class CajaData
    {
        public bool CajaAbierta { get; set; }

        public decimal Saldo { get; set; }

        public List<MovimientoCaja> Movimientos { get; set; } = new();

        public List<CorteCaja> Cortes { get; set; } = new();
    }
}

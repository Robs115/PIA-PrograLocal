using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace piaWinUI.Models
{
    public class MovimientoCaja
    {
        public DateTime Fecha { get; set; }

        public string? Tipo { get; set; }
        public string? Concepto { get; set; }

        public decimal Monto { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace piaWinUI.Models
{
    public class CorteCaja
    {
        public DateTime Fecha { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal Esperado { get; set; }
        public decimal Contado { get; set; }
        public decimal Diferencia { get; set; }
    }
}
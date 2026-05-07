using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace piaWinUI.Models
{
    public class MovimientoCaja
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime Fecha { get; set; } = DateTime.Now;

        public string Tipo { get; set; } = "";

        public string Concepto { get; set; } = "";

        public decimal Monto { get; set; }

        public string Origen { get; set; } = "Manual";

        public string Referencia { get; set; } = "";

        public bool EsAutomatico { get; set; }
    }
}
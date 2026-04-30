using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace piaWinUI.Services
{
    public class CajaService
    {
        private List<MovimientoCaja> _movimientos = new();
        private List<CorteCaja> _cortes = new();

        public bool CajaAbierta { get; private set; }
        public decimal Saldo { get; private set; }

        public void AbrirCaja(decimal montoInicial)
        {
            if (CajaAbierta)
                throw new Exception("La caja ya está abierta");

            CajaAbierta = true;
            Saldo = montoInicial;
            _movimientos.Clear(); // nuevo turno
        }

        public void CerrarCaja()
        {
            if (!CajaAbierta)
                throw new Exception("La caja ya está cerrada");

            CajaAbierta = false;
        }

        public void RegistrarMovimiento(string tipo, decimal monto, string concepto)
        {
            if (!CajaAbierta)
                throw new Exception("No puedes registrar movimientos con la caja cerrada");

            if (monto <= 0)
                throw new Exception("Monto inválido");

            var mov = new MovimientoCaja
            {
                Fecha = DateTime.Now,
                Tipo = tipo,
                Concepto = concepto,
                Monto = monto
            };

            _movimientos.Add(mov);

            if (tipo == "Ingreso")
                Saldo += monto;
            else
                Saldo -= monto;
        }

        public List<MovimientoCaja> ObtenerMovimientos() => _movimientos;

        public List<CorteCaja> ObtenerHistorialCortes() => _cortes;

        public (decimal ingresos, decimal egresos, decimal esperado, decimal diferencia)
            CalcularCorte(decimal conteo)
        {
            var ingresos = _movimientos
                .Where(m => m.Tipo == "Ingreso")
                .Sum(m => m.Monto);

            var egresos = _movimientos
                .Where(m => m.Tipo == "Egreso")
                .Sum(m => m.Monto);

            var esperado = Saldo;
            var diferencia = conteo - esperado;

            return (ingresos, egresos, esperado, diferencia);
        }

        public string GuardarCorte(decimal conteo)
        {
            var corte = CalcularCorte(conteo);

            var nuevoCorte = new CorteCaja
            {
                Fecha = DateTime.Now,
                Ingresos = corte.ingresos,
                Egresos = corte.egresos,
                Esperado = corte.esperado,
                Contado = conteo,
                Diferencia = corte.diferencia
            };

            _cortes.Add(nuevoCorte);

            // 🔥 ALERTA INTELIGENTE
            if (Math.Abs(corte.diferencia) > 100)
                return " Diferencia alta detectada";

            return " Corte correcto";
        }
    }
}
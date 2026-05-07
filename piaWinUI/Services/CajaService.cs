using piaWinUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace piaWinUI.Services
{
    public class CajaService
    {
        private List<MovimientoCaja> _movimientos = new();

        private List<CorteCaja> _cortes = new();

        private bool _corteRealizado = false;

        public bool CajaAbierta { get; private set; }

        public decimal Saldo { get; private set; }

        public async Task LoadCajaAsync()
        {
            if (!File.Exists(App.CajaFilePath))
                return;

            var json =
                await File.ReadAllTextAsync(App.CajaFilePath);

            var data =
                JsonSerializer.Deserialize<CajaData>(json);

            if (data == null)
                return;

            CajaAbierta = data.CajaAbierta;

            Saldo = data.Saldo;

            _movimientos = data.Movimientos ?? new();

            _cortes = data.Cortes ?? new();
        }

        public async Task SaveCajaAsync()
        {
            Directory.CreateDirectory(App.DataFolder);

            var data = new CajaData
            {
                CajaAbierta = CajaAbierta,
                Saldo = Saldo,
                Movimientos = _movimientos,
                Cortes = _cortes
            };

            var json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(
                App.CajaFilePath,
                json);
        }

        public async Task AbrirCaja(decimal montoInicial)
        {
            if (CajaAbierta)
                throw new Exception(
                    "La caja ya está abierta");

            if (montoInicial < 0)
                throw new Exception(
                    "Monto inválido");

            CajaAbierta = true;

            Saldo = montoInicial;

            _movimientos.Clear();

            _corteRealizado = false;

            await SaveCajaAsync();
        }

        public async Task CerrarCaja()
        {
            if (!CajaAbierta)
                throw new Exception(
                    "La caja ya está cerrada");

            if (!_corteRealizado)
                throw new Exception(
                    "Debes realizar el corte");

            CajaAbierta = false;

            await SaveCajaAsync();
        }

        private async Task AgregarMovimiento(
            MovimientoCaja mov)
        {
            _movimientos.Add(mov);

            if (mov.Tipo == "Ingreso")
                Saldo += mov.Monto;
            else
                Saldo -= mov.Monto;

            await SaveCajaAsync();
        }

        public async Task RegistrarMovimiento(
            string tipo,
            decimal monto,
            string concepto)
        {
            if (!CajaAbierta)
                throw new Exception(
                    "Caja cerrada");

            if (string.IsNullOrWhiteSpace(concepto))
                throw new Exception(
                    "Ingresa concepto");

            if (monto <= 0)
                throw new Exception(
                    "Monto inválido");

            if (tipo == "Egreso" &&
                monto > Saldo)
            {
                throw new Exception(
                    "Saldo insuficiente");
            }

            var mov = new MovimientoCaja
            {
                Fecha = DateTime.Now,
                Tipo = tipo,
                Concepto = concepto,
                Monto = monto,
                Origen = "Manual",
                EsAutomatico = false
            };

            await AgregarMovimiento(mov);
        }

        public async Task RegistrarVenta(Venta venta)
        {
            if (!CajaAbierta)
                return;

            bool existe = _movimientos.Any(x =>
                x.Referencia ==
                venta.IdVenta.ToString()
                &&
                x.Origen == "Ventas");

            if (existe)
                return;

            var mov = new MovimientoCaja
            {
                Fecha = DateTime.Now,
                Tipo = "Ingreso",
                Concepto = "Venta realizada",
                Monto = venta.Total,
                Origen = "Ventas",
                Referencia = venta.IdVenta.ToString(),
                EsAutomatico = true
            };

            await AgregarMovimiento(mov);
        }

        public async Task RegistrarPedido(
            Pedidos pedido,
            decimal costo)
        {
            if (!CajaAbierta)
                return;

            if (costo <= 0)
                throw new Exception(
                    "Costo inválido");

            if (costo > Saldo)
                throw new Exception(
                    "Saldo insuficiente");

            bool existe = _movimientos.Any(x =>
                x.Referencia ==
                pedido.Id.ToString()
                &&
                x.Origen == "Pedidos");

            if (existe)
                return;

            var mov = new MovimientoCaja
            {
                Fecha = DateTime.Now,
                Tipo = "Egreso",
                Concepto =
                    $"Pedido: {pedido.NombreProducto}",
                Monto = costo,
                Origen = "Pedidos",
                Referencia = pedido.Id.ToString(),
                EsAutomatico = true
            };

            await AgregarMovimiento(mov);
        }

        public List<MovimientoCaja>
            ObtenerMovimientos()
        {
            return _movimientos
                .OrderByDescending(x => x.Fecha)
                .ToList();
        }

        public List<CorteCaja>
            ObtenerHistorialCortes()
        {
            return _cortes
                .OrderByDescending(x => x.Fecha)
                .ToList();
        }

        public (
            decimal ingresos,
            decimal egresos,
            decimal esperado,
            decimal diferencia)
            CalcularCorte(decimal conteo)
        {
            var ingresos =
                _movimientos
                .Where(x => x.Tipo == "Ingreso")
                .Sum(x => x.Monto);

            var egresos =
                _movimientos
                .Where(x => x.Tipo == "Egreso")
                .Sum(x => x.Monto);

            var esperado = Saldo;

            var diferencia =
                conteo - esperado;

            return (
                ingresos,
                egresos,
                esperado,
                diferencia);
        }

        public async Task<string>
            GuardarCorte(decimal conteo)
        {
            if (_corteRealizado)
            {
                throw new Exception(
                    "El corte ya fue realizado");
            }

            var corte =
                CalcularCorte(conteo);

            var nuevo = new CorteCaja
            {
                Fecha = DateTime.Now,
                Ingresos = corte.ingresos,
                Egresos = corte.egresos,
                Esperado = corte.esperado,
                Contado = conteo,
                Diferencia = corte.diferencia
            };

            _cortes.Add(nuevo);

            _corteRealizado = true;

            await SaveCajaAsync();

            return "Corte guardado correctamente";
        }
    }
}
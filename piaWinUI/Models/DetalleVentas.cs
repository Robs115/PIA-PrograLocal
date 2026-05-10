using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace piaWinUI.Models
{
    public class DetalleVentas : INotifyPropertyChanged
    {
        private int idVenta;
        private int idProducto;
        private string nombreProducto;
        private decimal precioUnitario;
        private int cantidad;
        public event Action<string> OnError;
        public int StockDisponible { get; set; }
        public bool TieneError { get; private set; }


        public int IdVenta
        {
            get => idVenta;
            set
            {
                idVenta = value;
                OnPropertyChanged(nameof(IdVenta));
            }
        }

        public int IdProducto
        {
            get => idProducto;
            set
            {
                idProducto = value;
                OnPropertyChanged(nameof(IdProducto));
            }
        }

        public string NombreProducto
        {
            get => nombreProducto;
            set
            {
                nombreProducto = value;
                OnPropertyChanged(nameof(NombreProducto));
            }
        }

        public decimal PrecioUnitario
        {
            get => precioUnitario;
            set
            {
                precioUnitario = value;
                OnPropertyChanged(nameof(PrecioUnitario));
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public int Cantidad
        {
            get => cantidad;
            set
            {
                if (value < 1)
                {
                    cantidad = 1;
                    TieneError = false;
                    OnPropertyChanged(nameof(Cantidad));
                    OnPropertyChanged(nameof(Subtotal));
                    return;
                }

                if (value > StockDisponible)
                {
                    TieneError = true;

                    // 1. Corregimos internamente regresando al stock máximo que sí es válido
                    cantidad = StockDisponible;

                    // 2. ¡MUY IMPORTANTE! Le avisamos a la interfaz que el valor cambió
                    // para que limpie el TextBox y no intente re-enviar el error al perder el foco
                    OnPropertyChanged(nameof(Cantidad));
                    OnPropertyChanged(nameof(Subtotal));

                    // 3. Disparamos la alerta de error
                    OnError?.Invoke($"Stock insuficiente. Disponible: {StockDisponible}");

                    return;
                }

                TieneError = false;
                cantidad = value;

                OnPropertyChanged(nameof(Cantidad));
                OnPropertyChanged(nameof(Subtotal));
            }
        }



        // 🔥 Calculado automáticamente
        public decimal Subtotal => PrecioUnitario * Cantidad;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }
}
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
        private Guid idVenta;
        private Guid idProducto;
        private string nombreProducto;
        private decimal precioUnitario;
        private int cantidad;

        public Guid IdVenta
        {
            get => idVenta;
            set
            {
                idVenta = value;
                OnPropertyChanged(nameof(IdVenta));
            }
        }

        public Guid IdProducto
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
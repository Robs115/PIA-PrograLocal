using System;
using System.ComponentModel;

namespace piaWinUI.Models
{
    public class Venta : INotifyPropertyChanged
    {
        private Guid idVenta = Guid.NewGuid();
        private Guid idUsuario;
        private Guid idCliente;
        private decimal total;
        private DateTime fecha;

        public Guid IdVenta
        {
            get => idVenta;
            set
            {
                idVenta = value;
                OnPropertyChanged(nameof(IdVenta));
            }
        }

        public Guid IdUsuario
        {
            get => idUsuario;
            set
            {
                idUsuario = value;
                OnPropertyChanged(nameof(IdUsuario));
            }
        }

        public Guid IdCliente
        {
            get => idCliente;
            set
            {
                idCliente = value;
                OnPropertyChanged(nameof(IdCliente));
            }
        }

        public decimal Total
        {
            get => total;
            set
            {
                total = value;
                OnPropertyChanged(nameof(Total));
            }
        }

        public DateTime Fecha
        {
            get => fecha;
            set
            {
                fecha = value;
                OnPropertyChanged(nameof(Fecha));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}

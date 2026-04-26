using System;
using System.ComponentModel;

namespace piaWinUI.Models
{
    public class Venta : INotifyPropertyChanged
    {
        private int idVenta;
        private int idUsuario;
        private int idCliente;
        private double total;
        private DateTime fecha;

        public int IdVenta
        {
            get => idVenta;
            set
            {
                idVenta = value;
                OnPropertyChanged(nameof(IdVenta));
            }
        }

        public int IdUsuario
        {
            get => idUsuario;
            set
            {
                idUsuario = value;
                OnPropertyChanged(nameof(IdUsuario));
            }
        }

        public int IdCliente
        {
            get => idCliente;
            set
            {
                idCliente = value;
                OnPropertyChanged(nameof(IdCliente));
            }
        }

        public double Total
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
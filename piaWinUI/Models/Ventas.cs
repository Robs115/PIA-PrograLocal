using System;
using System.ComponentModel;

namespace piaWinUI.Models
{
    public class Venta : INotifyPropertyChanged
    {
        private int id;
        private int idUsuario;
        private string metodopago;
        private decimal total;
        private DateTime fecha;

        public int Id
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged(nameof(Id));
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

        public string MetodoPago
        {
            get => metodopago;
            set
            {
                metodopago = value;
                OnPropertyChanged(nameof(metodopago));
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

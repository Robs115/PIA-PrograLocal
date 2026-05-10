using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace piaWinUI.Models
{
    public class Venta : INotifyPropertyChanged
    {
        private int id;
        private string userName;
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

        public string UserName
        {
            get => userName;
            set
            {
                userName = value;
                OnPropertyChanged(nameof(userName));
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
        public List<DetalleVentas> Detalles { get; set; } = new List<DetalleVentas>();
        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}

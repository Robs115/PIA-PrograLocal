using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel;

namespace piaWinUI.Models
{
    public class Pedidos : INotifyPropertyChanged
    {
        private string producto = "";
        private string proveedor = "";
        private int cantidad;
        private DateTime fecha;

        public string Producto
        {
            get => producto;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new Exception("Debes seleccionar un producto");

                producto = value;
                OnPropertyChanged(nameof(Producto));
            }
        }

        public string Proveedor
        {
            get => proveedor;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new Exception("Debes seleccionar un proveedor");

                proveedor = value;
                OnPropertyChanged(nameof(Proveedor));
            }
        }

        public int Cantidad
        {
            get => cantidad;
            set
            {
                if (value <= 0)
                    throw new Exception("La cantidad debe ser mayor a 0");

                if (value > 1000)
                    throw new Exception("Cantidad demasiado alta (posible error)");

                cantidad = value;
                OnPropertyChanged(nameof(Cantidad));
            }
        }

        public DateTime Fecha
        {
            get => fecha;
            set
            {
                if (value > DateTime.Now)
                    throw new Exception("La fecha no puede ser futura");

                fecha = value;
                OnPropertyChanged(nameof(Fecha));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}
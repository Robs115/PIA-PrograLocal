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
        private Guid id;
        private Guid idProducto;
        private Guid idProveedor;

        private string nombreProducto = "";
        private string nombreProveedor = "";

        private int cantidad;
        private DateTime fecha;

        // 🔑 ID único del pedido
        public Guid Id
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        // 🔗 Relación real con producto
        public Guid IdProducto
        {
            get => idProducto;
            set
            {
                if (value == Guid.Empty)
                    throw new Exception("Producto inválido");

                idProducto = value;
                OnPropertyChanged(nameof(IdProducto));
            }
        }

        // 🔗 Relación real con proveedor
        public Guid IdProveedor
        {
            get => idProveedor;
            set
            {
                if (value == Guid.Empty)
                    throw new Exception("Proveedor inválido");

                idProveedor = value;
                OnPropertyChanged(nameof(IdProveedor));
            }
        }

        // 🧾 Solo para mostrar en UI
        public string NombreProducto
        {
            get => nombreProducto;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new Exception("Nombre de producto requerido");

                nombreProducto = value;
                OnPropertyChanged(nameof(NombreProducto));
            }
        }

        public string NombreProveedor
        {
            get => nombreProveedor;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new Exception("Nombre de proveedor requerido");

                nombreProveedor = value;
                OnPropertyChanged(nameof(NombreProveedor));
            }
        }

        public int Cantidad
        {
            get => cantidad;
            set
            {
                if (value <= 0)
                    throw new Exception("Cantidad debe ser mayor a 0");

                if (value > 1000)
                    throw new Exception("Cantidad sospechosamente alta");

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

        protected void OnPropertyChanged(string nombre)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
        }
    }
}
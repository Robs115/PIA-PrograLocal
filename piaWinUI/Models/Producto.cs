using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace piaWinUI.Models
{
    public class Producto : INotifyPropertyChanged
    {
        private Guid id;
        private string nombre;
        private string descripcion;
        private decimal precioCompra;
        private decimal precioVenta;
        private Guid idProveedor;
        private string categoria;
        private int stock;

        public Guid Id
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Nombre
        {
            get => nombre;
            set
            {
                nombre = value;
                OnPropertyChanged(nameof(Nombre));
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value;
                OnPropertyChanged(nameof(Descripcion));
            }
        }

        public decimal PrecioCompra
        {
            get => precioCompra;
            set
            {
                precioCompra = value;
                OnPropertyChanged(nameof(PrecioCompra));
                OnPropertyChanged(nameof(ValorInventario));
            }
        }

        public decimal PrecioVenta
        {
            get => precioVenta;
            set
            {
                precioVenta = value;
                OnPropertyChanged(nameof(PrecioVenta));
            }
        }

        public Guid IdProveedor
        {
            get => idProveedor;
            set
            {
                idProveedor = value;
                OnPropertyChanged(nameof(IdProveedor));
            }
        }

        public string Categoria
        {
            get => categoria;
            set
            {
                categoria = value;
                OnPropertyChanged(nameof(Categoria));
            }
        }

        public int Stock
        {
            get => stock;
            set
            {
                stock = value;
                OnPropertyChanged(nameof(Stock));
                OnPropertyChanged(nameof(ValorInventario));
            }
        }

        // Computed property (updates automatically in UI)
        public decimal ValorInventario => PrecioCompra * Stock;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}


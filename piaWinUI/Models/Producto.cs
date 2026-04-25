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
        private int id;
        private string nombre;
        private decimal precio;
        private int stock;

        public int Id
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

        public decimal Precio
        {
            get => precio;
            set
            {
                precio = value;
                OnPropertyChanged(nameof(Precio));
                OnPropertyChanged(nameof(ValorInventario));
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
        public decimal ValorInventario => Precio * Stock;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace piaWinUI.Models
{
    public class Proveedor : INotifyPropertyChanged
    {
        private Guid idProveedor;
        private string nombre = "";
        private string telefono = "";

        public Guid IdProveedor
        {
            get => idProveedor;
            set
            {
                idProveedor = value;
                OnPropertyChanged(nameof(IdProveedor));
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

        public string Telefono
        {
            get => telefono;
            set
            {
                telefono = value;
                OnPropertyChanged(nameof(Telefono));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}
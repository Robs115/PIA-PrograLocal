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
        public int id;
        private string nombre = "";
        private string telefono = "";
        private string email = "";

        public int Id
        {
            get => Id;
            set
            {
                Id = value;
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

        public string Email         {
            get => email;
            set
            {
                email = value;
                OnPropertyChanged(nameof(Email));
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
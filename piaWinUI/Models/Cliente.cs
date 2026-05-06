using System;
using System.ComponentModel;

namespace piaWinUI.Models
{
    public class Cliente : INotifyPropertyChanged
    {
        private Guid id;
        private string nombre;
        private string telefono;
        private DateTime fechanacimiento;
        private string email;

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

        public string Telefono
        {
            get => telefono;
            set
            {
                telefono = value;
                OnPropertyChanged(nameof(Telefono));
            }
        }

        public DateTime FechaNacimiento
        {
            get => fechanacimiento;
            set
            {
                fechanacimiento = value;
                OnPropertyChanged(nameof(FechaNacimiento));
            }
        }

        public string Email
        {
            get => email;
            set
            {
                email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}
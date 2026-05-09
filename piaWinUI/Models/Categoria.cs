using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace piaWinUI.Models
{
    public class Categoria : INotifyPropertyChanged
    {
        private string nombre;

        public string Nombre
        {
            get => nombre;
            set
            {
                nombre = value;
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(Nombre)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
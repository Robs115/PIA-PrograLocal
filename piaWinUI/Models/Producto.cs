using System;
using System.ComponentModel;
using System.IO;

namespace piaWinUI.Models
{
    public class Producto : INotifyPropertyChanged
    {
        private int id;
        private string codigobarras;
        private string nombre;
        private string descripcion;
        private decimal precioCompra;
        private decimal precioVenta;
        private int idProveedor;
        private string categoria;
        private int stock;
        private string imagenPath;

        public int Id
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(ImagenCompleta));
            }
        }

        public string CodigoBarras
        {
            get => codigobarras;
            set
            {
                codigobarras = value;
                OnPropertyChanged(nameof(CodigoBarras));
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

        public int IdProveedor
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

        public string ImagenPath
        {
            get => imagenPath;
            set
            {
                imagenPath = value;
                OnPropertyChanged(nameof(ImagenPath));
                OnPropertyChanged(nameof(ImagenCompleta));
            }
        }

        // 🔥 ESTA ES LA CLAVE
        public string ImagenCompleta
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ImagenPath))
                        return "ms-appx:///Assets/StoreLogo.png";

                    string rutaCompleta = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        ImagenPath);

                    if (!File.Exists(rutaCompleta))
                        return "ms-appx:///Assets/StoreLogo.png";

                    return new Uri(rutaCompleta).AbsoluteUri;
                }
                catch
                {
                    return "ms-appx:///Assets/StoreLogo.png";
                }
            }
        }
        public bool TieneImagen
        {
            get
            {
                return ImagenCompleta != "ms-appx:///Assets/StoreLogo.png";
            }
        }
        public decimal ValorInventario => PrecioCompra * Stock;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}
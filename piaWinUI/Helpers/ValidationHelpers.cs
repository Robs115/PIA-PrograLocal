using System;
using System.Text.RegularExpressions;
using Windows.Foundation.Diagnostics;

namespace piaWinUI.Helpers
{
    public static class ValidationHelper
    {

        public static bool ValidarNombre(string nombre, out string error)
        {
            error = "";

            nombre = nombre?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                error = "El nombre es obligatorio.";
                return false;
            }

            if (nombre.Length < 3)
            {
                error = "El nombre debe tener mínimo 3 caracteres.";
                return false;
            }

            if (nombre.Length > 50)
            {
                error = "El nombre es demasiado largo.";
                return false;
            }

            // Solo letras y espacios
            if (!Regex.IsMatch(nombre, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
            {
                error = "El nombre solo debe contener letras.";
                return false;
            }

            return true;
        }

        // 🔥 TELÉFONO
        public static bool ValidarTelefono(string telefono, out string error)
        {
            error = "";

            telefono = telefono?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(telefono))
            {
                error = "El teléfono es obligatorio.";
                return false;
            }

            if (!Regex.IsMatch(telefono, @"^\d+$"))
            {
                error = "El teléfono solo debe contener números.";
                return false;
            }

            if (telefono.Length != 10)
            {
                error = "El teléfono debe tener 10 dígitos.";
                return false;
            }

            return true;
        }

        // 🔥 EMAIL
        public static bool ValidarEmail(string email, out string error)
        {
            error = "";

            email = email?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                error = "El email es obligatorio.";
                return false;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);

                if (addr.Address != email)
                {
                    error = "Email inválido.";
                    return false;
                }
            }
            catch
            {
                error = "Formato de email inválido.";
                return false;
            }

            return true;
        }
        public static bool ValidarFechaNacimiento(DateTime fecha, out string error)
        {
            error = "";

            var hoy = DateTime.Today;

            // 🔥 No futura
            if (fecha > hoy)
            {
                error = "La fecha no puede ser futura.";
                return false;
            }

            // 🔥 Edad
            int edad = hoy.Year - fecha.Year;

            if (fecha.Date > hoy.AddYears(-edad))
                edad--;

            // 🔥 Muy joven
            if (edad < 18)
            {
                error = "La persona debe ser mayor de edad.";
                return false;
            }

            // 🔥 Muy viejo (evitar fechas absurdas)
            if (edad > 120)
            {
                error = "La fecha ingresada no es válida.";
                return false;
            }

            return true;
        }
      
    }
}

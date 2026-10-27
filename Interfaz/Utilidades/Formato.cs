using System.Globalization;
using BackendLogica.Modelos;

namespace Interfaz.Utilidades
{
    /// <summary>
    /// Formatos de fechas, precios y horarios en español de México. BackendLogica entrega datos
    /// crudos (DateTime, decimal); aquí se convierten a texto de interfaz.
    /// </summary>
    public static class Formato
    {
        private static readonly string[] Meses = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];
        private static readonly string[] MesesLargos = ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio",
            "agosto", "septiembre", "octubre", "noviembre", "diciembre"];
        private static readonly CultureInfo Mexico = CultureInfo.GetCultureInfo("es-MX");

        public enum EstiloPrecio { Rango, Desde }

        /// <summary>("18", "may") para la etiqueta de fecha de las tarjetas.</summary>
        public static (string Dia, string Mes) DiaYMes(DateTime fecha) => (fecha.Day.ToString(), Meses[fecha.Month - 1]);

        /// <summary>
        /// Rango de fechas compacto:
        /// mismo día → "6 nov" · mismo mes → "23–25 oct" · distinto mes → "16 oct – 18 dic".
        /// </summary>
        public static string RangoFechas(DateTime inicio, DateTime? fin = null)
        {
            DateTime a = inicio, b = fin ?? inicio;
            bool mismoMes = a.Month == b.Month && a.Year == b.Year;
            if (a.Date == b.Date) return $"{a.Day} {Meses[a.Month - 1]}";
            if (mismoMes) return $"{a.Day}–{b.Day} {Meses[a.Month - 1]}";
            return $"{a.Day} {Meses[a.Month - 1]} – {b.Day} {Meses[b.Month - 1]}";
        }

        /// <summary>"1,450"</summary>
        public static string Numero(decimal cantidad) => Math.Round(cantidad).ToString("#,0", Mexico);

        /// <summary>"$1,450"</summary>
        public static string Dinero(decimal cantidad) => $"${Numero(cantidad)}";

        /// <summary>
        /// Texto del precio: <see cref="EstiloPrecio.Rango"/> → "$600–850" · <see cref="EstiloPrecio.Desde"/> → "Desde $600".
        /// </summary>
        /// <returns>"" si no hay dato de precio.</returns>
        public static string Precio(decimal? min, decimal? max, EstiloPrecio estilo = EstiloPrecio.Rango)
        {
            if (min == null && max == null) return "";
            decimal bajo = (min ?? max)!.Value;
            decimal alto = (max ?? min)!.Value;
            if (alto == 0) return "Gratis";
            if (estilo == EstiloPrecio.Desde) return $"Desde {Dinero(bajo)}";
            if (bajo == alto) return Dinero(bajo);
            return $"{Dinero(bajo)}–{Numero(alto)}";
        }

        /// <summary>Fecha de un evento: rango compacto o "Fecha por confirmar".</summary>
        public static string FechaEvento(EventoTarjeta e) =>
            e.FechaPorConfirmar ? "Fecha por confirmar" : RangoFechas(e.Inicio, e.Fin);

        /// <summary>Precio de un evento: "$600–850", "Gratis" o "Precio por confirmar".</summary>
        public static string PrecioEvento(EventoTarjeta e, EstiloPrecio estilo = EstiloPrecio.Rango) =>
            e.PrecioPorConfirmar ? "Precio por confirmar" : Precio(e.PrecioMin, e.PrecioMax, estilo);

        /// <summary>Precio de un lugar: "$100–200", "Gratis" o "" si no hay dato.</summary>
        public static string PrecioLugar(LugarTarjeta l, EstiloPrecio estilo = EstiloPrecio.Rango) =>
            Precio(l.PrecioMin, l.PrecioMax, estilo);

        /// <summary>
        /// Fecha escrita completa de un evento (resumen de los modales "¿Vas a ir?" y "¿Cancelar tu
        /// inscripción?"): "6 de noviembre" · "23, 24 y 25 de octubre" · "Del 1 al 29 de noviembre" ·
        /// "Del 16 de octubre al 18 de diciembre" · "Fecha por confirmar".
        /// </summary>
        public static string FechaLarga(EventoTarjeta e)
        {
            if (e.FechaPorConfirmar) return "Fecha por confirmar";
            DateTime a = e.Inicio, b = e.Fin;
            int dias = (b.Date - a.Date).Days;
            bool mismoMes = a.Month == b.Month && a.Year == b.Year;
            string mesA = MesesLargos[a.Month - 1];

            if (dias <= 0) return $"{a.Day} de {mesA}";
            if (mismoMes && dias <= 2)
            {
                var lista = Enumerable.Range(a.Day, dias + 1).ToList();
                return $"{string.Join(", ", lista.SkipLast(1))} y {lista[^1]} de {mesA}";
            }
            if (mismoMes) return $"Del {a.Day} al {b.Day} de {mesA}";
            return $"Del {a.Day} de {mesA} al {b.Day} de {MesesLargos[b.Month - 1]}";
        }

        /// <summary>Hora con minutos, como en el dato "Horario" del detalle: 14:00 → "2:00 p.m.", 0:30 → "12:30 a.m."</summary>
        public static string HoraCompleta(DateTime fecha)
        {
            int h12 = fecha.Hour % 12 == 0 ? 12 : fecha.Hour % 12;
            return $"{h12}:{fecha.Minute:00} {(fecha.Hour < 12 ? "a.m." : "p.m.")}";
        }

        /// <summary>"2:00 p.m. a 9:00 p.m." para el dato "Horario" del detalle; "" con fecha por confirmar.</summary>
        public static string HorarioEvento(EventoTarjeta e) =>
            e.FechaPorConfirmar ? "" : $"{HoraCompleta(e.Inicio)} a {HoraCompleta(e.Fin)}";

        /// <summary>Tiempo transcurrido: "5 min" · "6 h" · "1 día" · "2 días" (métrica "Espera más larga").</summary>
        public static string Duracion(TimeSpan tiempo) => tiempo switch
        {
            { TotalHours: < 1 } => $"{Math.Max(1, (int)tiempo.TotalMinutes)} min",
            { TotalDays: < 1 } => $"{(int)tiempo.TotalHours} h",
            _ => (int)tiempo.TotalDays == 1 ? "1 día" : $"{(int)tiempo.TotalDays} días",
        };

        /// <summary>"Hace 6 h", "Hace 2 días" (bandeja de moderación).</summary>
        public static string Hace(DateTime fecha) => $"Hace {Duracion(DateTime.Now - fecha)}";

        /// <summary>"7 oct a las 18:42" ("Enviado el ..." de Revisar evento).</summary>
        public static string FechaYHora(DateTime fecha) => $"{RangoFechas(fecha)} a las {fecha:HH:mm}";

        /// <summary>18:00 → "6 p.m." · 21:30 → "9:30 p.m." · 00:00 → "12 a.m."</summary>
        public static string Hora(TimeSpan valor)
        {
            int h = valor.Hours;
            string sufijo = h < 12 ? "a.m." : "p.m.";
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return valor.Minutes != 0 ? $"{h12}:{valor.Minutes:00} {sufijo}" : $"{h12} {sufijo}";
        }

        /// <summary>Estado de apertura de un lugar ("Abierto hasta 9 p.m.", "Abre a las 10 a.m.", "Cerrado").</summary>
        public static (string Texto, bool Abierto) HorarioLugar(EstadoHorario? estado)
        {
            if (estado is { Abierto: true, Hasta: { } hasta }) return ($"Abierto hasta {Hora(hasta)}", true);
            if (estado?.AbreA is { } abre) return ($"Abre a las {Hora(abre)}", false);
            return ("Cerrado", false);
        }

        /// <summary>Calificación con un decimal: 4.8</summary>
        public static string Calificacion(decimal? valor) =>
            valor == null ? "" : valor.Value.ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>Nombre del lugar o, si el evento es en la calle, su dirección.</summary>
        public static string? UbicacionEvento(EventoTarjeta e) => e.LugarNombre ?? e.Direccion;

        /// <summary>Texto bajo el mini mapa, como en Figma: "Forum Buenavista, Chihuahua".</summary>
        public static string DireccionConCiudad(params string?[] partes)
        {
            string texto = string.Join(", ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
            if (texto.Length == 0) return "Ubicación por confirmar";
            return texto.Contains("chihuahua", StringComparison.OrdinalIgnoreCase) ? texto : $"{texto}, Chihuahua";
        }
    }
}

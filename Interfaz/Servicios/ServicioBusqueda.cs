using System.Globalization;
using BackendLogica.Contratos;
using BackendLogica.Modelos;
using Interfaz.Utilidades;

namespace Interfaz.Servicios
{
    /// <summary>
    /// "Resultados de búsqueda" (Paginas/Buscar.razor) y los modales "Filtros" y "Calendario"
    /// (WBS 4.5 y 4.7). Los eventos los filtra BackendLogica (<c>Eventos.FiltrarAsync</c>: texto, categorías,
    /// precio y fechas en una sola consulta); los lugares se buscan aparte y se filtran por tipo y precio.
    /// </summary>
    public sealed class ServicioBusqueda(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Cuántos eventos se piden como máximo.</summary>
        private const int Limite = 60;

        /// <summary>Chips de "Categoría" del modal Filtros: las cinco categorías de evento de Figma.</summary>
        public async Task<List<Etiqueta>> ListarCategoriasAsync() =>
            (await bd.Etiquetas.ListarAsync(AlcanceEtiqueta.Evento)).Where(t => Categorias.Tipo(t.Icono) != null).ToList();

        public async Task<ResultadosBusqueda> BuscarAsync(FiltroBusqueda filtro)
        {
            int? usuarioId = sesion.Usuario?.Id;
            var eventos = filtro.IncluyeEventos
                ? bd.Eventos.FiltrarAsync(new FiltroEventos
                {
                    Texto = filtro.Texto,
                    EtiquetaIds = filtro.Categorias,
                    Precio = filtro.Precio,
                    Cuando = filtro.Cuando,
                    Desde = filtro.Fecha,
                    Hasta = filtro.Fecha,
                    Limite = Limite,
                })
                : Task.FromResult(new List<EventoTarjeta>());
            var lugares = !filtro.IncluyeLugares ? Task.FromResult(new List<LugarTarjeta>())
                : string.IsNullOrWhiteSpace(filtro.Texto) ? bd.Lugares.ListarAprobadosAsync()
                : bd.Lugares.BuscarAsync(filtro.Texto, Limite);
            var favoritosEventos = usuarioId is int id ? bd.Favoritos.ListarIdsAsync(id) : Task.FromResult(new HashSet<int>());
            var favoritosLugares = usuarioId is int id2 ? bd.Favoritos.ListarIdsLugaresAsync(id2) : Task.FromResult(new HashSet<int>());
            await Task.WhenAll(eventos, lugares, favoritosEventos, favoritosLugares);

            return new ResultadosBusqueda(
                eventos.Result,
                lugares.Result.Where(filtro.PasaLugar).ToList(),
                favoritosEventos.Result,
                favoritosLugares.Result);
        }
    }

    /// <summary>"Tipo" del modal Filtros: eventos o un tipo de lugar (por tag.icon, como las píldoras del mapa).</summary>
    public enum TipoBusqueda { Eventos, Cafes, Museos, Parques }

    /// <summary>
    /// Lo que se busca. Viaja en la dirección (<see cref="Ruta"/> / <see cref="DesdeConsulta"/>) para que Atrás
    /// regrese a la búsqueda anterior y cualquier pantalla pueda abrir unos resultados ya filtrados:
    /// <c>buscar?q=música&amp;cat=2,3&amp;precio=gratis&amp;cuando=fecha&amp;fecha=2026-11-12&amp;tipo=eventos,cafes</c>.
    /// </summary>
    public sealed record FiltroBusqueda
    {
        public static readonly FiltroBusqueda Ninguno = new();

        /// <summary>Texto del buscador. Null = sin texto ("Explorar").</summary>
        public string? Texto { get; init; }
        /// <summary>Ids de etiquetas: eventos con AL MENOS UNA.</summary>
        public IReadOnlyList<int> Categorias { get; init; } = [];
        public FiltroPrecio Precio { get; init; } = FiltroPrecio.Todos;
        public FiltroCuando Cuando { get; init; } = FiltroCuando.Cualquiera;
        /// <summary>Día elegido en el Calendario (con <see cref="FiltroCuando.Rango"/>).</summary>
        public DateTime? Fecha { get; init; }
        /// <summary>Vacío = todo.</summary>
        public IReadOnlyList<TipoBusqueda> Tipos { get; init; } = [];

        private static readonly (TipoBusqueda Tipo, string Icono)[] IconosLugar =
        [
            (TipoBusqueda.Cafes, "coffee"),
            (TipoBusqueda.Museos, "museum"),
            (TipoBusqueda.Parques, "tree"),
        ];

        /// <summary>Algún filtro además del texto (para "Quitar filtros").</summary>
        public bool HayFiltros => Categorias.Count > 0 || Precio != FiltroPrecio.Todos || Cuando != FiltroCuando.Cualquiera || Tipos.Count > 0;

        /// <summary>Cuántos filtros del modal están puestos ("Filtros (2)").</summary>
        public int CuantosFiltros =>
            (Categorias.Count > 0 ? 1 : 0) + (Precio != FiltroPrecio.Todos ? 1 : 0) + (Cuando != FiltroCuando.Cualquiera ? 1 : 0) + (Tipos.Count > 0 ? 1 : 0);

        public bool IncluyeEventos => Tipos.Count == 0 || Tipos.Contains(TipoBusqueda.Eventos);

        /// <summary>
        /// Lugares: si se eligió un tipo de lugar o, sin tipo, mientras no haya filtros que solo aplican a
        /// eventos (fecha y categoría).
        /// </summary>
        public bool IncluyeLugares => Tipos.Count == 0
            ? Cuando == FiltroCuando.Cualquiera && Categorias.Count == 0
            : Tipos.Any(t => t != TipoBusqueda.Eventos);

        public bool PasaLugar(LugarTarjeta l)
        {
            var iconos = IconosLugar.Where(x => Tipos.Contains(x.Tipo)).Select(x => x.Icono).ToList();
            if (iconos.Count > 0 && !l.Etiquetas.Any(t => iconos.Contains(t.Icono))) return false;
            return Precio switch
            {
                FiltroPrecio.Gratis => l.PrecioMax == 0,
                FiltroPrecio.DePago => l.PrecioMax > 0,
                _ => true,
            };
        }

        /// <summary>Mismos criterios sin filtros: solo el texto ("Quitar filtros").</summary>
        public FiltroBusqueda SinFiltros() => new() { Texto = Texto };

        // ---------------------------------------------------------------- Dirección

        private static readonly (FiltroCuando Valor, string Texto)[] CuandoEnRuta =
        [
            (FiltroCuando.Hoy, "hoy"), (FiltroCuando.Manana, "manana"), (FiltroCuando.EsteFinDeSemana, "finde"),
            (FiltroCuando.ProximaSemana, "proxima"), (FiltroCuando.Rango, "fecha"),
        ];

        /// <summary>"buscar?q=...&amp;..." (solo lo que no está en su valor por defecto).</summary>
        public string Ruta()
        {
            var partes = new List<string>();
            void Poner(string clave, string? valor)
            {
                if (!string.IsNullOrEmpty(valor)) partes.Add($"{clave}={Uri.EscapeDataString(valor)}");
            }
            Poner("q", Texto?.Trim());
            Poner("cat", string.Join(",", Categorias));
            Poner("precio", Precio switch { FiltroPrecio.Gratis => "gratis", FiltroPrecio.DePago => "pago", _ => null });
            Poner("cuando", CuandoEnRuta.FirstOrDefault(c => c.Valor == Cuando).Texto);
            if (Cuando == FiltroCuando.Rango) Poner("fecha", Fecha?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Poner("tipo", string.Join(",", Tipos.Select(t => t.ToString().ToLowerInvariant())));
            return partes.Count == 0 ? "buscar" : $"buscar?{string.Join("&", partes)}";
        }

        /// <summary>Arma el filtro con los parámetros de la dirección (lo que no se entiende se ignora).</summary>
        public static FiltroBusqueda DesdeConsulta(string? q, string? cat, string? precio, string? cuando, string? fecha, string? tipo)
        {
            DateTime? dia = DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
            FiltroCuando cuandoValor = CuandoEnRuta.FirstOrDefault(c => c.Texto == cuando).Valor;
            if (cuandoValor == FiltroCuando.Rango && dia == null) cuandoValor = FiltroCuando.Cualquiera;
            return new FiltroBusqueda
            {
                Texto = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                Categorias = Separar(cat).Select(x => int.TryParse(x, out int id) ? id : 0).Where(id => id > 0).Distinct().ToList(),
                Precio = precio switch { "gratis" => FiltroPrecio.Gratis, "pago" => FiltroPrecio.DePago, _ => FiltroPrecio.Todos },
                Cuando = cuandoValor,
                Fecha = cuandoValor == FiltroCuando.Rango ? dia : null,
                Tipos = Separar(tipo).Select(x => Enum.TryParse<TipoBusqueda>(x, true, out var t) ? t : (TipoBusqueda?)null)
                    .OfType<TipoBusqueda>().Distinct().ToList(),
            };
        }

        private static IEnumerable<string> Separar(string? valor) =>
            (valor ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <param name="Eventos">Ordenados por fecha (los vigentes primero, como los da BackendLogica).</param>
    public sealed record ResultadosBusqueda(
        IReadOnlyList<EventoTarjeta> Eventos,
        IReadOnlyList<LugarTarjeta> Lugares,
        IReadOnlySet<int> FavoritosEventos,
        IReadOnlySet<int> FavoritosLugares)
    {
        public int Total => Eventos.Count + Lugares.Count;
    }
}

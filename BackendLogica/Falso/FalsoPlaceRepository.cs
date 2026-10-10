using BackendLogica.Contratos;
using BackendLogica.Modelos;
using BackendLogica.Repositorios;

namespace BackendLogica.Falso
{
    /// <summary>
    /// <see cref="IPlaceRepository"/> en memoria. Repite las consultas de <c>LugarDB</c> y los triggers
    /// <c>trg_place_bi</c>, <c>trg_place_bu</c> y <c>trg_placeimg_ai</c>.
    /// </summary>
    public sealed class FalsoPlaceRepository : RepositorioFalso, IPlaceRepository
    {
        internal FalsoPlaceRepository(AlmacenFalso almacen) : base(almacen) { }

        public Task<List<LugarTarjeta>> ListarRecomendadosAsync(int limite = 4, CancellationToken ct = default) =>
            Hacer(a => Aprobados(a)
                .OrderBy(l => l.Calificacion == null).ThenByDescending(l => l.Calificacion)
                .ThenByDescending(l => l.NumResenas).ThenBy(l => l.Nombre)
                .Take(limite).ToList(), ct);

        public Task<List<LugarTarjeta>> ListarAprobadosAsync(int? etiquetaId = null, CancellationToken ct = default) =>
            Hacer(a => Aprobados(a)
                .Where(l => etiquetaId == null || l.Etiquetas.Any(t => t.Id == etiquetaId))
                .OrderBy(l => l.Nombre).ToList(), ct);

        public Task<List<LugarTarjeta>> BuscarAsync(string texto, int limite = 30, CancellationToken ct = default) =>
            Hacer(a =>
            {
                texto = texto.Trim();
                return Aprobados(a)
                    .Where(l => AlmacenFalso.Contiene(l.Nombre, texto) || AlmacenFalso.Contiene(l.Direccion, texto)
                                || l.Etiquetas.Any(t => AlmacenFalso.Contiene(t.Nombre, texto)))
                    .OrderBy(l => l.Nombre).Take(limite).ToList();
            }, ct);

        public Task<int> ContarAprobadosAsync(CancellationToken ct = default) =>
            Hacer(a => a.Lugares.Count(l => l.Estado == EstadoLugar.Aprobado), ct);

        public Task<LugarDetalle?> ObtenerAsync(int lugarId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var l = a.Lugar(lugarId);
                return l == null ? null : new LugarDetalle(a.Tarjeta(l, Ahora),
                    l.Galeria.OrderBy(i => i.Orden).ThenBy(i => i.Id).Select(i => i.Url).ToList());
            }, ct);

        public Task<CalificacionLugar?> ObtenerCalificacionAsync(int lugarId, CancellationToken ct = default) =>
            Hacer(a => a.Lugar(lugarId) == null ? null : (CalificacionLugar?)a.Calificacion(lugarId), ct);

        public Task<List<LugarTarjeta>> ListarDeDuenoAsync(int duenoId, CancellationToken ct = default) =>
            Hacer(a => a.Lugares.Where(l => l.DuenoId == duenoId).Select(l => a.Tarjeta(l, Ahora)).OrderBy(l => l.Nombre).ToList(), ct);

        public Task<List<LugarTarjeta>> ListarPorEstadoAsync(EstadoLugar estado, CancellationToken ct = default) =>
            Hacer(a => a.Lugares.Where(l => l.Estado == estado).OrderBy(l => l.CreadoEn).Select(l => a.Tarjeta(l, Ahora)).ToList(), ct);

        public Task<int> CrearAsync(int duenoId, DatosLugar datos, CancellationToken ct = default) =>
            Hacer(a =>
            {
                LugarDB.ValidarDatos(datos);
                // trg_place_bi
                if (!a.EsOrganizadorAprobado(duenoId)) throw Trigger(MensajesTrigger.SoloOrganizadoresLugares);
                RevisarEtiquetas(a, datos.EtiquetaIds);

                var l = new FilaLugar
                {
                    Id = AlmacenFalso.Siguiente(a.Lugares, x => x.Id),
                    DuenoId = duenoId, Estado = EstadoLugar.Pendiente, CreadoEn = Ahora
                };
                Copiar(datos, l);
                a.Lugares.Add(l);
                return l.Id;
            }, ct);

        public Task<bool> ActualizarAsync(int lugarId, int duenoId, DatosLugar datos, CancellationToken ct = default) =>
            Hacer(a =>
            {
                LugarDB.ValidarDatos(datos);
                var l = a.Lugares.FirstOrDefault(x => x.Id == lugarId && x.DuenoId == duenoId);
                if (l == null) return false;
                RevisarEtiquetas(a, datos.EtiquetaIds);

                // trg_place_bu: editar el contenido de un lugar aprobado lo regresa a moderación.
                bool cambioContenido =
                    l.Nombre != datos.Nombre.Trim() || l.Descripcion != datos.Descripcion || l.ImagenUrl != datos.ImagenUrl ||
                    l.SitioWebUrl != datos.SitioWebUrl || l.Direccion != datos.Direccion.Trim() ||
                    l.Latitud != datos.Latitud || l.Longitud != datos.Longitud;
                if (l.Estado == EstadoLugar.Aprobado && cambioContenido) l.Estado = EstadoLugar.Pendiente;
                Copiar(datos, l);
                return true;
            }, ct);

        public Task<int> AgregarImagenAsync(int lugarId, string url, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var l = a.Lugar(lugarId) ?? throw AlmacenFalso.NoExiste();
                var imagen = new FilaImagen(a.SiguienteImagenLugar(), url, l.Galeria.Select(i => i.Orden).DefaultIfEmpty(0).Max() + 1);
                l.Galeria.Add(imagen);
                if (l.Estado == EstadoLugar.Aprobado) l.Estado = EstadoLugar.Pendiente;   // trg_placeimg_ai
                return imagen.Id;
            }, ct);

        private static IEnumerable<LugarTarjeta> Aprobados(AlmacenFalso a) =>
            a.Lugares.Where(l => l.Estado == EstadoLugar.Aprobado).Select(l => a.Tarjeta(l, Ahora)).ToList();

        private static void RevisarEtiquetas(AlmacenFalso a, IEnumerable<int> etiquetaIds)
        {
            if (etiquetaIds.Any(id => a.Etiquetas.All(t => t.Datos.Id != id))) throw AlmacenFalso.NoExiste();
        }

        private static void Copiar(DatosLugar d, FilaLugar l)
        {
            l.Nombre = d.Nombre.Trim();
            l.Descripcion = d.Descripcion;
            l.Direccion = d.Direccion.Trim();
            l.Latitud = d.Latitud;
            l.Longitud = d.Longitud;
            l.Telefono = d.Telefono;
            l.SitioWebUrl = d.SitioWebUrl;
            l.PrecioMin = d.PrecioMin;
            l.PrecioMax = d.PrecioMax;
            l.ImagenUrl = d.ImagenUrl;
            l.EtiquetaIds = d.EtiquetaIds.Distinct().ToList();
            l.Horario = d.Horario.ToList();
        }
    }
}

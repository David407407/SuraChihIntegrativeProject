using BackendLogica.Datos;

namespace BackendLogica.Modelos
{
    // Cada valor lleva el texto exacto de la columna ENUM en MySQL (ver BaseDeDatos/surachih_v1.sql).

    /// <summary>organizer_profile.status</summary>
    public enum EstadoOrganizador
    {
        [ValorBD("pending")] Pendiente,
        [ValorBD("approved")] Aprobado,
        [ValorBD("rejected")] Rechazado,
        [ValorBD("suspended")] Suspendido
    }

    /// <summary>place.status</summary>
    public enum EstadoLugar
    {
        [ValorBD("pending")] Pendiente,
        [ValorBD("approved")] Aprobado,
        [ValorBD("rejected")] Rechazado,
        [ValorBD("inactive")] Inactivo
    }

    /// <summary>event.status</summary>
    public enum EstadoEvento
    {
        [ValorBD("draft")] Borrador,
        [ValorBD("pending")] Pendiente,
        [ValorBD("approved")] Aprobado,
        [ValorBD("rejected")] Rechazado,
        [ValorBD("cancelled")] Cancelado
    }

    /// <summary>tag.scope: a qué se puede aplicar la etiqueta.</summary>
    public enum AlcanceEtiqueta
    {
        [ValorBD("event")] Evento,
        [ValorBD("place")] Lugar,
        [ValorBD("both")] Ambos
    }

    /// <summary>inscription.status</summary>
    public enum EstadoInscripcion
    {
        [ValorBD("active")] Activa,
        [ValorBD("cancelled")] Cancelada
    }

    /// <summary>moderation.decision</summary>
    public enum DecisionModeracion
    {
        [ValorBD("approved")] Aprobado,
        [ValorBD("rejected")] Rechazado
    }

    /// <summary>Qué se está moderando (una sola columna objetivo por fila en <c>moderation</c>).</summary>
    public enum TipoObjetivoModeracion { Evento, Lugar, Organizador }

    /// <summary>report.reason</summary>
    public enum MotivoReporte
    {
        [ValorBD("spam")] Spam,
        [ValorBD("scam")] Estafa,
        [ValorBD("inappropriate")] Inapropiado,
        [ValorBD("false_info")] InformacionFalsa,
        [ValorBD("other")] Otro
    }

    /// <summary>report.status</summary>
    public enum EstadoReporte
    {
        [ValorBD("open")] Abierto,
        [ValorBD("resolved")] Resuelto,
        [ValorBD("dismissed")] Descartado
    }

    /// <summary>Qué se está reportando (una sola columna objetivo por fila en <c>report</c>).</summary>
    public enum TipoObjetivoReporte { Evento, Lugar, ResenaEvento, ResenaLugar }

    /// <summary>Reseñas de eventos (event_review) o de lugares (place_review).</summary>
    public enum TipoResena { Evento, Lugar }

    /// <summary>Pestañas de la pantalla "Mis planes".</summary>
    public enum PestanaPlanes { Proximos, Pasados, Cancelados }

    /// <summary>Filtro "Precio" del modal de filtros.</summary>
    public enum FiltroPrecio { Todos, Gratis, DePago }

    /// <summary>
    /// Filtro "Cuándo" (chips Hoy / Mañana / Este fin de semana / Próxima semana, o un rango del calendario).
    /// </summary>
    public enum FiltroCuando { Cualquiera, Hoy, Manana, EsteFinDeSemana, ProximaSemana, Rango }
}

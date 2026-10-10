namespace BackendLogica.Falso
{
    /// <summary>
    /// Textos EXACTOS de los <c>SIGNAL SQLSTATE '45000'</c> de BaseDeDatos/surachih_v1.sql.
    /// En MySQL llegan como <c>MySqlException.Number == 1644</c> y se convierten en
    /// <see cref="Datos.ReglaNegocioException"/> con el mismo mensaje; el repositorio falso lanza esa
    /// excepción con estos textos para que la pantalla se comporte igual con los dos orígenes.
    /// Si cambias un trigger, cambia aquí su texto.
    /// </summary>
    public static class MensajesTrigger
    {
        // trg_event_bi / trg_event_bu
        public const string SoloOrganizadoresEventos = "Solo organizadores aprobados pueden publicar eventos.";
        public const string LugarNoAprobado = "El lugar seleccionado no esta aprobado.";

        // trg_place_bi
        public const string SoloOrganizadoresLugares = "Solo organizadores aprobados pueden registrar lugares.";

        // trg_moderation_bi
        public const string SoloModeradores = "Solo moderadores pueden moderar.";
        public const string EventoNoPendiente = "El evento no esta pendiente de moderacion.";
        public const string PropioEvento = "No puedes moderar tu propio evento.";
        public const string LugarNoPendiente = "El lugar no esta pendiente de moderacion.";
        public const string PropioLugar = "No puedes moderar tu propio lugar.";
        public const string SolicitudNoPendiente = "La solicitud no esta pendiente.";
        public const string PropiaSolicitud = "No puedes aprobar tu propia solicitud.";

        // trg_inscription_bi
        public const string NoPuedesInscribirte = "No puedes inscribirte a este evento.";

        // trg_evrev_bi / trg_plrev_bi
        public const string ResenaEventoNoTerminado = "Solo puedes resenar eventos que ya terminaron.";
        public const string ResenaPropioEvento = "No puedes resenar tu propio evento.";
        public const string ResenaLugarNoDisponible = "Este lugar no esta disponible para resenas.";
        public const string ResenaPropioLugar = "No puedes resenar tu propio lugar.";
    }
}

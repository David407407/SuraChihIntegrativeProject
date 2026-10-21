namespace Interfaz.Utilidades
{
    /// <summary>
    /// Textos fijos de la landing (copiados del frame "Landing" de Figma). Separados de los componentes
    /// para poder corregir redacción sin tocar lógica. Lo que cambia con la BD (eventos, lugares, cifras
    /// del hero) llega de <see cref="Servicios.ServicioLanding"/>.
    /// </summary>
    public static class TextosLanding
    {
        public static class Hero
        {
            public const string Sello = "Hecho en Chihuahua";
            public const string Titulo = "Descubre lo que pasa en Chihuahua antes de que pase.";
            public const string Bajada = "Conciertos, talleres, cafés y lugares para salir, reunidos en un solo lugar y revisados antes de publicarse.";
            public const string ChipInscrito = "Ya vas a ir";
            public const string ChipFavorito = "Guardado en favoritos";
        }

        /// <summary>Palabras de la cinta azul animada.</summary>
        public static readonly string[] CintaCategorias =
        [
            "Conciertos", "Talleres", "Cafés", "Museos", "Aire libre",
            "Gastronomía", "Planes en familia", "Festivales", "Parques",
        ];

        public static class ComoFunciona
        {
            public const string Titulo = "Tu plan, en tres pasos";
            public const string Bajada = "Sin buscar en cinco redes distintas ni enterarte cuando ya pasó.";
            public static readonly (string Icono, string Titulo, string Texto)[] Pasos =
            [
                ("paso-buscar", "Descubre", "Explora conciertos, talleres, cafés y lugares por categoría, fecha o en el mapa."),
                ("paso-calendario", "Guarda o inscríbete", "Marca con corazón lo que te interesa y toca “Voy a ir” para tenerlo en Mis planes."),
                ("paso-estrella", "Ve y opina", "Asiste, deja tu reseña y ayuda a otras personas a descubrir lugares nuevos."),
            ];
        }

        public static class EventosDestacados
        {
            public const string Titulo = "No te los pierdas";
            public const string Bajada = "Lo más guardado de las próximas semanas.";
            public const string Vacio = "Pronto habrá eventos publicados. ¡Vuelve en unos días!";
        }

        public static class Encuesta
        {
            public const string Titulo = "Chihuahua tiene mucho que hacer. Encontrarlo no debería ser difícil.";
            public static readonly (string Cifra, string Texto)[] Datos =
            [
                ("78.9%", "se entera de los eventos cuando ya pasaron o el mismo día."),
                ("86.8%", "cree que los negocios independientes tienen menos alcance que las plazas comerciales."),
                ("92.1%", "visita siempre los mismos lugares por no conocer otras opciones."),
            ];
            public const string Fuente = "Encuesta a 38 personas de Chihuahua, proyecto integrador UTCH 2026.";
        }

        public static class LugaresParaDescubrir
        {
            public const string Titulo = "Lugares para descubrir";
            public const string Bajada = "Cafés, museos y parques que quizá todavía no conoces.";
            public const string Vacio = "Todavía no hay lugares publicados.";
        }

        public static class Organizadores
        {
            public const string Etiqueta = "Para organizadores";
            public const string Titulo = "¿Organizas eventos en Chihuahua?";
            public const string Texto = "El 86.8% de las personas que encuestamos cree que los negocios independientes tienen menos alcance. Publica tu evento: un moderador lo revisa y llega a quien busca qué hacer.";
        }

        public static class LlamadoFinal
        {
            public const string Titulo = "Tu próximo plan está a un clic";
            public const string Texto = "Crea tu cuenta y guarda lo que te interesa para no enterarte tarde.";
        }

        public static class Pie
        {
            public const string Lema = "Eventos y lugares de Chihuahua, en un solo lugar.";
        }
    }
}

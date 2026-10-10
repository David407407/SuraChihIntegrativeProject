namespace SuraChihIntegrativeProject
{
    internal static class Navegacion
    {
        // Oculta la pantalla actual, muestra la nueva en el mismo lugar y tamaño, y la restaura al cerrar la nueva
        public static void Abrir(Form actual, Form nueva)
        {
            nueva.StartPosition = FormStartPosition.Manual;
            nueva.Location = actual.Location;
            nueva.Size = actual.Size;
            nueva.FormClosed += (_, _) =>
            {
                actual.Location = nueva.Location;
                actual.Size = nueva.Size;
                actual.Show();
            };
            actual.Hide();
            nueva.Show();
        }
    }
}

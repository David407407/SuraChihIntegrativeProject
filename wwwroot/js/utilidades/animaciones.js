/**
 * @file Animaciones de aparición al hacer scroll.
 *
 * Cualquier elemento con el atributo `data-revelar` aparece (fade + subida) cuando entra en
 * pantalla. Con `style="--orden: 2"` se escalona respecto a sus hermanos. Los estilos están en
 * css/base.css y respetan "reducir movimiento" del sistema.
 */

const observador = 'IntersectionObserver' in window
  ? new IntersectionObserver((entradas) => {
      for (const entrada of entradas) {
        if (!entrada.isIntersecting) continue;
        entrada.target.classList.add('revelado');
        observador.unobserve(entrada.target);
      }
    }, { rootMargin: '0px 0px -10% 0px', threshold: 0.12 })
  : null;

/** Empieza a observar los `[data-revelar]` dentro de `raiz` que aún no se revelan. */
export function observarRevelado(raiz = document) {
  raiz.querySelectorAll('[data-revelar]:not(.revelado)').forEach((el) => {
    if (observador) observador.observe(el);
    else el.classList.add('revelado');
  });
}

/** true si el usuario pidió reducir el movimiento en su sistema. */
export function reducirMovimiento() {
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

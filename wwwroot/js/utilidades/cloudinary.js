/**
 * @file Optimización de imágenes de Cloudinary.
 *
 * Las fotos de eventos y lugares se guardan en la BD como URL de Cloudinary
 * (event.main_image_url, place.main_image_url, *_image.url). En lugar de descargar el
 * original (varios MB), se pide a Cloudinary una versión del tamaño exacto en que se muestra:
 *
 *   https://res.cloudinary.com/<cuenta>/image/upload/v123/foto.jpg
 *   → https://res.cloudinary.com/<cuenta>/image/upload/f_auto,q_auto,c_fill,g_auto,w_600,h_660/v123/foto.jpg
 *
 * Las URL que no son de Cloudinary (p. ej. las imágenes demo locales) se devuelven sin cambios.
 */

const MARCA_UPLOAD = '/image/upload/';

/** @param {string} url */
export function esCloudinary(url) {
  return typeof url === 'string' && url.includes('res.cloudinary.com') && url.includes(MARCA_UPLOAD);
}

/**
 * URL optimizada para mostrarse en una caja de `ancho` × `alto` píxeles CSS.
 * Se multiplica por la densidad de pantalla (máx. 2) para que se vea nítida.
 * @param {string} url
 * @param {{ancho?: number, alto?: number}} [tamano]
 */
export function urlOptimizada(url, { ancho, alto } = {}) {
  if (!esCloudinary(url)) return url;

  const densidad = Math.min(window.devicePixelRatio || 1, 2);
  const transformaciones = ['f_auto', 'q_auto'];
  if (ancho || alto) transformaciones.push('c_fill', 'g_auto');
  if (ancho) transformaciones.push(`w_${Math.round(ancho * densidad)}`);
  if (alto) transformaciones.push(`h_${Math.round(alto * densidad)}`);

  const [antes, despues] = url.split(MARCA_UPLOAD);
  // Si la URL ya trae transformaciones (p. ej. "w_400,c_fill/v123/..."), se respetan.
  const yaTransformada = /^(?:(?:w|h|c|f|q|g|e|t|a|b|o|x|y|ar|dpr|fl|bo)_[^/,]+,?)+\//.test(despues);
  return yaTransformada ? url : `${antes}${MARCA_UPLOAD}${transformaciones.join(',')}/${despues}`;
}

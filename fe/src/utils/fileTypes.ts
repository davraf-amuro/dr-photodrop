/** Estensione per il nome file inviato (il server non si fida del nome, ma il tipo deve essere coerente). */
export function extensionFor(contentType: string): string {
  switch (contentType) {
    case 'image/png':
      return 'png'
    case 'image/webp':
      return 'webp'
    case 'image/heic':
      return 'heic'
    default:
      return 'jpg'
  }
}

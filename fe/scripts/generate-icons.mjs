// Genera le icone PNG della PWA (senza dipendenze): node scripts/generate-icons.mjs
// Disegno: fondo scuro, obiettivo di fotocamera (anello chiaro + lente) e una freccia verso il basso
// ("parcheggia sul PC"). Le icone "maskable" hanno il disegno entro l'area di sicurezza (cerchio al 40%).
import { mkdirSync, writeFileSync } from 'node:fs'
import { deflateSync } from 'node:zlib'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const outDir = join(dirname(fileURLToPath(import.meta.url)), '..', 'public', 'icons')

/** Stessi valori di themeColor in vite.config.ts. */
const BACKGROUND = [0x1f, 0x29, 0x37]
const FOREGROUND = [0xf9, 0xfa, 0xfb]
const ACCENT = [0x60, 0xa5, 0xfa]
const SUPERSAMPLE = 3

const crcTable = Array.from({ length: 256 }, (_, n) => {
  let c = n
  for (let k = 0; k < 8; k += 1) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1
  return c >>> 0
})

function crc32(buffer) {
  let c = 0xffffffff
  for (const byte of buffer) c = crcTable[(c ^ byte) & 0xff] ^ (c >>> 8)
  return (c ^ 0xffffffff) >>> 0
}

function chunk(type, data) {
  const length = Buffer.alloc(4)
  length.writeUInt32BE(data.length)
  const body = Buffer.concat([Buffer.from(type), data])
  const crc = Buffer.alloc(4)
  crc.writeUInt32BE(crc32(body))
  return Buffer.concat([length, body, crc])
}

function encodePng(size, rgb) {
  const header = Buffer.alloc(13)
  header.writeUInt32BE(size, 0)
  header.writeUInt32BE(size, 4)
  header[8] = 8 // 8 bit
  header[9] = 2 // RGB
  const rows = []
  for (let y = 0; y < size; y += 1) {
    rows.push(Buffer.from([0]), rgb.subarray(y * size * 3, (y + 1) * size * 3))
  }
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', header),
    chunk('IDAT', deflateSync(Buffer.concat(rows))),
    chunk('IEND', Buffer.alloc(0)),
  ])
}

/** Colore del punto (x,y) in coordinate normalizzate [-1,1] con scala `s` del disegno. */
function shade(x, y, s) {
  const r = Math.hypot(x, y) / s
  if (r <= 0.42) return ACCENT // lente
  if (r <= 0.58) return BACKGROUND // intercapedine
  if (r <= 0.78) return FOREGROUND // anello
  // freccia verso il basso sotto l'anello, solo se c'e' spazio
  return null
}

function arrow(x, y, s) {
  const ux = x / s
  const uy = y / s
  const shaft = Math.abs(ux) <= 0.07 && uy >= 0.82 && uy <= 1.05
  const head = uy >= 1.0 && uy <= 1.18 && Math.abs(ux) <= 0.2 - (uy - 1.0) * 1.1
  return shaft || head
}

function render(size, scale) {
  const rgb = Buffer.alloc(size * size * 3)
  for (let py = 0; py < size; py += 1) {
    for (let px = 0; px < size; px += 1) {
      let r = 0
      let g = 0
      let b = 0
      for (let sy = 0; sy < SUPERSAMPLE; sy += 1) {
        for (let sx = 0; sx < SUPERSAMPLE; sx += 1) {
          const x = ((px + (sx + 0.5) / SUPERSAMPLE) / size) * 2 - 1
          // Il disegno e' spostato un po' in alto per far posto alla freccia
          const y = ((py + (sy + 0.5) / SUPERSAMPLE) / size) * 2 - 1 + 0.12
          const color = shade(x, y, scale) ?? (arrow(x, y, scale) ? FOREGROUND : BACKGROUND)
          r += color[0]
          g += color[1]
          b += color[2]
        }
      }
      const samples = SUPERSAMPLE * SUPERSAMPLE
      const i = (py * size + px) * 3
      rgb[i] = Math.round(r / samples)
      rgb[i + 1] = Math.round(g / samples)
      rgb[i + 2] = Math.round(b / samples)
    }
  }
  return rgb
}

mkdirSync(outDir, { recursive: true })

// scala = raggio dell'anello esterno in coordinate normalizzate / 0.78; il disegno completo (freccia inclusa)
// arriva a ~1.18 * scala dal centro: per "any" 0.62 (margine), per "maskable" 0.5 (entro il cerchio sicuro al 40% del lato)
const icons = [
  { file: 'icon-192.png', size: 192, scale: 0.62 },
  { file: 'icon-512.png', size: 512, scale: 0.62 },
  { file: 'icon-maskable-512.png', size: 512, scale: 0.5 },
  { file: 'apple-touch-icon.png', size: 180, scale: 0.62 },
]

for (const { file, size, scale } of icons) {
  writeFileSync(join(outDir, file), encodePng(size, render(size, scale)))
  console.log(`scritto ${file} (${size}x${size})`)
}

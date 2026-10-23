"""Genera las 6 caras del cubo marcador (para Vuforia) y la plantilla imprimible.

Uso:  python generar_marcadores.py [lado_cm]
Salida en ../Marcadores/
  - cara_front.png ... cara_bottom.png  -> se suben a Vuforia Target Manager
  - cubo_imprimible_A4.png              -> se imprime al 100 % (sin "ajustar a página")
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

LADO_CM = float(sys.argv[1]) if len(sys.argv) > 1 else 6.0
DPI = 300
CARA_PX = 1024
SALIDA = Path(__file__).resolve().parent.parent / "Marcadores"

# Nombre de cara Vuforia -> (semilla, símbolo grande)
CARAS = {
    "front": (11, "1"),
    "back": (22, "2"),
    "left": (33, "3"),
    "right": (44, "4"),
    "top": (55, "5"),
    "bottom": (66, "6"),
}

FONDO = (245, 245, 240)
TINTAS = [(15, 23, 42), (30, 58, 138), (180, 83, 9), (60, 60, 60), (0, 0, 0)]


def fuente(tam):
    for nombre in ("arialbd.ttf", "Arial Bold.ttf", "DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(nombre, tam)
        except OSError:
            pass
    return ImageFont.load_default()


def poligono_aleatorio(rng, cx, cy, r):
    n = rng.randint(3, 7)
    angs = sorted(rng.uniform(0, 2 * math.pi) for _ in range(n))
    return [(cx + r * rng.uniform(0.4, 1) * math.cos(a), cy + r * rng.uniform(0.4, 1) * math.sin(a)) for a in angs]


def generar_cara(semilla, simbolo):
    """Patrón asimétrico, de alto contraste y sin repeticiones (lo que Vuforia necesita para 4-5 estrellas)."""
    rng = random.Random(semilla)
    img = Image.new("RGB", (CARA_PX, CARA_PX), FONDO)
    d = ImageDraw.Draw(img)
    m = 40  # margen interior

    # Formas medianas y pequeñas repartidas por toda la cara
    for _ in range(170):
        cx, cy = rng.uniform(m, CARA_PX - m), rng.uniform(m, CARA_PX - m)
        r = rng.choice([12, 18, 26, 38, 55])
        color = rng.choice(TINTAS)
        tipo = rng.random()
        if tipo < 0.45:
            d.polygon(poligono_aleatorio(rng, cx, cy, r), fill=color)
        elif tipo < 0.65:
            d.rectangle([cx - r / 2, cy - r / 3, cx + r / 2, cy + r / 3], fill=color)
        elif tipo < 0.8:
            d.ellipse([cx - r / 2, cy - r / 2, cx + r / 2, cy + r / 2], outline=color, width=6)
        else:
            x2, y2 = cx + rng.uniform(-90, 90), cy + rng.uniform(-90, 90)
            d.line([cx, cy, x2, y2], fill=color, width=rng.choice([4, 7, 10]))

    # Símbolo grande descentrado para romper simetrías y que el usuario identifique la cara
    f = fuente(300)
    x, y = rng.uniform(180, 520), rng.uniform(180, 480)
    d.ellipse([x - 40, y - 20, x + 260, y + 300], fill=FONDO, outline=TINTAS[0], width=14)
    d.text((x + 110, y + 140), simbolo, font=f, fill=TINTAS[1], anchor="mm")

    # Marco
    d.rectangle([0, 0, CARA_PX - 1, CARA_PX - 1], outline=TINTAS[0], width=24)
    return img


def plantilla(caras):
    """Desplegable en cruz para A4 vertical:

              [top]
       [left][front][right]
             [bottom]
              [back]   (rotada 180°)

    Orientación según convención de cuboides Vuforia: borde superior de 'top' se une con el
    borde superior de 'back', y el borde superior de 'bottom' con el borde inferior de 'front'.
    """
    a4 = (round(21.0 / 2.54 * DPI), round(29.7 / 2.54 * DPI))
    lado = round(LADO_CM / 2.54 * DPI)
    hoja = Image.new("RGB", a4, "white")
    d = ImageDraw.Draw(hoja)

    x0 = (a4[0] - 3 * lado) // 2
    y0 = (a4[1] - 4 * lado) // 2
    pos = {
        "top": (1, 0, 0),
        "left": (0, 1, 0),
        "front": (1, 1, 0),
        "right": (2, 1, 0),
        "bottom": (1, 2, 0),
        "back": (1, 3, 180),
    }
    pestana = round(0.8 / 2.54 * DPI)

    def tab(x1, y1, x2, y2, nx, ny):
        # Pestaña trapezoidal hacia afuera (normal nx, ny)
        dx, dy = (x2 - x1), (y2 - y1)
        k = 0.2
        p = [
            (x1, y1),
            (x1 + dx * k + nx * pestana, y1 + dy * k + ny * pestana),
            (x2 - dx * k + nx * pestana, y2 - dy * k + ny * pestana),
            (x2, y2),
        ]
        d.polygon(p, fill=(225, 225, 225), outline=(150, 150, 150))

    for nombre, (cx, cy, rot) in pos.items():
        x, y = x0 + cx * lado, y0 + cy * lado
        cara = caras[nombre].resize((lado, lado), Image.LANCZOS).rotate(rot)
        hoja.paste(cara, (x, y))

    # Pestañas de pegado
    L, T = x0, y0
    tab(L + lado, T, L + lado, T + lado, -1, 0)              # izquierda de top
    tab(L + 2 * lado, T, L + 2 * lado, T + lado, 1, 0)       # derecha de top
    tab(L, T + lado, L + lado, T + lado, 0, -1)              # arriba de left
    tab(L + 2 * lado, T + lado, L + 3 * lado, T + lado, 0, -1)  # arriba de right
    tab(L + lado, T + 2 * lado, L + lado, T + 3 * lado, -1, 0)  # izquierda de bottom
    tab(L + 2 * lado, T + 2 * lado, L + 2 * lado, T + 3 * lado, 1, 0)  # derecha de bottom
    tab(L + lado, T + 4 * lado, L + 2 * lado, T + 4 * lado, 0, 1)  # abajo de back

    f = fuente(40)
    d.text((a4[0] // 2, 60), f"Cubo marcador - Sistema Solar RA  ·  lado {LADO_CM:g} cm  ·  imprimir al 100 %",
           font=f, fill="black", anchor="mm")
    d.text((a4[0] // 2, a4[1] - 60), "Recortar por el borde, doblar por las líneas entre caras, pegar las pestañas grises por dentro.",
           font=fuente(32), fill=(80, 80, 80), anchor="mm")
    return hoja


def main():
    SALIDA.mkdir(parents=True, exist_ok=True)
    caras = {n: generar_cara(s, sim) for n, (s, sim) in CARAS.items()}
    for n, img in caras.items():
        img.save(SALIDA / f"cara_{n}.png", optimize=True)
    plantilla(caras).save(SALIDA / "cubo_imprimible_A4.png", dpi=(DPI, DPI))
    print(f"Listo: {SALIDA}")


if __name__ == "__main__":
    main()

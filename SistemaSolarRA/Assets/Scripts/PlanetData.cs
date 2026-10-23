using UnityEngine;

// Datos de cada cuerpo del sistema solar (valores aproximados, fuente: NASA Planetary Fact Sheet).
[System.Serializable]
public class PlanetData
{
    public string nombre;
    public string tipo;
    public string diametro;
    public string distanciaSol;
    public string duracionDia;
    public string duracionAnio;
    public int lunas;
    [TextArea] public string datoCurioso;
    public Color color;

    // Tamaño y órbita en la escena (no están a escala real, para que todo se vea).
    public float tamanoRelativo;
    public float radioOrbitaRelativo;
    public float velocidadOrbita;

    public static PlanetData[] SistemaSolar()
    {
        return new[]
        {
            new PlanetData { nombre = "Sol", tipo = "Estrella (enana amarilla)", diametro = "1 392 700 km",
                distanciaSol = "-", duracionDia = "≈ 27 días (ecuador)", duracionAnio = "-", lunas = 0,
                datoCurioso = "Concentra el 99,8 % de la masa de todo el sistema solar.",
                color = new Color(1f, 0.78f, 0.2f), tamanoRelativo = 1.2f, radioOrbitaRelativo = 0f, velocidadOrbita = 0f },
            new PlanetData { nombre = "Mercurio", tipo = "Planeta rocoso", diametro = "4 879 km",
                distanciaSol = "57,9 millones de km", duracionDia = "176 días terrestres", duracionAnio = "88 días", lunas = 0,
                datoCurioso = "Es el planeta más pequeño y el más cercano al Sol.",
                color = new Color(0.6f, 0.58f, 0.55f), tamanoRelativo = 0.12f, radioOrbitaRelativo = 0.22f, velocidadOrbita = 4.1f },
            new PlanetData { nombre = "Venus", tipo = "Planeta rocoso", diametro = "12 104 km",
                distanciaSol = "108,2 millones de km", duracionDia = "243 días terrestres", duracionAnio = "225 días", lunas = 0,
                datoCurioso = "Es el planeta más caliente (≈ 465 °C) por su efecto invernadero.",
                color = new Color(0.93f, 0.75f, 0.45f), tamanoRelativo = 0.2f, radioOrbitaRelativo = 0.31f, velocidadOrbita = 1.6f },
            new PlanetData { nombre = "Tierra", tipo = "Planeta rocoso", diametro = "12 756 km",
                distanciaSol = "149,6 millones de km", duracionDia = "24 horas", duracionAnio = "365,25 días", lunas = 1,
                datoCurioso = "El único planeta conocido con agua líquida en su superficie y vida.",
                color = new Color(0.2f, 0.45f, 0.9f), tamanoRelativo = 0.21f, radioOrbitaRelativo = 0.41f, velocidadOrbita = 1f },
            new PlanetData { nombre = "Marte", tipo = "Planeta rocoso", diametro = "6 792 km",
                distanciaSol = "227,9 millones de km", duracionDia = "24,6 horas", duracionAnio = "687 días", lunas = 2,
                datoCurioso = "Tiene el Monte Olimpo, el volcán más grande del sistema solar.",
                color = new Color(0.85f, 0.35f, 0.2f), tamanoRelativo = 0.15f, radioOrbitaRelativo = 0.51f, velocidadOrbita = 0.53f },
            new PlanetData { nombre = "Júpiter", tipo = "Gigante gaseoso", diametro = "142 984 km",
                distanciaSol = "778,5 millones de km", duracionDia = "9,9 horas", duracionAnio = "11,9 años", lunas = 95,
                datoCurioso = "Su Gran Mancha Roja es una tormenta más grande que la Tierra.",
                color = new Color(0.85f, 0.7f, 0.55f), tamanoRelativo = 0.45f, radioOrbitaRelativo = 0.66f, velocidadOrbita = 0.084f },
            new PlanetData { nombre = "Saturno", tipo = "Gigante gaseoso", diametro = "120 536 km",
                distanciaSol = "1 432 millones de km", duracionDia = "10,7 horas", duracionAnio = "29,4 años", lunas = 146,
                datoCurioso = "Su densidad es tan baja que flotaría en el agua.",
                color = new Color(0.92f, 0.83f, 0.6f), tamanoRelativo = 0.38f, radioOrbitaRelativo = 0.86f, velocidadOrbita = 0.034f },
            new PlanetData { nombre = "Urano", tipo = "Gigante helado", diametro = "51 118 km",
                distanciaSol = "2 867 millones de km", duracionDia = "17,2 horas", duracionAnio = "84 años", lunas = 28,
                datoCurioso = "Gira 'acostado': su eje está inclinado unos 98°.",
                color = new Color(0.55f, 0.85f, 0.9f), tamanoRelativo = 0.28f, radioOrbitaRelativo = 1.03f, velocidadOrbita = 0.012f },
            new PlanetData { nombre = "Neptuno", tipo = "Gigante helado", diametro = "49 528 km",
                distanciaSol = "4 515 millones de km", duracionDia = "16,1 horas", duracionAnio = "164,8 años", lunas = 16,
                datoCurioso = "Tiene los vientos más rápidos del sistema solar (≈ 2 000 km/h).",
                color = new Color(0.25f, 0.4f, 0.95f), tamanoRelativo = 0.27f, radioOrbitaRelativo = 1.17f, velocidadOrbita = 0.006f },
        };
    }
}

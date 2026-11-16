using UnityEngine;

// Sonidos generados por código (no hacen falta archivos de audio):
// - música ambiental espacial en bucle,
// - un "ding" al tocar un planeta,
// - un barrido que sube cuando aparece el sistema solar.
public class SonidoEspacial : MonoBehaviour
{
    public static SonidoEspacial Instancia { get; private set; }

    [Range(0f, 1f)] public float volumenAmbiente = 0.45f;
    [Range(0f, 1f)] public float volumenEfectos = 0.7f;

    const int Frecuencia = 22050;
    AudioSource fuenteAmbiente, fuenteEfectos;
    AudioClip clipToque, clipAparicion;
    public bool Activo { get; private set; } = true;

    void Awake()
    {
        Instancia = this;
        if (FindObjectOfType<AudioListener>() == null)
        {
            var cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam != null) cam.gameObject.AddComponent<AudioListener>();
        }

        fuenteAmbiente = gameObject.AddComponent<AudioSource>();
        fuenteAmbiente.clip = CrearAmbiente();
        fuenteAmbiente.loop = true;
        fuenteAmbiente.volume = volumenAmbiente;
        fuenteAmbiente.Play();

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.volume = volumenEfectos;
        clipToque = CrearToque();
        clipAparicion = CrearAparicion();
    }

    public void Toque() { if (Activo) fuenteEfectos.PlayOneShot(clipToque); }
    public void Aparicion() { if (Activo) fuenteEfectos.PlayOneShot(clipAparicion); }

    public bool Alternar()
    {
        Activo = !Activo;
        fuenteAmbiente.mute = !Activo;
        fuenteEfectos.mute = !Activo;
        return Activo;
    }

    // Acorde grave (La) con un vaivén lento. 16 s exactos y frecuencias con ciclos enteros: el bucle no se nota.
    static AudioClip CrearAmbiente()
    {
        const float segundos = 16f;
        int n = (int)(Frecuencia * segundos);
        var datos = new float[n];
        float[] notas = { 55f, 82.5f, 110f, 165f, 220f, 330f, 440f };
        float[] volumen = { 0.5f, 0.35f, 0.3f, 0.2f, 0.12f, 0.08f, 0.05f };
        float[] periodo = { 16f, 8f, 16f / 3f, 4f, 16f / 5f, 8f / 3f, 2f };
        float max = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Frecuencia;
            float s = 0f;
            for (int k = 0; k < notas.Length; k++)
            {
                float vaiven = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * t / periodo[k] + k * 1.3f);
                s += Mathf.Sin(2f * Mathf.PI * notas[k] * t) * volumen[k] * vaiven;
            }
            datos[i] = s;
            max = Mathf.Max(max, Mathf.Abs(s));
        }
        for (int i = 0; i < n; i++) datos[i] = datos[i] / max * 0.35f;
        var clip = AudioClip.Create("Ambiente", n, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }

    // Campanita: dos notas (Do y Sol) que se apagan rápido.
    static AudioClip CrearToque()
    {
        int n = (int)(Frecuencia * 0.6f);
        var datos = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Frecuencia;
            float env = Mathf.Exp(-t * 7f) * Mathf.Clamp01(t * 400f);
            datos[i] = env * 0.5f * (Mathf.Sin(2f * Mathf.PI * 1046.5f * t)
                + 0.6f * Mathf.Sin(2f * Mathf.PI * 1568f * t)
                + 0.25f * Mathf.Sin(2f * Mathf.PI * 2093f * t));
        }
        var clip = AudioClip.Create("Toque", n, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }

    // Barrido que sube de tono (como un "despegue") y termina en un brillo agudo.
    static AudioClip CrearAparicion()
    {
        const float segundos = 2.2f;
        int n = (int)(Frecuencia * segundos);
        var datos = new float[n];
        var azar = new System.Random(7);
        float fase = 0f, ruidoFiltrado = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Frecuencia;
            float p = t / segundos;
            float f = Mathf.Lerp(90f, 520f, p * p);
            fase += 2f * Mathf.PI * f / Frecuencia;
            ruidoFiltrado = Mathf.Lerp(ruidoFiltrado, (float)azar.NextDouble() * 2f - 1f, 0.05f + 0.3f * p);
            float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, p * 1.15f));
            float barrido = (Mathf.Sin(fase) * 0.5f + Mathf.Sin(fase * 1.5f) * 0.25f + ruidoFiltrado * 0.6f) * env;
            float brillo = 0f;
            if (t > 1.1f)
            {
                float tb = t - 1.1f;
                brillo = Mathf.Exp(-tb * 3f) * 0.3f *
                    (Mathf.Sin(2f * Mathf.PI * 1318.5f * tb) + 0.7f * Mathf.Sin(2f * Mathf.PI * 1975.5f * tb));
            }
            datos[i] = (barrido + brillo) * 0.45f;
        }
        var clip = AudioClip.Create("Aparicion", n, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

// Convierte lo que el usuario dijo en una acción de la app. Funciona igual con frases libres (Android)
// y con la lista cerrada de frases de Windows: se normaliza el texto (minúsculas, sin tildes) y se buscan palabras clave.
// Ejemplos: "ir a Saturno", "Júpiter", "recorrido", "quiz", "pausa", "más rápido", "vista general", "modo visor".
public class ComandosVoz : MonoBehaviour
{
    public ReconocimientoVoz voz;
    public PlanetSelector selector;
    public RecorridoGuiado recorrido;
    public QuizPlanetas quiz;
    public ModoVisor visor;
    public SolarSystemController control;
    public EtiquetasPlanetas etiquetas;
    [Tooltip("Cartel que muestra lo que se escuchó.")]
    public GameObject cartel;
    public TextMeshProUGUI textoCartel;

    // Nombre normalizado que se dice -> nombre del cuerpo en la escena.
    static readonly Dictionary<string, string> Cuerpos = new Dictionary<string, string>
    {
        { "sol", "Sol" }, { "mercurio", "Mercurio" }, { "venus", "Venus" }, { "tierra", "Tierra" },
        { "luna", "Luna" }, { "marte", "Marte" }, { "jupiter", "Júpiter" }, { "saturno", "Saturno" },
        { "urano", "Urano" }, { "neptuno", "Neptuno" }, { "pluton", "Plutón"}, { "cometa", "Cometa Halley" }, { "halley", "Cometa Halley" },
    };

    const string Ayuda = "Puedes decir: ir a Saturno, o el nombre de cualquier planeta. También: recorrido, quiz, " +
                         "pausa, continuar, más rápido, más lento, vista general, nombres, modo visor, leer, o silencio.";

    float ocultarCartelEn;

    void Start()
    {
        voz.frasesWindows = FrasesWindows();
        voz.alReconocer = Interpretar;
        voz.alCambiarEstado = estado => MostrarCartel(estado, 2.5f);
    }

    void Update()
    {
        if (cartel != null && cartel.activeSelf && Time.unscaledTime > ocultarCartelEn && !voz.Activo) cartel.SetActive(false);
        if (cartel != null && voz.Activo && Time.unscaledTime > ocultarCartelEn)
        {
            if (!cartel.activeSelf) cartel.SetActive(true);
            textoCartel.text = "<color=#F2B533>Micrófono:</color> di un comando (\"ayuda\" para ver cuáles)";
        }
    }

    public void MostrarCartel(string texto, float segundos)
    {
        if (cartel == null) return;
        cartel.SetActive(true);
        textoCartel.text = texto;
        ocultarCartelEn = Time.unscaledTime + segundos;
    }

    // ---------- Interpretación ----------

    public void Interpretar(string dicho)
    {
        string t = Normalizar(dicho);
        var palabras = new HashSet<string>(t.Split(' '));
        bool Tiene(params string[] p) => p.Any(palabras.Contains);
        bool Contiene(string frase) => (" " + t + " ").Contains(" " + frase + " ");

        string accion = Ejecutar(t, Tiene, Contiene);
        MostrarCartel(accion != null ? $"«{dicho}»  →  {accion}" : $"«{dicho}»  <color=#FF9A9A>(no entendí; di \"ayuda\")</color>", 3.5f);
        if (accion != null && SonidoEspacial.Instancia != null) SonidoEspacial.Instancia.Toque();
    }

    // Devuelve la acción realizada (para mostrarla) o null si no se entendió.
    string Ejecutar(string t, System.Func<string[], bool> tiene, System.Func<string, bool> contiene)
    {
        bool Tiene(params string[] p) => tiene(p);
        var narrador = Narrador.Instancia;

        if (Tiene("silencio", "callate", "calla", "callar")) { narrador?.Callar(); return "Silencio"; }
        if (Tiene("ayuda") || contiene("que puedo decir")) { narrador?.Decir(Ayuda); return "Ayuda"; }

        // Modo visor
        if (visor != null && visor.Activo && (contiene("salir del visor") || Tiene("salir"))) { visor.Salir(); return "Salir del visor"; }
        if (Tiene("visor", "cardboard")) { recorrido.Detener(); visor.Entrar(); return "Modo visor"; }

        // Quiz (tiene prioridad: el nombre de un planeta es una respuesta)
        if (quiz.Activo)
        {
            if (Tiene("siguiente", "otra", "continuar")) { quiz.Siguiente(); return "Siguiente pregunta"; }
            if (Tiene("salir", "cerrar", "terminar")) { quiz.Cerrar(); return "Cerrar quiz"; }
            if (Tiene("leer", "repetir", "repite")) { quiz.LeerPregunta(); return "Repetir pregunta"; }
            int opcion = Tiene("uno", "primera", "primero") ? 0 : Tiene("dos", "segunda", "segundo") ? 1
                       : Tiene("tres", "tercera", "tercero") ? 2 : -1;
            if (opcion >= 0 && quiz.Responder(opcion)) return "Respuesta " + (opcion + 1);
            string cuerpo = BuscarCuerpo(t);
            if (cuerpo != null && quiz.ResponderPorNombre(cuerpo)) return "Respuesta: " + cuerpo;
        }
        if (Tiene("quiz", "trivia", "preguntas", "juego", "jugar", "examen")) { recorrido.Detener(); quiz.Iniciar(); return "Quiz"; }

        // Recorrido
        if (Tiene("detener", "alto", "basta") && recorrido.Activo) { recorrido.Detener(); return "Detener recorrido"; }
        if (Tiene("recorrido", "tour", "paseo")) { if (quiz.Activo) quiz.Cerrar(); recorrido.Iniciar(); return "Recorrido guiado"; }

        // Órbitas
        if (Tiene("pausa", "pausar", "detente", "congelar")) { control.Pausar(true); return "Pausa"; }
        if (Tiene("continuar", "reanudar", "seguir", "play")) { control.Pausar(false); return "Continuar"; }
        if (contiene("mas rapido") || Tiene("acelerar", "rapido")) { control.MultiplicarVelocidad(1.8f); return "Más rápido"; }
        if (contiene("mas lento") || Tiene("despacio", "lento", "frenar")) { control.MultiplicarVelocidad(1f / 1.8f); return "Más lento"; }

        // Cámara y vista
        if (contiene("vista general") || Tiene("volver", "inicio", "regresar", "general"))
        {
            recorrido.Detener();
            selector.Deseleccionar();
            CamaraExplorador.Instancia?.VistaGeneral();
            return "Vista general";
        }
        if (Tiene("nombres", "etiquetas"))
        {
            bool mostrar = !Tiene("ocultar", "quitar", "sin") && (Tiene("mostrar", "ver", "poner") || !etiquetas.Visibles);
            etiquetas.Mostrar(mostrar);
            return mostrar ? "Mostrar nombres" : "Ocultar nombres";
        }
        if (Tiene("leer", "repetir", "repite", "informacion", "datos"))
        {
            var p = selector.Seleccionado;
            if (p != null) { narrador?.Decir(TextoFicha(p.datos)); return "Leer ficha"; }
        }
        if (Tiene("cerrar") && selector.Seleccionado != null) { selector.Deseleccionar(); return "Cerrar ficha"; }

        // Planeta: "ir a Marte", "vamos a la Luna", "Saturno"...
        string nombre = BuscarCuerpo(t);
        if (nombre != null)
        {
            var planeta = FindObjectsOfType<PlanetInfo>().FirstOrDefault(p => p.datos.nombre == nombre);
            if (planeta == null) return null;
            recorrido.Detener();
            if (visor != null && visor.Activo) visor.ElegirPorVoz(planeta);
            else
            {
                selector.Seleccionar(planeta);
                CamaraExplorador.Instancia?.Viajar(planeta.transform);
            }
            return "Viajar a " + nombre;
        }
        return null;
    }

    static string BuscarCuerpo(string normalizado)
    {
        foreach (var palabra in normalizado.Split(' '))
            if (Cuerpos.TryGetValue(palabra, out var nombre)) return nombre;
        return null;
    }

    public static string TextoFicha(PlanetData d) =>
        $"{d.nombre}. {d.tipo}. Diámetro: {d.diametro}. {d.datoCurioso}";

    // Minúsculas, sin tildes ni signos: "¿Ir a Júpiter?" -> "ir a jupiter".
    public static string Normalizar(string texto)
    {
        var sb = new StringBuilder();
        foreach (char c in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return string.Join(" ", sb.ToString().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
    }

    // ---------- Frases para Windows (su reconocedor solo acepta una lista cerrada) ----------

    static string[] FrasesWindows()
    {
        var frases = new HashSet<string>
        {
            "ayuda", "qué puedo decir", "silencio", "cállate",
            "modo visor", "visor", "salir del visor", "salir",
            "quiz", "trivia", "preguntas", "jugar", "siguiente", "siguiente pregunta", "cerrar", "terminar",
            "opción uno", "opción dos", "opción tres", "la primera", "la segunda", "la tercera",
            "recorrido", "recorrido guiado", "iniciar recorrido", "detener", "alto",
            "pausa", "pausar", "continuar", "reanudar",
            "más rápido", "más lento", "acelerar", "frenar",
            "vista general", "volver", "regresar", "inicio",
            "nombres", "mostrar nombres", "ocultar nombres",
            "leer", "repetir", "leer ficha",
        };
        // Cada cuerpo, solo o con "ir a", "viajar a", "vamos a" (con su artículo cuando lo lleva).
        var formas = new (string nombre, string conArticulo)[]
        {
            ("Sol", "al Sol"), ("Mercurio", "a Mercurio"), ("Venus", "a Venus"), ("Tierra", "a la Tierra"),
            ("Luna", "a la Luna"), ("Marte", "a Marte"), ("Júpiter", "a Júpiter"), ("Saturno", "a Saturno"),
            ("Urano", "a Urano"), ("Neptuno", "a Neptuno"), ("Plutón", "a Plutón"), ("cometa", "al cometa"), ("cometa Halley", "al cometa Halley"),
        };
        foreach (var (nombre, conArticulo) in formas)
        {
            frases.Add(nombre);
            foreach (var verbo in new[] { "ir", "viajar", "vamos", "llévame" })
                frases.Add($"{verbo} {conArticulo}");
        }
        frases.Add("la Tierra");
        frases.Add("la Luna");
        frases.Add("el Sol");
        return frases.ToArray();
    }
}

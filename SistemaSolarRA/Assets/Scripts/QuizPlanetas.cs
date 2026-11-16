using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Quiz de 8 preguntas. Al responder, la cámara vuela hasta el planeta correcto (acierte o no),
// así cada pregunta termina mostrando la respuesta en el espacio. Al final muestra el puntaje.
public class QuizPlanetas : MonoBehaviour
{
    [Header("Interfaz (la crea AppUIBuilder)")]
    public GameObject panel;
    public TextMeshProUGUI titulo;
    public TextMeshProUGUI pregunta;
    public Button[] opciones;
    public TextMeshProUGUI[] textosOpciones;
    public TextMeshProUGUI retroalimentacion;
    public Button botonSiguiente;
    public TextMeshProUGUI textoSiguiente;
    public PlanetSelector selector;
    public Color colorNormal = new Color(0.95f, 0.71f, 0.2f);
    public Color colorCorrecto = new Color(0.3f, 0.8f, 0.4f);
    public Color colorIncorrecto = new Color(0.9f, 0.3f, 0.3f);

    class Pregunta
    {
        public string texto, correcta, explicacion;
        public string[] incorrectas;
    }

    // La respuesta correcta es siempre el nombre de un cuerpo de la escena (para poder viajar hasta él).
    static readonly Pregunta[] Banco =
    {
        new Pregunta { texto = "¿Cuál es el planeta más caliente del sistema solar?", correcta = "Venus",
            incorrectas = new[] { "Mercurio", "Marte" }, explicacion = "Su atmósfera atrapa el calor: unos 465 °C, aunque Mercurio está más cerca del Sol." },
        new Pregunta { texto = "¿Cuál es el planeta más grande?", correcta = "Júpiter",
            incorrectas = new[] { "Saturno", "Neptuno" }, explicacion = "En Júpiter cabrían más de 1 300 Tierras." },
        new Pregunta { texto = "¿Qué planeta gira \"acostado\" sobre su eje?", correcta = "Urano",
            incorrectas = new[] { "Neptuno", "Venus" }, explicacion = "Su eje está inclinado unos 98°: rueda alrededor del Sol." },
        new Pregunta { texto = "¿Cuál es conocido como el planeta rojo?", correcta = "Marte",
            incorrectas = new[] { "Júpiter", "Mercurio" }, explicacion = "Su suelo tiene mucho óxido de hierro." },
        new Pregunta { texto = "¿Cuál es el planeta más cercano al Sol?", correcta = "Mercurio",
            incorrectas = new[] { "Venus", "Tierra" }, explicacion = "Está a unos 58 millones de km del Sol." },
        new Pregunta { texto = "¿Qué planeta flotaría en el agua por su baja densidad?", correcta = "Saturno",
            incorrectas = new[] { "Júpiter", "Urano" }, explicacion = "Es menos denso que el agua, aunque es enorme." },
        new Pregunta { texto = "¿Cuál es el planeta más lejano del Sol?", correcta = "Neptuno",
            incorrectas = new[] { "Urano", "Saturno" }, explicacion = "Está a unos 4 500 millones de km: la luz del Sol tarda 4 horas en llegar." },
        new Pregunta { texto = "¿Qué cuerpo concentra el 99,8 % de la masa del sistema solar?", correcta = "Sol",
            incorrectas = new[] { "Júpiter", "Tierra" }, explicacion = "Todo lo demás junto es apenas el 0,2 %." },
        new Pregunta { texto = "¿En qué planeta un día dura más que un año?", correcta = "Venus",
            incorrectas = new[] { "Mercurio", "Neptuno" }, explicacion = "Venus tarda 243 días en girar sobre sí mismo y 225 en dar la vuelta al Sol." },
        new Pregunta { texto = "¿Qué cuerpo tiene una cola que siempre apunta lejos del Sol?", correcta = "Cometa Halley",
            incorrectas = new[] { "Luna", "Marte" }, explicacion = "El viento solar empuja el gas y el polvo del cometa." },
    };

    const int PreguntasPorPartida = 8;

    readonly List<Pregunta> partida = new List<Pregunta>();
    string[] textosActuales;
    int indice, puntaje;
    bool respondida, terminado;
    PlanetInfo resaltado;

    public bool Activo => panel != null && panel.activeSelf;

    // En Start: AppUIBuilder asigna los botones después de agregar este componente.
    void Start()
    {
        for (int i = 0; i < opciones.Length; i++)
        {
            int n = i;
            opciones[i].onClick.AddListener(() => Responder(n));
        }
        botonSiguiente.onClick.AddListener(Siguiente);
    }

    public void Iniciar()
    {
        if (selector != null)
        {
            selector.Deseleccionar();
            selector.enabled = false; // durante el quiz, tocar planetas no abre fichas
        }
        partida.Clear();
        var mezcla = new List<Pregunta>(Banco);
        for (int i = 0; i < PreguntasPorPartida && mezcla.Count > 0; i++)
        {
            int k = Random.Range(0, mezcla.Count);
            partida.Add(mezcla[k]);
            mezcla.RemoveAt(k);
        }
        indice = 0;
        puntaje = 0;
        terminado = false;
        panel.SetActive(true);
        CamaraExplorador.fichaAbierta = true; // el panel ocupa la derecha, como la ficha
        Mostrar();
    }

    public void Cerrar()
    {
        Resaltar(null);
        panel.SetActive(false);
        CamaraExplorador.fichaAbierta = false;
        if (selector != null) selector.enabled = true;
    }

    void Mostrar()
    {
        var p = partida[indice];
        respondida = false;
        titulo.text = $"Pregunta {indice + 1} de {partida.Count}";
        pregunta.text = p.texto;
        textosActuales = new[] { p.correcta, p.incorrectas[0], p.incorrectas[1] };
        for (int i = textosActuales.Length - 1; i > 0; i--)
        {
            int k = Random.Range(0, i + 1);
            (textosActuales[i], textosActuales[k]) = (textosActuales[k], textosActuales[i]);
        }
        for (int i = 0; i < opciones.Length; i++)
        {
            opciones[i].gameObject.SetActive(true);
            opciones[i].interactable = true;
            opciones[i].targetGraphic.color = colorNormal;
            textosOpciones[i].text = textosActuales[i];
        }
        retroalimentacion.text = $"Puntaje: {puntaje}";
        botonSiguiente.gameObject.SetActive(false);
    }

    public void Responder(int elegida)
    {
        if (respondida) return;
        respondida = true;
        var p = partida[indice];
        bool acierto = textosActuales[elegida] == p.correcta;
        if (acierto) puntaje++;

        for (int i = 0; i < opciones.Length; i++)
        {
            opciones[i].interactable = false;
            bool esCorrecta = textosActuales[i] == p.correcta;
            if (esCorrecta) opciones[i].targetGraphic.color = colorCorrecto;
            else if (i == elegida) opciones[i].targetGraphic.color = colorIncorrecto;
        }
        // Con interactable = false Unity atenúa el color: se fuerza a que se vea completo.
        foreach (var b in opciones)
        {
            var colores = b.colors;
            colores.disabledColor = Color.white;
            b.colors = colores;
        }

        if (SonidoEspacial.Instancia != null)
        {
            if (acierto) SonidoEspacial.Instancia.Correcto();
            else SonidoEspacial.Instancia.Incorrecto();
        }
        retroalimentacion.text = (acierto ? "<color=#6EE07F><b>¡Correcto!</b></color> " : $"<color=#FF7A7A><b>Era {p.correcta}.</b></color> ")
                                 + p.explicacion + $"\n<size=80%>Puntaje: {puntaje}  ·  Viajando a {p.correcta}…</size>";

        // Viajar hasta la respuesta correcta.
        foreach (var planeta in FindObjectsOfType<PlanetInfo>())
            if (planeta.datos.nombre == p.correcta)
            {
                Resaltar(planeta);
                if (CamaraExplorador.Instancia != null) CamaraExplorador.Instancia.Viajar(planeta.transform);
                break;
            }

        botonSiguiente.gameObject.SetActive(true);
        textoSiguiente.text = indice + 1 < partida.Count ? "Siguiente" : "Ver resultado";
    }

    void Siguiente()
    {
        if (terminado) { Iniciar(); return; }
        indice++;
        if (indice < partida.Count) { Mostrar(); return; }

        terminado = true;
        Resaltar(null);
        titulo.text = "Resultado";
        string mensaje = puntaje == partida.Count ? "¡Perfecto! Eres un experto del sistema solar."
                       : puntaje >= partida.Count * 0.6f ? "¡Muy bien!"
                       : "Explora un poco más y vuelve a intentarlo.";
        pregunta.text = $"<size=140%><b>{puntaje} de {partida.Count}</b></size>\n{mensaje}";
        foreach (var b in opciones) b.gameObject.SetActive(false);
        retroalimentacion.text = "";
        textoSiguiente.text = "Jugar otra vez";
        if (SonidoEspacial.Instancia != null && puntaje >= partida.Count * 0.6f) SonidoEspacial.Instancia.Aparicion();
        if (CamaraExplorador.Instancia != null) CamaraExplorador.Instancia.VistaGeneral();
    }

    void Resaltar(PlanetInfo planeta)
    {
        if (resaltado != null) resaltado.Resaltar(false);
        resaltado = planeta;
        if (resaltado != null) resaltado.Resaltar(true);
    }

}

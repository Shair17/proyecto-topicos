using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Crea toda la interfaz (horizontal, para PC y celular) y conecta los scripts.
// Uso: crear un objeto vacío "GestorApp" y agregarle solo este componente.
public class AppUIBuilder : MonoBehaviour
{
    [Header("Colores")]
    public Color colorPanel = new Color(0.08f, 0.12f, 0.24f, 0.88f);
    public Color colorBoton = new Color(0.95f, 0.71f, 0.2f, 1f);
    public Color colorTextoBoton = new Color(0.08f, 0.12f, 0.24f, 1f);

    Button botonGiro;
    TextMeshProUGUI textoGiro;

    void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep; // que el celular no apague la pantalla durante la exposición
        CrearEventSystem();
        var canvas = CrearCanvas("Canvas", 0);
        bool celular = Application.isMobilePlatform;

        // ---------- Arriba: ayuda (izquierda) y distancia real al Sol (derecha) ----------
        var ayuda = CrearRect("Ayuda", canvas, new Vector2(0.01f, 0.925f), new Vector2(0.6f, 0.99f));
        ayuda.gameObject.AddComponent<Image>().color = new Color(colorPanel.r, colorPanel.g, colorPanel.b, 0.6f);
        var textoAyuda = CrearTexto("Texto", ayuda, celular
                ? "Gira el celular o arrastra para mirar  ·  Toca un planeta"
                : "WASD: moverse  ·  Q/E: bajar/subir  ·  Shift: rápido  ·  Arrastrar: mirar  ·  Clic: planeta",
            26, FontStyles.Normal, TextAlignmentOptions.Center);
        AutoTamano(textoAyuda, 16, 26);
        textoAyuda.margin = new Vector4(12f, 0f, 12f, 0f);

        var distancia = CrearRect("Distancia", canvas, new Vector2(0.62f, 0.925f), new Vector2(0.99f, 0.99f));
        distancia.gameObject.AddComponent<Image>().color = new Color(colorPanel.r, colorPanel.g, colorPanel.b, 0.6f);
        var textoDistancia = CrearTexto("Texto", distancia, "", 26, FontStyles.Normal, TextAlignmentOptions.Center);
        AutoTamano(textoDistancia, 16, 26);

        // ---------- Abajo: botones y velocidad ----------
        var controles = CrearRect("Controles", canvas, new Vector2(0f, 0f), new Vector2(1f, 0.11f));
        controles.gameObject.AddComponent<Image>().color = colorPanel;
        int columna = 0;
        Button BotonBarra(string nombre, string texto, out TextMeshProUGUI etiqueta)
        {
            float x = 0.006f + columna++ * 0.092f;
            return CrearBoton(nombre, controles, texto, new Vector2(x, 0.15f), new Vector2(x + 0.086f, 0.85f), out etiqueta);
        }
        var botonPausa = BotonBarra("BotonPausa", "Pausar", out var textoPausa);
        var botonSonido = BotonBarra("BotonSonido", "Sonido: Sí", out var textoSonido);
        var botonNombres = BotonBarra("BotonNombres", "Nombres: Sí", out var textoNombres);
        var botonGeneral = BotonBarra("BotonVistaGeneral", "Vista general", out _);
        var botonRecorrido = BotonBarra("BotonRecorrido", "Recorrido", out var textoRecorrido);
        var botonQuiz = BotonBarra("BotonQuiz", "Quiz", out _);
        var botonVisor = BotonBarra("BotonVisor", "Visor VR", out _);
        botonGiro = BotonBarra("BotonGiroscopio", "Giroscopio: No", out textoGiro);

        var etiqueta = CrearRect("EtiquetaVelocidad", controles, new Vector2(0.76f, 0.55f), new Vector2(0.99f, 0.95f));
        CrearTexto("Texto", etiqueta, "Velocidad de las órbitas", 30, FontStyles.Normal, TextAlignmentOptions.Center);
        var slider = CrearSlider(controles, new Vector2(0.77f, 0.15f), new Vector2(0.98f, 0.5f));

        // Botones para avanzar en el celular (en PC se usa el teclado)
        if (celular)
        {
            CrearBoton("BotonAvanzar", canvas, "Avanzar",
                new Vector2(0.01f, 0.4f), new Vector2(0.12f, 0.56f), out _).gameObject.AddComponent<BotonMantener>().valor = 1f;
            CrearBoton("BotonRetroceder", canvas, "Retroceder",
                new Vector2(0.01f, 0.22f), new Vector2(0.12f, 0.38f), out _).gameObject.AddComponent<BotonMantener>().valor = -1f;
        }

        // ---------- Ficha del planeta (derecha) ----------
        var panelInfo = CrearRect("PanelInfo", canvas, new Vector2(0.64f, 0.13f), new Vector2(0.99f, 0.905f));
        panelInfo.gameObject.AddComponent<Image>().color = colorPanel;
        var titulo = CrearTexto("Titulo", CrearRect("ZonaTitulo", panelInfo, new Vector2(0.06f, 0.86f), new Vector2(0.8f, 0.98f)),
            "", 60, FontStyles.Bold, TextAlignmentOptions.Left);
        titulo.color = colorBoton;
        AutoTamano(titulo, 30, 60);
        var cuerpo = CrearTexto("Cuerpo", CrearRect("ZonaCuerpo", panelInfo, new Vector2(0.06f, 0.17f), new Vector2(0.95f, 0.85f)),
            "", 34, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        AutoTamano(cuerpo, 18, 34);
        var botonCerrar = CrearBoton("BotonCerrar", panelInfo, "X",
            new Vector2(0.84f, 0.87f), new Vector2(0.96f, 0.98f), out _);
        var botonViajar = CrearBoton("BotonViajar", panelInfo, "Viajar hasta aquí",
            new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.14f), out _);
        var acento = CrearRect("Acento", panelInfo, new Vector2(0f, 0f), new Vector2(0.015f, 1f)).gameObject.AddComponent<Image>();

        // ---------- Panel del quiz (mismo lugar que la ficha) ----------
        var panelQuiz = CrearRect("PanelQuiz", canvas, new Vector2(0.64f, 0.13f), new Vector2(0.99f, 0.905f));
        panelQuiz.gameObject.AddComponent<Image>().color = colorPanel;
        var tituloQuiz = CrearTexto("Titulo", CrearRect("ZonaTitulo", panelQuiz, new Vector2(0.06f, 0.88f), new Vector2(0.8f, 0.98f)),
            "", 40, FontStyles.Bold, TextAlignmentOptions.Left);
        tituloQuiz.color = colorBoton;
        var preguntaQuiz = CrearTexto("Pregunta", CrearRect("ZonaPregunta", panelQuiz, new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.87f)),
            "", 36, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        AutoTamano(preguntaQuiz, 20, 36);
        var opciones = new Button[3];
        var textosOpciones = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            float y = 0.54f - i * 0.12f;
            opciones[i] = CrearBoton("Opcion" + i, panelQuiz, "", new Vector2(0.06f, y), new Vector2(0.94f, y + 0.1f), out textosOpciones[i]);
        }
        var retro = CrearTexto("Retro", CrearRect("ZonaRetro", panelQuiz, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.3f)),
            "", 26, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        AutoTamano(retro, 16, 26);
        var botonSiguiente = CrearBoton("BotonSiguiente", panelQuiz, "Siguiente",
            new Vector2(0.06f, 0.03f), new Vector2(0.62f, 0.13f), out var textoSiguiente);
        var botonCerrarQuiz = CrearBoton("BotonCerrarQuiz", panelQuiz, "Salir",
            new Vector2(0.66f, 0.03f), new Vector2(0.94f, 0.13f), out _);
        CrearRect("Acento", panelQuiz, new Vector2(0f, 0f), new Vector2(0.015f, 1f)).gameObject.AddComponent<Image>().color = colorBoton;

        // ---------- Modo visor: Canvas aparte con solo el botón para salir ----------
        var canvasVisor = CrearCanvas("CanvasVisor", 10);
        var botonSalirVisor = CrearBoton("BotonSalirVisor", canvasVisor, "Salir del visor",
            new Vector2(0.43f, 0.93f), new Vector2(0.57f, 0.995f), out _);
        canvasVisor.gameObject.SetActive(false);

        // ---------- Scripts de la app ----------
        var info = panelInfo.gameObject.AddComponent<InfoPanelUI>();
        info.titulo = titulo;
        info.cuerpo = cuerpo;
        info.acento = acento;
        panelInfo.gameObject.SetActive(false);

        var selector = gameObject.AddComponent<PlanetSelector>();
        selector.panel = info;
        selector.camara = BuscarCamara();

        var sonido = gameObject.AddComponent<SonidoEspacial>();
        var control = gameObject.AddComponent<SolarSystemController>();
        control.textoBotonPausa = textoPausa;
        var etiquetas = gameObject.AddComponent<EtiquetasPlanetas>();
        gameObject.AddComponent<IndicadorDistancia>().texto = textoDistancia;

        var recorrido = gameObject.AddComponent<RecorridoGuiado>();
        recorrido.selector = selector;
        recorrido.textoEstado = textoAyuda;
        recorrido.alCambiar = activo => textoRecorrido.text = activo ? "Detener" : "Recorrido";

        var quiz = gameObject.AddComponent<QuizPlanetas>();
        quiz.panel = panelQuiz.gameObject;
        quiz.titulo = tituloQuiz;
        quiz.pregunta = preguntaQuiz;
        quiz.opciones = opciones;
        quiz.textosOpciones = textosOpciones;
        quiz.retroalimentacion = retro;
        quiz.botonSiguiente = botonSiguiente;
        quiz.textoSiguiente = textoSiguiente;
        quiz.selector = selector;
        quiz.colorNormal = colorBoton;
        panelQuiz.gameObject.SetActive(false);

        var visor = gameObject.AddComponent<ModoVisor>();
        visor.canvasNormal = canvas.GetComponent<Canvas>();
        visor.interfazVisor = canvasVisor.gameObject;
        visor.selector = selector;
        visor.quiz = quiz;
        var builder = FindObjectOfType<SolarSystemBuilder>();
        if (builder != null) visor.materialAditivo = builder.materialAditivo;

        // ---------- Conexiones ----------
        botonPausa.onClick.AddListener(control.AlternarPausa);
        botonSonido.onClick.AddListener(() => textoSonido.text = sonido.Alternar() ? "Sonido: Sí" : "Sonido: No");
        botonNombres.onClick.AddListener(() => textoNombres.text = etiquetas.Alternar() ? "Nombres: Sí" : "Nombres: No");
        botonCerrar.onClick.AddListener(selector.Deseleccionar);
        botonViajar.onClick.AddListener(selector.ViajarAlSeleccionado);
        slider.onValueChanged.AddListener(control.CambiarVelocidad);

        botonGeneral.onClick.AddListener(() =>
        {
            recorrido.Detener();
            selector.Deseleccionar();
            if (CamaraExplorador.Instancia != null) CamaraExplorador.Instancia.VistaGeneral();
        });
        botonRecorrido.onClick.AddListener(() =>
        {
            if (quiz.Activo) quiz.Cerrar();
            recorrido.Alternar();
        });
        botonQuiz.onClick.AddListener(() =>
        {
            recorrido.Detener();
            if (quiz.Activo) quiz.Cerrar();
            else quiz.Iniciar();
        });
        botonCerrarQuiz.onClick.AddListener(quiz.Cerrar);
        botonVisor.onClick.AddListener(() => visor.Entrar());
        botonSalirVisor.onClick.AddListener(visor.Salir);
    }

    // En Start (y no en Awake) porque la cámara ya tiene que estar preparada.
    void Start()
    {
        // El botón del giroscopio solo aparece si el dispositivo tiene uno (no en PC).
        var camara = CamaraExplorador.Instancia;
        if (camara != null && camara.GiroscopioDisponible)
        {
            botonGiro.onClick.AddListener(() => textoGiro.text = camara.AlternarGiroscopio() ? "Giroscopio: Sí" : "Giroscopio: No");
            // En el celular empieza encendido: girar el teléfono es la forma más natural de mirar.
            textoGiro.text = camara.AlternarGiroscopio() ? "Giroscopio: Sí" : "Giroscopio: No";
        }
        else botonGiro.gameObject.SetActive(false);
    }

    // El texto del botón del giroscopio también cambia cuando el modo visor lo enciende o apaga.
    void LateUpdate()
    {
        var camara = CamaraExplorador.Instancia;
        if (camara != null && botonGiro != null && botonGiro.gameObject.activeSelf)
            textoGiro.text = camara.GiroscopioActivo ? "Giroscopio: Sí" : "Giroscopio: No";
    }

    static Camera BuscarCamara()
    {
        if (Camera.main != null) return Camera.main;
        return FindObjectOfType<Camera>();
    }

    static void CrearEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    static RectTransform CrearCanvas(string nombre, int orden)
    {
        var go = new GameObject(nombre, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = orden;
        var escala = go.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        return (RectTransform)go.transform;
    }

    static RectTransform CrearRect(string nombre, Transform padre, Vector2 anclaMin, Vector2 anclaMax)
    {
        var rt = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string texto, float tamano,
        FontStyles estilo, TextAlignmentOptions alineacion)
    {
        var rt = CrearRect(nombre, padre, Vector2.zero, Vector2.one);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.fontStyle = estilo;
        tmp.alignment = alineacion;
        tmp.color = Color.white;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void AutoTamano(TextMeshProUGUI tmp, float minimo, float maximo)
    {
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minimo;
        tmp.fontSizeMax = maximo;
    }

    Button CrearBoton(string nombre, Transform padre, string texto, Vector2 anclaMin, Vector2 anclaMax,
        out TextMeshProUGUI etiqueta)
    {
        var rt = CrearRect(nombre, padre, anclaMin, anclaMax);
        var imagen = rt.gameObject.AddComponent<Image>();
        imagen.color = colorBoton;
        var boton = rt.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        etiqueta = CrearTexto("Texto", rt, texto, 34, FontStyles.Bold, TextAlignmentOptions.Center);
        etiqueta.color = colorTextoBoton;
        AutoTamano(etiqueta, 14, 34);
        etiqueta.margin = new Vector4(6f, 0f, 6f, 0f);
        return boton;
    }

    Slider CrearSlider(Transform padre, Vector2 anclaMin, Vector2 anclaMax)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = "SliderVelocidad";
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Colores: fondo gris, relleno y perilla dorados
        foreach (var img in go.GetComponentsInChildren<Image>())
            img.color = img.name == "Background" ? new Color(1f, 1f, 1f, 0.3f) : colorBoton;

        // Perilla más grande para el dedo
        var perilla = go.GetComponent<Slider>().handleRect;
        if (perilla != null) perilla.sizeDelta = new Vector2(50f, 30f);

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0.1f;
        slider.maxValue = 5f;
        slider.value = 1f;
        return slider;
    }
}

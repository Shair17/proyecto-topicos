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

    void Awake()
    {
        CrearEventSystem();
        var canvas = CrearCanvas();
        bool celular = Application.isMobilePlatform;

        // Ayuda de controles (arriba a la izquierda)
        var ayuda = CrearRect("Ayuda", canvas, new Vector2(0.01f, 0.925f), new Vector2(celular ? 0.5f : 0.62f, 0.99f));
        ayuda.gameObject.AddComponent<Image>().color = new Color(colorPanel.r, colorPanel.g, colorPanel.b, 0.6f);
        var textoAyuda = CrearTexto("Texto", ayuda, celular
                ? "Gira el celular o arrastra para mirar  ·  Toca un planeta"
                : "WASD: moverse  ·  Q/E: bajar/subir  ·  Shift: rápido  ·  Arrastrar: mirar  ·  Clic: planeta",
            26, FontStyles.Normal, TextAlignmentOptions.Center);
        textoAyuda.enableAutoSizing = true;
        textoAyuda.fontSizeMin = 16;
        textoAyuda.fontSizeMax = 26;
        textoAyuda.margin = new Vector4(12f, 0f, 12f, 0f);

        // Controles de abajo
        var controles = CrearRect("Controles", canvas, new Vector2(0f, 0f), new Vector2(1f, 0.11f));
        controles.gameObject.AddComponent<Image>().color = colorPanel;

        var botonPausa = CrearBoton("BotonPausa", controles, "Pausar",
            new Vector2(0.01f, 0.15f), new Vector2(0.13f, 0.85f), out TextMeshProUGUI textoPausa);
        var botonSonido = CrearBoton("BotonSonido", controles, "Sonido: Sí",
            new Vector2(0.14f, 0.15f), new Vector2(0.27f, 0.85f), out TextMeshProUGUI textoSonido);
        var botonGeneral = CrearBoton("BotonVistaGeneral", controles, "Vista general",
            new Vector2(0.28f, 0.15f), new Vector2(0.42f, 0.85f), out _);
        var botonGiro = CrearBoton("BotonGiroscopio", controles, "Giroscopio: No",
            new Vector2(0.43f, 0.15f), new Vector2(0.58f, 0.85f), out TextMeshProUGUI textoGiro);

        var etiqueta = CrearRect("EtiquetaVelocidad", controles, new Vector2(0.62f, 0.55f), new Vector2(0.98f, 0.95f));
        CrearTexto("Texto", etiqueta, "Velocidad de las órbitas", 32, FontStyles.Normal, TextAlignmentOptions.Center);
        var slider = CrearSlider(controles, new Vector2(0.62f, 0.15f), new Vector2(0.98f, 0.5f));

        // Botones para avanzar en el celular (en PC se usa el teclado)
        if (celular)
        {
            CrearBoton("BotonAvanzar", canvas, "Avanzar",
                new Vector2(0.01f, 0.4f), new Vector2(0.12f, 0.56f), out _).gameObject.AddComponent<BotonMantener>().valor = 1f;
            CrearBoton("BotonRetroceder", canvas, "Retroceder",
                new Vector2(0.01f, 0.22f), new Vector2(0.12f, 0.38f), out _).gameObject.AddComponent<BotonMantener>().valor = -1f;
        }

        // Ficha del planeta (a la derecha)
        var panelInfo = CrearRect("PanelInfo", canvas, new Vector2(0.64f, 0.13f), new Vector2(0.99f, 0.97f));
        panelInfo.gameObject.AddComponent<Image>().color = colorPanel;
        var titulo = CrearTexto("Titulo", CrearRect("ZonaTitulo", panelInfo, new Vector2(0.06f, 0.86f), new Vector2(0.8f, 0.98f)),
            "", 64, FontStyles.Bold, TextAlignmentOptions.Left);
        titulo.color = colorBoton;
        var cuerpo = CrearTexto("Cuerpo", CrearRect("ZonaCuerpo", panelInfo, new Vector2(0.06f, 0.17f), new Vector2(0.95f, 0.85f)),
            "", 36, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        cuerpo.enableAutoSizing = true;
        cuerpo.fontSizeMin = 20;
        cuerpo.fontSizeMax = 36;
        var botonCerrar = CrearBoton("BotonCerrar", panelInfo, "X",
            new Vector2(0.84f, 0.87f), new Vector2(0.96f, 0.98f), out _);
        var botonViajar = CrearBoton("BotonViajar", panelInfo, "Viajar hasta aquí",
            new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.14f), out _);
        // Franja del color del planeta en el borde izquierdo de la ficha
        var acento = CrearRect("Acento", panelInfo, new Vector2(0f, 0f), new Vector2(0.015f, 1f)).gameObject.AddComponent<Image>();

        // Scripts de la app
        var info = panelInfo.gameObject.AddComponent<InfoPanelUI>();
        info.titulo = titulo;
        info.cuerpo = cuerpo;
        info.acento = acento;
        panelInfo.gameObject.SetActive(false);

        var selector = gameObject.AddComponent<PlanetSelector>();
        selector.panel = info;
        selector.camara = BuscarCamara();

        var sonido = gameObject.AddComponent<SonidoEspacial>();
        botonSonido.onClick.AddListener(() => textoSonido.text = sonido.Alternar() ? "Sonido: Sí" : "Sonido: No");

        var control = gameObject.AddComponent<SolarSystemController>();
        control.textoBotonPausa = textoPausa;

        botonPausa.onClick.AddListener(control.AlternarPausa);
        botonCerrar.onClick.AddListener(selector.Deseleccionar);
        botonViajar.onClick.AddListener(selector.ViajarAlSeleccionado);
        slider.onValueChanged.AddListener(control.CambiarVelocidad);

        botonGeneral.onClick.AddListener(() =>
        {
            selector.Deseleccionar();
            if (CamaraExplorador.Instancia != null) CamaraExplorador.Instancia.VistaGeneral();
        });

        this.botonGiro = botonGiro;
        this.textoGiro = textoGiro;
    }

    Button botonGiro;
    TextMeshProUGUI textoGiro;

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

    RectTransform CrearCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
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

    Button CrearBoton(string nombre, Transform padre, string texto, Vector2 anclaMin, Vector2 anclaMax,
        out TextMeshProUGUI etiqueta)
    {
        var rt = CrearRect(nombre, padre, anclaMin, anclaMax);
        var imagen = rt.gameObject.AddComponent<Image>();
        imagen.color = colorBoton;
        var boton = rt.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        etiqueta = CrearTexto("Texto", rt, texto, 40, FontStyles.Bold, TextAlignmentOptions.Center);
        etiqueta.color = colorTextoBoton;
        etiqueta.enableAutoSizing = true;
        etiqueta.fontSizeMin = 18;
        etiqueta.fontSizeMax = 40;
        etiqueta.margin = new Vector4(8f, 0f, 8f, 0f);
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

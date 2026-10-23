using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Crea toda la interfaz (Canvas, botones, slider, ficha del planeta) y conecta los scripts.
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

        // Mensaje cuando no se ve el cubo
        var buscar = CrearRect("TextoBuscar", canvas, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f));
        buscar.gameObject.AddComponent<Image>().color = colorPanel;
        CrearTexto("Texto", buscar, "Apunta la cámara al cubo", 56, FontStyles.Bold, TextAlignmentOptions.Center);

        // Controles de abajo: pausa y velocidad
        var controles = CrearRect("Controles", canvas, new Vector2(0f, 0f), new Vector2(1f, 0.12f));
        controles.gameObject.AddComponent<Image>().color = colorPanel;

        var botonPausa = CrearBoton("BotonPausa", controles, "Pausar",
            new Vector2(0.04f, 0.2f), new Vector2(0.36f, 0.8f), out TextMeshProUGUI textoPausa);

        var etiqueta = CrearRect("EtiquetaVelocidad", controles, new Vector2(0.42f, 0.55f), new Vector2(0.96f, 0.9f));
        CrearTexto("Texto", etiqueta, "Velocidad", 40, FontStyles.Normal, TextAlignmentOptions.Center);
        var slider = CrearSlider(controles, new Vector2(0.42f, 0.2f), new Vector2(0.96f, 0.5f));

        // Ficha del planeta (arriba)
        var panelInfo = CrearRect("PanelInfo", canvas, new Vector2(0.04f, 0.6f), new Vector2(0.96f, 0.97f));
        panelInfo.gameObject.AddComponent<Image>().color = colorPanel;
        var titulo = CrearTexto("Titulo", CrearRect("ZonaTitulo", panelInfo, new Vector2(0.05f, 0.83f), new Vector2(0.8f, 0.97f)),
            "", 72, FontStyles.Bold, TextAlignmentOptions.Left);
        titulo.color = colorBoton;
        var cuerpo = CrearTexto("Cuerpo", CrearRect("ZonaCuerpo", panelInfo, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.82f)),
            "", 42, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        cuerpo.enableAutoSizing = true;
        cuerpo.fontSizeMin = 24;
        cuerpo.fontSizeMax = 42;
        var botonCerrar = CrearBoton("BotonCerrar", panelInfo, "X",
            new Vector2(0.84f, 0.84f), new Vector2(0.96f, 0.97f), out _);

        // Scripts de la app
        var info = panelInfo.gameObject.AddComponent<InfoPanelUI>();
        info.titulo = titulo;
        info.cuerpo = cuerpo;
        panelInfo.gameObject.SetActive(false);

        var selector = gameObject.AddComponent<PlanetSelector>();
        selector.panel = info;
        selector.camara = BuscarCamara();

        var control = gameObject.AddComponent<SolarSystemController>();
        control.textoBotonPausa = textoPausa;

        var seguimiento = gameObject.AddComponent<CubeTrackingUI>();
        seguimiento.mensajeBuscar = buscar.gameObject;
        seguimiento.controles = controles.gameObject;

        botonPausa.onClick.AddListener(control.AlternarPausa);
        botonCerrar.onClick.AddListener(selector.Deseleccionar);
        slider.onValueChanged.AddListener(control.CambiarVelocidad);
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
        escala.referenceResolution = new Vector2(1080f, 1920f);
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
        etiqueta = CrearTexto("Texto", rt, texto, 48, FontStyles.Bold, TextAlignmentOptions.Center);
        etiqueta.color = colorTextoBoton;
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
        if (perilla != null) perilla.sizeDelta = new Vector2(60f, 30f);

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0.1f;
        slider.maxValue = 5f;
        slider.value = 1f;
        return slider;
    }
}

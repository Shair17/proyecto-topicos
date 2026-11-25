using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Modo visor: RV INMERSIVA adaptado con la óptica estéreo exacta de visores TITI VR / YouTube VR.
public class ModoVisor : MonoBehaviour
{
    public Canvas canvasNormal;
    [Tooltip("Interfaz mínima del modo visor (botón Salir), en su propio Canvas.")]
    public GameObject interfazVisor;
    public PlanetSelector selector;
    public QuizPlanetas quiz;
    public Material materialAditivo;
    
    [Header("Ajustes de Óptica Estéreo TITI VR")]
    public float separacionOjos = 0.024f;   // Distancia IPD optimizada para visores tipo Cardboard / TITI VR
    public float campoVisual = 85f;          // FOV amplio para compensar el aumento de las lentes físicas
    [Range(0.6f, 0.9f)]
    public float escalaLente = 0.75f;        // Tamaño de los círculos de las lentes estilo YouTube VR
    public float tiempoMirada = 2f;
    public System.Action<bool> alCambiar;

    public bool Activo { get; private set; }

    CamaraExplorador explorador;
    Camera principal, ojoIzquierdo, ojoDerecho;
    
    GameObject canvasOverlayVR;
    bool giroscopioAntes;
    PlanetInfo mirando, elegido;
    float tMirada, ocultarTextoEn;
    Vector2 inicioToque;
    float tiempoToque;
    bool toqueSobreUI;

    public bool Alternar()
    {
        if (Activo) Salir();
        else Entrar();
        return Activo;
    }

    public void Entrar()
    {
        explorador = CamaraExplorador.Instancia;
        if (explorador == null || Activo) return;
        principal = explorador.GetComponent<Camera>();
        
        if (ojoIzquierdo == null)
        {
            // Viewports divididos exactamente al 50% para cada ojo
            float ancho = 0.5f;
            float alto = 1f;

            ojoIzquierdo = CrearOjo("OjoIzquierdo", -1f, new Rect(0f, 0f, ancho, alto));
            ojoDerecho = CrearOjo("OjoDerecho", 1f, new Rect(0.5f, 0f, ancho, alto));
            
            CrearInterfazEstereoYouTubeVR(); 
        }
        
        MostrarElementos(true);
        principal.enabled = false;
        if (canvasNormal != null) canvasNormal.enabled = false;
        if (interfazVisor != null) interfazVisor.SetActive(true);
        if (quiz != null && quiz.Activo) quiz.Cerrar();
        if (selector != null)
        {
            selector.Deseleccionar();
            selector.enabled = false;
        }
        giroscopioAntes = explorador.GiroscopioActivo;
        if (explorador.GiroscopioDisponible && !explorador.GiroscopioActivo) explorador.AlternarGiroscopio();

        Activo = true;
        mirando = elegido = null;
        MostrarTexto("Mira un planeta 2 segundos\npara viajar hasta él", 5f);
        alCambiar?.Invoke(true);
    }

    public void Salir()
    {
        if (!Activo) return;
        Activo = false;
        MostrarElementos(false);
        principal.enabled = true;
        if (canvasNormal != null) canvasNormal.enabled = true;
        if (interfazVisor != null) interfazVisor.SetActive(false);
        if (selector != null) selector.enabled = true;
        if (explorador.GiroscopioActivo != giroscopioAntes) explorador.AlternarGiroscopio();
        if (elegido != null) elegido.Resaltar(false);
        elegido = mirando = null;
        alCambiar?.Invoke(false);
    }

    void MostrarElementos(bool mostrar)
    {
        if (ojoIzquierdo != null) ojoIzquierdo.gameObject.SetActive(mostrar);
        if (ojoDerecho != null) ojoDerecho.gameObject.SetActive(mostrar);
        if (canvasOverlayVR != null) canvasOverlayVR.SetActive(mostrar);
    }

    Camera CrearOjo(string nombre, float lado, Rect rect)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(principal.transform, false);
        var camara = go.AddComponent<Camera>();
        camara.CopyFrom(principal); 
        camara.rect = rect;
        camara.fieldOfView = campoVisual; // FOV calibrado para el aumento de las lentes TITI VR
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = Color.black;
        
        go.transform.localPosition = new Vector3(lado * separacionOjos * 0.5f, 0f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return camara;
    }

    void CrearInterfazEstereoYouTubeVR()
    {
        canvasOverlayVR = new GameObject("Overlay_TITI_VR");
        Canvas canvas = canvasOverlayVR.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasOverlayVR.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasOverlayVR.AddComponent<GraphicRaycaster>();

        // Línea divisoria central vertical exacta (como en YouTube VR)[cite: 1]
        GameObject lineaCentral = new GameObject("LineaDivisoria");
        lineaCentral.transform.SetParent(canvasOverlayVR.transform, false);
        Image imgLinea = lineaCentral.AddComponent<Image>();
        imgLinea.color = new Color(1f, 1f, 1f, 0.95f);

        RectTransform rectLinea = lineaCentral.GetComponent<RectTransform>();
        rectLinea.anchorMin = new Vector2(0.5f, 0f);
        rectLinea.anchorMax = new Vector2(0.5f, 1f);
        rectLinea.sizeDelta = new Vector2(3f, 0f);
        rectLinea.anchoredPosition = Vector2.zero;

        // Marcos circulares negros para cada lente (evitan la distorsión y visión doble)[cite: 1]
        CrearMarcoLente(canvasOverlayVR.transform, new Vector2(0.25f, 0.5f)); 
        CrearMarcoLente(canvasOverlayVR.transform, new Vector2(0.75f, 0.5f)); 

        // Elementos de UI limpios y centrados para cada lente (Mira y Ficha del planeta)
        CrearElementoUIEstereo(canvasOverlayVR.transform, new Vector2(0.25f, 0.5f)); 
        CrearElementoUIEstereo(canvasOverlayVR.transform, new Vector2(0.75f, 0.5f)); 
    }

    void CrearMarcoLente(Transform padre, Vector2 anclaCentro)
    {
        GameObject marco = new GameObject("EnmascaradoLente");
        marco.transform.SetParent(padre, false);
        
        Outline borde = marco.AddComponent<Outline>();
        borde.effectColor = Color.black;
        borde.effectDistance = new Vector2(160f, 160f); // Aro negro envolvente que enmarca la lente física

        Image img = marco.AddComponent<Image>();
        img.color = Color.clear;

        RectTransform rt = marco.GetComponent<RectTransform>();
        rt.anchorMin = anclaCentro;
        rt.anchorMax = anclaCentro;
        rt.sizeDelta = new Vector2(960f * escalaLente, 1080f * escalaLente);
        rt.anchoredPosition = Vector2.zero;
    }

    void CrearElementoUIEstereo(Transform padre, Vector2 anclaCentro)
    {
        GameObject contenedor = new GameObject("LenteUI_" + (anclaCentro.x < 0.5f ? "Izq" : "Der"));
        contenedor.transform.SetParent(padre, false);
        RectTransform rtContenedor = contenedor.AddComponent<RectTransform>();
        rtContenedor.anchorMin = anclaCentro;
        rtContenedor.anchorMax = anclaCentro;
        rtContenedor.sizeDelta = new Vector2(960, 1080);
        rtContenedor.anchoredPosition = Vector2.zero;

        // Mira centrada individualmente por lente
        GameObject miraObj = new GameObject("MiraUI");
        miraObj.transform.SetParent(contenedor.transform, false);
        Image imgMira = miraObj.AddComponent<Image>();
        imgMira.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        
        RectTransform rtMira = miraObj.GetComponent<RectTransform>();
        rtMira.anchorMin = new Vector2(0.5f, 0.5f);
        rtMira.anchorMax = new Vector2(0.5f, 0.5f);
        rtMira.sizeDelta = new Vector2(24f, 24f);
        rtMira.anchoredPosition = Vector2.zero;

        // Ficha de información detallada del planeta limpia y centrada
        GameObject textoObj = new GameObject("TextoFichaUI");
        textoObj.transform.SetParent(contenedor.transform, false);
        TextMeshProUGUI txt = textoObj.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 24;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.enableWordWrapping = true;

        Outline outline = textoObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, 2f);

        RectTransform rtTexto = textoObj.GetComponent<RectTransform>();
        rtTexto.anchorMin = new Vector2(0.5f, 0.5f);
        rtTexto.anchorMax = new Vector2(0.5f, 0.5f);
        rtTexto.sizeDelta = new Vector2(700f, 250f);
        rtTexto.anchoredPosition = new Vector2(0f, -120f);
        txt.text = "";
    }

    void Update()
    {
        if (!Activo) return;
        if (PresionoAtras()) { Salir(); return; }

        var t = principal.transform;
        PlanetInfo actual = null;
        if (Physics.Raycast(t.position, t.forward, out RaycastHit golpe, 100f)) golpe.collider.TryGetComponent(out actual);
        if (actual != mirando)
        {
            mirando = actual;
            tMirada = 0f;
        }

        float progreso = 0f;
        if (mirando != null && mirando != elegido)
        {
            tMirada += Time.unscaledDeltaTime;
            progreso = Mathf.Clamp01(tMirada / tiempoMirada);
            MostrarTexto($"<b>{mirando.datos.nombre}</b>\n<size=70%>Mantén la mirada para viajar</size>", 0.2f);
            if (progreso >= 1f) Elegir(mirando);
        }

        if (Toco())
        {
            if (mirando != null) Elegir(mirando);
            else
            {
                if (elegido != null) elegido.Resaltar(false);
                elegido = null;
                explorador.VistaGeneral();
                MostrarTexto("Vista general", 2f);
            }
        }

        float escalaMiraActual = 24f * (1f + progreso * 1.5f);
        Color colorMiraActual = Color.Lerp(new Color(0.8f, 0.8f, 0.8f, 0.9f), new Color(1f, 0.85f, 0.2f, 1f), progreso);

        if (canvasOverlayVR != null)
        {
            foreach (Transform hijo in canvasOverlayVR.transform)
            {
                if (hijo.name.StartsWith("LenteUI_"))
                {
                    Transform m = hijo.Find("MiraUI");
                    if (m != null)
                    {
                        m.GetComponent<RectTransform>().sizeDelta = new Vector2(escalaMiraActual, escalaMiraActual);
                        m.GetComponent<Image>().color = colorMiraActual;
                    }
                }
            }
        }

        if (Time.unscaledTime > ocultarTextoEn)
        {
            MostrarTexto("", 0f);
        }
    }

    public void ElegirPorVoz(PlanetInfo planeta)
    {
        if (Activo) Elegir(planeta);
    }

    void Elegir(PlanetInfo planeta)
    {
        if (elegido != null) elegido.Resaltar(false);
        elegido = planeta;
        elegido.Resaltar(true);
        tMirada = 0f;
        explorador.Viajar(planeta.transform);
        if (SonidoEspacial.Instancia != null) SonidoEspacial.Instancia.Toque();
        var d = planeta.datos;
        
        string ficha = $"<b><color=#F2B533>{d.nombre}</color></b>\n<size=75%>{d.tipo}  ·  {d.diametro}\n<i>{d.datoCurioso}</i></size>";
        MostrarTexto(ficha, 9f);
        
        Narrador.Instancia?.Decir(ComandosVoz.TextoFicha(d));
    }

    void MostrarTexto(string texto, float segundos)
    {
        if (canvasOverlayVR != null)
        {
            foreach (Transform hijo in canvasOverlayVR.transform)
            {
                if (hijo.name.StartsWith("LenteUI_"))
                {
                    TextMeshProUGUI txtUI = hijo.Find("TextoFichaUI")?.GetComponent<TextMeshProUGUI>();
                    if (txtUI != null) txtUI.text = texto;
                }
            }
        }
        ocultarTextoEn = Time.unscaledTime + segundos;
    }

    bool Toco()
    {
        bool presiono, solto;
        Vector2 posicion;
#if ENABLE_INPUT_SYSTEM
        var puntero = Pointer.current;
        posicion = puntero != null ? puntero.position.ReadValue() : Vector2.zero;
        presiono = puntero != null && puntero.press.wasPressedThisFrame;
        solto = puntero != null && puntero.press.wasReleasedThisFrame;
        int id = -1;
#else
        int id = -1;
        if (Input.touchCount > 0)
        {
            var toque = Input.GetTouch(0);
            posicion = toque.position;
            presiono = toque.phase == TouchPhase.Began;
            solto = toque.phase == TouchPhase.Ended;
            id = toque.fingerId;
        }
        else
        {
            posicion = Input.mousePosition;
            presiono = Input.GetMouseButtonDown(0);
            solto = Input.GetMouseButtonUp(0);
        }
#endif
        if (presiono)
        {
            inicioToque = posicion;
            tiempoToque = Time.unscaledTime;
            toqueSobreUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id);
        }
        return solto && !toqueSobreUI && (posicion - inicioToque).magnitude < 20f && Time.unscaledTime - tiempoToque < 0.5f;
    }

    static bool PresionoAtras()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
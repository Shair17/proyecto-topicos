using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Modo visor: RV INMERSIVA con un visor tipo Google Cardboard (el celular va dentro del visor).
// - Divide la pantalla en dos imágenes, una por ojo, tomadas desde puntos un poco separados: eso da profundidad
//   (visión estereoscópica, como hacen los ojos).
// - Se mira girando la cabeza (giroscopio). En PC se puede probar arrastrando el mouse.
// - Se elige con la mirada: mantener la mira sobre un planeta 2 segundos viaja hasta él.
//   Tocar la pantalla (o el botón del visor) viaja de inmediato; tocar mirando al vacío vuelve a la vista general.
// - Atrás (Android) o Esc salen del modo.
public class ModoVisor : MonoBehaviour
{
    public Canvas canvasNormal;
    [Tooltip("Interfaz mínima del modo visor (botón Salir), en su propio Canvas.")]
    public GameObject interfazVisor;
    public PlanetSelector selector;
    public QuizPlanetas quiz;
    public Material materialAditivo;
    public float separacionOjos = 0.0025f;   // proporcional a la escala de la escena
    public float campoVisual = 75f;
    public float tiempoMirada = 2f;
    public System.Action<bool> alCambiar;

    const float DistanciaMira = 0.05f;

    public bool Activo { get; private set; }

    CamaraExplorador explorador;
    Camera principal, ojoIzquierdo, ojoDerecho;
    Transform mira;
    Material materialMira;
    TextMeshPro textoVR;
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
            ojoIzquierdo = CrearOjo("OjoIzquierdo", -1f, new Rect(0f, 0f, 0.5f, 1f));
            ojoDerecho = CrearOjo("OjoDerecho", 1f, new Rect(0.5f, 0f, 0.5f, 1f));
            CrearMiraYTexto();
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
        ojoIzquierdo.gameObject.SetActive(mostrar);
        ojoDerecho.gameObject.SetActive(mostrar);
        mira.gameObject.SetActive(mostrar);
        textoVR.gameObject.SetActive(mostrar);
    }

    Camera CrearOjo(string nombre, float lado, Rect rect)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(principal.transform, false);
        var camara = go.AddComponent<Camera>();
        camara.CopyFrom(principal); // copia fondo, planos de recorte, etc. (también la posición: se corrige abajo)
        camara.rect = rect;
        camara.fieldOfView = campoVisual;
        go.transform.localPosition = new Vector3(lado * separacionOjos * 0.5f, 0f, 0f);
        go.transform.localRotation = Quaternion.identity;
        return camara;
    }

    void CrearMiraYTexto()
    {
        // Mira: un punto brillante en el centro de la vista.
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Mira";
        Destroy(quad.GetComponent<Collider>());
        mira = quad.transform;
        mira.SetParent(principal.transform, false);
        mira.localPosition = new Vector3(0f, 0f, DistanciaMira);
        materialMira = new Material(materialAditivo) { mainTexture = Procedural.TexturaBrillo() };
        quad.GetComponent<Renderer>().material = materialMira;

        // Texto flotando debajo de la mira (lo ven los dos ojos).
        var go = new GameObject("TextoVisor");
        go.transform.SetParent(principal.transform, false);
        go.transform.localPosition = new Vector3(0f, -0.004f, DistanciaMira);
        go.transform.localScale = Vector3.one * 0.032f;
        textoVR = go.AddComponent<TextMeshPro>();
        textoVR.fontSize = 1f;
        textoVR.alignment = TextAlignmentOptions.Top;
        textoVR.rectTransform.sizeDelta = new Vector2(1.4f, 1.6f);
        textoVR.rectTransform.pivot = new Vector2(0.5f, 1f);
        textoVR.enableWordWrapping = true;
        textoVR.outlineWidth = 0.25f;
        textoVR.outlineColor = new Color32(0, 0, 0, 220);
        textoVR.text = "";
    }

    void Update()
    {
        if (!Activo) return;
        if (PresionoAtras()) { Salir(); return; }

        // ¿Qué planeta está en el centro de la vista?
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

        // La mira crece y se pone dorada mientras se mantiene la mirada.
        mira.localScale = Vector3.one * 0.0032f * (1f + progreso * 1.6f);
        materialMira.SetColor("_TintColor", Color.Lerp(new Color(0.5f, 0.5f, 0.5f, 0.45f), new Color(0.6f, 0.45f, 0.1f, 0.6f), progreso));
        if (Time.unscaledTime > ocultarTextoEn) textoVR.text = "";
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
        MostrarTexto($"<b><color=#F2B533>{d.nombre}</color></b>\n<size=75%>{d.tipo}  ·  {d.diametro}\n<i>{d.datoCurioso}</i></size>", 9f);
    }

    void MostrarTexto(string texto, float segundos)
    {
        textoVR.text = texto;
        ocultarTextoEn = Time.unscaledTime + segundos;
    }

    // Un toque corto (no un arrastre) que no empezó sobre el botón Salir.
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

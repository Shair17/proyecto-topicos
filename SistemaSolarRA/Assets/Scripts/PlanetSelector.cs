using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Detecta un toque corto (o clic) sobre un planeta y muestra su ficha.
// Se elige al SOLTAR y solo si casi no se movió el dedo/mouse: arrastrar sirve para mirar alrededor.
public class PlanetSelector : MonoBehaviour
{
    public Camera camara;
    public InfoPanelUI panel;

    const float MovimientoMaximo = 20f;   // píxeles
    const float DuracionMaxima = 0.5f;    // segundos

    PlanetInfo seleccionado;
    Vector2 inicio;
    float tiempoInicio;
    bool empezoSobreUI;

    public PlanetInfo Seleccionado => seleccionado;

    void Start()
    {
        if (camara == null) camara = Camera.main;
    }

    void Update()
    {
        if (Presiono(out Vector2 posicion))
        {
            inicio = posicion;
            tiempoInicio = Time.unscaledTime;
            empezoSobreUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(PunteroId());
        }

        if (!Solto(out posicion) || empezoSobreUI) return;
        if ((posicion - inicio).magnitude > MovimientoMaximo || Time.unscaledTime - tiempoInicio > DuracionMaxima) return;

        Ray rayo = camara.ScreenPointToRay(posicion);
        if (Physics.Raycast(rayo, out RaycastHit golpe, 100f) &&
            golpe.collider.TryGetComponent(out PlanetInfo planeta))
        {
            Seleccionar(planeta);
        }
    }

    public void Seleccionar(PlanetInfo planeta)
    {
        if (seleccionado != null) seleccionado.Resaltar(false);
        seleccionado = planeta;
        seleccionado.Resaltar(true);
        panel.Mostrar(planeta.datos);
        if (SonidoEspacial.Instancia != null) SonidoEspacial.Instancia.Toque();
    }

    public void Deseleccionar()
    {
        if (seleccionado != null) seleccionado.Resaltar(false);
        seleccionado = null;
        panel.Ocultar();
    }

    // Botón "Viajar" de la ficha: la cámara vuela hasta el planeta y lo sigue.
    public void ViajarAlSeleccionado()
    {
        if (seleccionado != null && CamaraExplorador.Instancia != null)
            CamaraExplorador.Instancia.Viajar(seleccionado.transform);
    }

    static bool Presiono(out Vector2 posicion)
    {
#if ENABLE_INPUT_SYSTEM
        var puntero = Pointer.current;
        posicion = puntero != null ? puntero.position.ReadValue() : Vector2.zero;
        return puntero != null && puntero.press.wasPressedThisFrame;
#else
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            posicion = t.position;
            return t.phase == TouchPhase.Began;
        }
        posicion = Input.mousePosition;
        return Input.GetMouseButtonDown(0);
#endif
    }

    static bool Solto(out Vector2 posicion)
    {
#if ENABLE_INPUT_SYSTEM
        var puntero = Pointer.current;
        posicion = puntero != null ? puntero.position.ReadValue() : Vector2.zero;
        return puntero != null && puntero.press.wasReleasedThisFrame;
#else
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            posicion = t.position;
            return t.phase == TouchPhase.Ended;
        }
        posicion = Input.mousePosition;
        return Input.GetMouseButtonUp(0);
#endif
    }

    static int PunteroId()
    {
#if ENABLE_INPUT_SYSTEM
        return -1;
#else
        return Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
#endif
    }
}

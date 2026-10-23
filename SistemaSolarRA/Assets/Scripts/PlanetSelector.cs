using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Detecta el toque (o clic en el editor) sobre un planeta y muestra su ficha.
// Va en cualquier objeto de la escena, por ejemplo en "GestorApp".
public class PlanetSelector : MonoBehaviour
{
    public Camera camara;
    public InfoPanelUI panel;

    PlanetInfo seleccionado;

    void Start()
    {
        if (camara == null) camara = Camera.main;
    }

    void Update()
    {
        if (!PresionoEstaFrame(out Vector2 posicion)) return;

        // Ignorar toques sobre botones o paneles de la interfaz.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(PunteroId())) return;

        Ray rayo = camara.ScreenPointToRay(posicion);
        if (Physics.Raycast(rayo, out RaycastHit golpe, 10f) &&
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
    }

    public void Deseleccionar()
    {
        if (seleccionado != null) seleccionado.Resaltar(false);
        seleccionado = null;
        panel.Ocultar();
    }

    static bool PresionoEstaFrame(out Vector2 posicion)
    {
#if ENABLE_INPUT_SYSTEM
        var puntero = Pointer.current;
        posicion = puntero != null ? puntero.position.ReadValue() : Vector2.zero;
        return puntero != null && puntero.press.wasPressedThisFrame;
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            posicion = Input.GetTouch(0).position;
            return true;
        }
        posicion = Input.mousePosition;
        return Input.GetMouseButtonDown(0);
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

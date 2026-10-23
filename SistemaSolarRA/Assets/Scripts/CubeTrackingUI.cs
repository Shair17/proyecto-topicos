using UnityEngine;

// Muestra "Apunta la cámara al cubo" cuando no se ve el cubo y oculta los controles.
// Vuforia (Default Observer Event Handler) apaga los Renderers del sistema solar cuando pierde el cubo;
// aquí solo se revisa si el Sol está visible. Así este script no depende de la API de Vuforia.
public class CubeTrackingUI : MonoBehaviour
{
    public GameObject mensajeBuscar;
    public GameObject controles;
    [Tooltip("Si se deja vacío, usa el Sol creado por SolarSystemBuilder.")]
    public Renderer referencia;

    bool? visible;

    void Update()
    {
        if (referencia == null)
        {
            var sistema = FindObjectOfType<SolarSystemBuilder>();
            if (sistema != null) referencia = sistema.GetComponentInChildren<Renderer>();
        }
        MostrarCubo(referencia != null && referencia.enabled);
    }

    void MostrarCubo(bool seVe)
    {
        if (visible == seVe) return;
        visible = seVe;
        if (mensajeBuscar != null) mensajeBuscar.SetActive(!seVe);
        if (controles != null) controles.SetActive(seVe);
    }
}

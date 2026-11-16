using UnityEngine;

// Mantiene finas las líneas de órbita y las estelas: su ancho crece con la distancia a la cámara,
// así se ven igual de delgadas desde lejos que de cerca (si no, de cerca parecen bandas gruesas).
[RequireComponent(typeof(LineRenderer))]
public class AnchoSegunDistancia : MonoBehaviour
{
    [Tooltip("Mayor que 0: la línea es un círculo de este radio (plano XZ local). 0: se mide hasta 'referencia'.")]
    public float radioOrbita;
    public Transform referencia;
    public float anchoPorMetro = 0.003f;
    public float anchoMaximo = 0.0012f;

    LineRenderer linea;
    Camera camara;

    void Awake()
    {
        linea = GetComponent<LineRenderer>();
    }

    void LateUpdate()
    {
        // La cámara del explorador (en el modo visor se apaga, pero sigue moviéndose con los dos "ojos").
        if (camara == null)
        {
            camara = CamaraExplorador.Instancia != null ? CamaraExplorador.Instancia.GetComponent<Camera>() : Camera.main;
            if (camara == null) return;
        }
        Vector3 posCamara = camara.transform.position;
        float distancia;
        if (radioOrbita > 0f)
        {
            // Distancia de la cámara al punto más cercano del círculo.
            Vector3 local = transform.InverseTransformPoint(posCamara);
            float horizontal = new Vector2(local.x, local.z).magnitude;
            distancia = Mathf.Sqrt((horizontal - radioOrbita) * (horizontal - radioOrbita) + local.y * local.y);
        }
        else distancia = Vector3.Distance(posCamara, referencia != null ? referencia.position : transform.position);
        linea.widthMultiplier = Mathf.Min(anchoMaximo, distancia * anchoPorMetro);
    }
}

using UnityEngine;

// Hace girar el pivote (traslación alrededor del Sol) y el planeta sobre su eje (rotación).
public class PlanetOrbit : MonoBehaviour
{
    // Lo controla SolarSystemController (pausa = 0).
    public static float multiplicadorVelocidad = 1f;

    [Header("Modo Real (Basado en Días Terrestres)")]
    public bool usarDatosReales = false;
    [Tooltip("Días terrestres que tarda en completar una órbita (año del planeta).")]
    public float diasOrbita = 365.25f;
    [Tooltip("Días terrestres que tarda en dar una vuelta sobre su propio eje (día del planeta). Negativo si rota al revés (ej. Venus).")]
    public float diasRotacion = 1f;

    [Header("Modo Manual (Grados por segundo)")]
    public float velocidadOrbita = 10f;   // grados por segundo
    public float velocidadRotacion = 30f; // grados por segundo
    
    public Transform cuerpo;

    // Escala base para acelerar o desacelerar el tiempo del sistema solar entero si se desea
    public static float factorEscalaTiempo = 10f; 

    void Update()
    {
        float dt = Time.deltaTime * multiplicadorVelocidad;

        float vOrbita = velocidadOrbita;
        float vRotacion = velocidadRotacion;

        if (usarDatosReales)
        {
            if (diasOrbita > 0.0001f)
                vOrbita = (360f / diasOrbita) * factorEscalaTiempo;
            
            if (Mathf.Abs(diasRotacion) > 0.0001f)
                vRotacion = (360f / diasRotacion) * factorEscalaTiempo;
            else
                vRotacion = 0f;
        }

        transform.Rotate(0f, vOrbita * dt, 0f, Space.Self);
        if (cuerpo != null)
            cuerpo.Rotate(0f, vRotacion * dt, 0f, Space.Self);
    }
}
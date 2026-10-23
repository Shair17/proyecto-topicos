using UnityEngine;

// Hace girar el pivote (traslación alrededor del Sol) y el planeta sobre su eje (rotación).
public class PlanetOrbit : MonoBehaviour
{
    // Lo controla SolarSystemController (pausa = 0).
    public static float multiplicadorVelocidad = 1f;

    public float velocidadOrbita = 10f;   // grados por segundo
    public float velocidadRotacion = 30f; // grados por segundo
    public Transform cuerpo;

    void Update()
    {
        float dt = Time.deltaTime * multiplicadorVelocidad;
        transform.Rotate(0f, velocidadOrbita * dt, 0f, Space.Self);
        if (cuerpo != null)
            cuerpo.Rotate(0f, velocidadRotacion * dt, 0f, Space.Self);
    }
}

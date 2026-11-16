using UnityEngine;

// Mueve el cometa por una órbita elíptica: va rápido cerca del Sol y lento lejos (segunda ley de Kepler).
// La cola siempre apunta en dirección contraria al Sol y crece cuando se acerca a él.
// Lo crea SolarSystemBuilder: Cometa (plano inclinado) > ancla (se mueve) > núcleo.
public class Cometa : MonoBehaviour
{
    public Transform ancla;
    public ParticleSystem cola;
    public float perihelio = 0.07f;   // distancia mínima al Sol (en la escena)
    public float afelio = 0.33f;      // distancia máxima
    public float periodo = 70f;       // segundos por vuelta a velocidad 1

    float angulo = -1.3f;
    float p, e, h;

    void Start()
    {
        float a = (perihelio + afelio) * 0.5f;
        e = (afelio - perihelio) / (afelio + perihelio);
        p = a * (1f - e * e);
        float b = a * Mathf.Sqrt(1f - e * e);
        h = 2f * Mathf.PI * a * b / periodo;   // área barrida por segundo x 2 (constante en toda la órbita)
        Mover();
    }

    void Update()
    {
        float r = Radio();
        // Órbita retrógrada como la del Halley: gira al revés que los planetas.
        angulo -= h / (r * r) * Time.deltaTime * PlanetOrbit.multiplicadorVelocidad;
        Mover();
    }

    float Radio() => p / (1f + e * Mathf.Cos(angulo));

    void Mover()
    {
        float r = Radio();
        ancla.localPosition = new Vector3(r * Mathf.Cos(angulo), 0f, r * Mathf.Sin(angulo));
        if (cola == null) return;

        Vector3 lejosDelSol = ancla.position - transform.parent.position;
        if (lejosDelSol.sqrMagnitude > 1e-10f) cola.transform.rotation = Quaternion.LookRotation(lejosDelSol);
        var emision = cola.emission;
        emision.rateOverTime = Mathf.Lerp(300f, 70f, Mathf.InverseLerp(perihelio, afelio * 0.7f, r));
    }
}

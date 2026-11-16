using UnityEngine;

// Va en cada planeta: guarda sus datos y lo resalta (crece suavemente y brilla) cuando se selecciona.
public class PlanetInfo : MonoBehaviour
{
    public PlanetData datos;
    [Tooltip("Partículas del brillo de selección (las crea SolarSystemBuilder).")]
    public ParticleSystem halo;
    public float tamanoHalo = 0.02f;
    public Color colorHalo = Color.white;

    Vector3 escalaOriginal;
    bool activo;
    float factor = 1f;

    void Awake()
    {
        escalaOriginal = transform.localScale;
    }

    public void Resaltar(bool encender)
    {
        activo = encender;
        if (halo == null) return;
        // El halo se enciende emitiendo una partícula (no con SetActive/enabled, que Vuforia vuelve a cambiar).
        halo.Clear();
        if (encender) Procedural.EmitirFija(halo, Vector3.zero, tamanoHalo, colorHalo);
    }

    void Update()
    {
        float objetivo = activo ? 1.4f : 1f;
        factor = Mathf.Lerp(factor, objetivo, 1f - Mathf.Exp(-10f * Time.deltaTime));
        transform.localScale = escalaOriginal * factor;
        if (activo && halo != null)
            halo.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.time * 4f));
    }
}

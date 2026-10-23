using UnityEngine;

// Construye el sistema solar como hijo de este objeto (que debe estar dentro del Multi Target del cubo).
// Así no hace falta armar los planetas a mano: basta con asignar los materiales en el Inspector.
public class SolarSystemBuilder : MonoBehaviour
{
    [Header("Materiales (crear en Assets/Materials)")]
    [Tooltip("Material Lit/Standard que se copia para cada planeta.")]
    public Material materialPlaneta;
    [Tooltip("Material Unlit o con emisión para el Sol. Si se deja vacío se usa materialPlaneta.")]
    public Material materialSol;
    [Tooltip("Material para las líneas de órbita (p. ej. shader Sprites/Default).")]
    public Material materialOrbita;

    [Header("Texturas opcionales (mismo orden: Sol, Mercurio ... Neptuno)")]
    public Texture2D[] texturas = new Texture2D[9];

    [Header("Escala (en metros; el cubo mide 0,06 m)")]
    public float radioSistema = 0.18f;
    public float tamanoBase = 0.06f;
    public bool mostrarOrbitas = true;

    public PlanetData[] Planetas { get; private set; }

    void Awake()
    {
        Planetas = PlanetData.SistemaSolar();
        for (int i = 0; i < Planetas.Length; i++)
            CrearCuerpo(Planetas[i], i);
    }

    void CrearCuerpo(PlanetData datos, int indice)
    {
        bool esSol = datos.radioOrbitaRelativo == 0f;

        // Pivote que gira alrededor del Sol; el planeta va desplazado dentro de él.
        var pivote = new GameObject("Orbita_" + datos.nombre).transform;
        pivote.SetParent(transform, false);
        pivote.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cuerpo.name = datos.nombre;
        cuerpo.transform.SetParent(pivote, false);
        cuerpo.transform.localPosition = new Vector3(RadioOrbita(datos), 0f, 0f);
        cuerpo.transform.localScale = Vector3.one * datos.tamanoRelativo * tamanoBase;

        var render = cuerpo.GetComponent<Renderer>();
        var baseMat = esSol && materialSol != null ? materialSol : materialPlaneta;
        if (baseMat != null)
        {
            render.material = new Material(baseMat) { color = datos.color };
            if (indice < texturas.Length && texturas[indice] != null)
            {
                render.material.mainTexture = texturas[indice];
                render.material.color = Color.white;
            }
        }

        var info = cuerpo.AddComponent<PlanetInfo>();
        info.datos = datos;

        var orbita = pivote.gameObject.AddComponent<PlanetOrbit>();
        // Raíz cuadrada de la velocidad real: mantiene el orden pero los planetas lejanos sí se mueven.
        orbita.velocidadOrbita = Mathf.Sqrt(datos.velocidadOrbita) * 20f;
        orbita.cuerpo = cuerpo.transform;

        if (datos.nombre == "Saturno")
            CrearAnillo(cuerpo.transform, datos.color);

        if (!esSol && mostrarOrbitas && materialOrbita != null)
            CrearLineaOrbita(RadioOrbita(datos));
    }

    // Las órbitas empiezan fuera del Sol (que es un poco más grande que el cubo).
    float RadioOrbita(PlanetData datos)
    {
        if (datos.radioOrbitaRelativo == 0f) return 0f;
        float radioSol = Planetas[0].tamanoRelativo * tamanoBase * 0.5f;
        return radioSol + 0.01f + datos.radioOrbitaRelativo * radioSistema;
    }

    void CrearAnillo(Transform planeta, Color color)
    {
        var anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anillo.name = "Anillo";
        Destroy(anillo.GetComponent<Collider>());
        anillo.transform.SetParent(planeta, false);
        anillo.transform.localScale = new Vector3(1.8f, 0.01f, 1.8f);
        anillo.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        var render = anillo.GetComponent<Renderer>();
        if (materialPlaneta != null)
            render.material = new Material(materialPlaneta) { color = color * 0.85f };
    }

    void CrearLineaOrbita(float radio)
    {
        const int segmentos = 96;
        var go = new GameObject("LineaOrbita");
        go.transform.SetParent(transform, false);
        var linea = go.AddComponent<LineRenderer>();
        linea.useWorldSpace = false;
        linea.loop = true;
        linea.positionCount = segmentos;
        linea.widthMultiplier = 0.0015f;
        linea.material = materialOrbita;
        linea.startColor = linea.endColor = new Color(1f, 1f, 1f, 0.35f);
        for (int i = 0; i < segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
            linea.SetPosition(i, new Vector3(Mathf.Cos(a) * radio, 0f, Mathf.Sin(a) * radio));
        }
    }
}

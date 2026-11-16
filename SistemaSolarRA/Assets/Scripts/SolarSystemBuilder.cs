using UnityEngine;

// Construye el sistema solar como hijo de este objeto. La cámara (CamaraExplorador) flota dentro de él.
// Los materiales y texturas los asigna el menú "Sistema Solar" del editor; todo lo demás se crea aquí.
public class SolarSystemBuilder : MonoBehaviour
{
    [Header("Materiales (los crea el menú Sistema Solar en Assets/Materials)")]
    [Tooltip("Standard: se copia para cada planeta.")]
    public Material materialPlaneta;
    [Tooltip("Unlit/Texture para el Sol (brilla sin necesitar luz).")]
    public Material materialSol;
    [Tooltip("Sprites/Default: líneas de órbita y estelas.")]
    public Material materialOrbita;
    [Tooltip("Legacy Shaders/Transparent/Diffuse: anillo de Saturno y nubes de la Tierra.")]
    public Material materialTransparente;
    [Tooltip("Legacy Shaders/Particles/Additive: resplandor del Sol, estrellas, asteroides y halos.")]
    public Material materialAditivo;
    [Tooltip("SistemaSolar/Atmosfera: halo de atmósfera alrededor de los planetas.")]
    public Material materialAtmosfera;

    [Header("Texturas (mismo orden: Sol, Mercurio ... Neptuno)")]
    public Texture2D[] texturas = new Texture2D[9];
    public Texture2D texturaAnillo;
    public Texture2D texturaNubes;
    public Texture2D texturaLuna;

    [Header("Escala (en metros; el sistema completo mide unos 0,5 m de radio)")]
    public float radioSistema = 0.18f;
    public float tamanoBase = 0.06f;

    [Header("Efectos")]
    public bool mostrarOrbitas = true;
    public bool mostrarEstelas = true;
    public bool mostrarAsteroides = true;
    public bool mostrarEstrellas = true;
    public bool mostrarLuna = true;
    public bool mostrarCometa = true;
    [Tooltip("Apaga las luces direccionales de la escena para que el Sol sea la única luz (lado de día y de noche).")]
    public bool apagarLucesDireccionales = true;
    public Color luzAmbiente = new Color(0.1f, 0.1f, 0.13f);

    public PlanetData[] Planetas { get; private set; }
    public Renderer Sol { get; private set; }

    AparicionSistema aparicion;
    Light luzSol;

    void Awake()
    {
        Planetas = PlanetData.SistemaSolar();
        ConfigurarIluminacion();
        aparicion = gameObject.AddComponent<AparicionSistema>();

        for (int i = 0; i < Planetas.Length; i++)
            CrearCuerpo(Planetas[i], i);
        if (mostrarAsteroides) CrearCinturonAsteroides();
        if (mostrarEstrellas) CrearEstrellas();
        if (mostrarCometa) CrearCometa();

        aparicion.Configurar(Sol, luzSol);
    }

    void ConfigurarIluminacion()
    {
        if (apagarLucesDireccionales)
            foreach (var l in FindObjectsOfType<Light>())
                if (l.type == LightType.Directional) l.enabled = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = luzAmbiente;
        QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 2);
    }

    void CrearCuerpo(PlanetData datos, int indice)
    {
        bool esSol = indice == 0;
        float diametro = datos.tamanoRelativo * tamanoBase;
        float radioOrbita = RadioOrbita(datos.radioOrbitaRelativo);

        // pivote (gira alrededor del Sol) > ancla (posición del planeta) > cuerpo (gira sobre su eje inclinado)
        var pivote = new GameObject("Orbita_" + datos.nombre).transform;
        pivote.SetParent(transform, false);
        if (!esSol) pivote.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        var ancla = new GameObject("Posicion_" + datos.nombre).transform;
        ancla.SetParent(pivote, false);
        ancla.localPosition = new Vector3(radioOrbita, 0f, 0f);

        var cuerpo = CrearEsfera(datos.nombre, ancla, diametro, datos.inclinacionEje);
        AsignarMaterial(cuerpo, esSol, datos.color, indice < texturas.Length ? texturas[indice] : null);
        AgrandarZonaDeToque(cuerpo, diametro);

        var orbita = pivote.gameObject.AddComponent<PlanetOrbit>();
        // Raíz cuadrada de la velocidad real: mantiene el orden, pero los planetas lejanos sí se mueven.
        orbita.velocidadOrbita = Mathf.Sqrt(datos.velocidadOrbita) * 20f;
        orbita.velocidadRotacion = datos.velocidadGiro;
        orbita.cuerpo = cuerpo;

        var info = cuerpo.gameObject.AddComponent<PlanetInfo>();
        info.datos = datos;
        CrearHalo(info, cuerpo, diametro * (esSol ? 1.8f : 2.8f), datos.color);

        if (esSol)
        {
            Sol = cuerpo.GetComponent<Renderer>();
            CrearEfectosSol(ancla, diametro);
            CrearAtmosfera(cuerpo, new Color(1f, 0.5f, 0.1f), 1.35f, 1.3f);
            return;
        }

        switch (datos.nombre)
        {
            case "Venus": CrearAtmosfera(cuerpo, new Color(1f, 0.85f, 0.55f), 1.12f, 0.8f); break;
            case "Tierra":
                CrearNubes(cuerpo);
                CrearAtmosfera(cuerpo, new Color(0.3f, 0.6f, 1f), 1.12f, 1.3f);
                if (mostrarLuna) CrearLuna(ancla, diametro);
                break;
            case "Marte": CrearAtmosfera(cuerpo, new Color(1f, 0.5f, 0.3f), 1.07f, 0.5f); break;
            case "Júpiter": CrearAtmosfera(cuerpo, new Color(1f, 0.85f, 0.65f), 1.06f, 0.4f); break;
            case "Saturno": CrearAnillo(cuerpo, datos.color); break;
            case "Urano": CrearAtmosfera(cuerpo, new Color(0.55f, 0.9f, 1f), 1.1f, 0.9f); break;
            case "Neptuno": CrearAtmosfera(cuerpo, new Color(0.3f, 0.5f, 1f), 1.1f, 1f); break;
        }

        Transform linea = mostrarOrbitas ? CrearLineaOrbita(radioOrbita) : null;
        Transform estela = mostrarEstelas ? CrearEstela(pivote, ancla, radioOrbita, diametro, datos.color) : null;
        aparicion.Agregar(ancla, linea, estela);
    }

    // Las órbitas empiezan fuera del Sol. Público: IndicadorDistancia lo usa para convertir a distancias reales.
    public float RadioOrbita(float relativo)
    {
        if (relativo == 0f) return 0f;
        float radioSol = Planetas[0].tamanoRelativo * tamanoBase * 0.5f;
        return radioSol + 0.01f + relativo * radioSistema;
    }

    Transform CrearEsfera(string nombre, Transform padre, float diametro, float inclinacion)
    {
        var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        esfera.name = nombre;
        esfera.SetParent(padre, false);
        esfera.localScale = Vector3.one * diametro;
        esfera.localRotation = Quaternion.Euler(0f, 0f, inclinacion);
        return esfera;
    }

    void AsignarMaterial(Transform cuerpo, bool esSol, Color color, Texture2D textura)
    {
        var render = cuerpo.GetComponent<Renderer>();
        if (esSol && materialSol != null && textura != null)
        {
            render.material = new Material(materialSol) { mainTexture = textura };
            return;
        }
        if (materialPlaneta == null) return;
        var mat = new Material(materialPlaneta);
        if (textura != null)
        {
            mat.mainTexture = textura;
            mat.color = Color.white;
        }
        else mat.color = color;
        if (esSol)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color);
        }
        render.material = mat;
    }

    // En el celular los planetas pequeños son difíciles de tocar: se agranda su collider (no se ve).
    static void AgrandarZonaDeToque(Transform cuerpo, float diametro)
    {
        var col = cuerpo.GetComponent<SphereCollider>();
        if (col != null) col.radius = Mathf.Max(0.5f, 0.008f / diametro);
    }

    static void QuitarCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    Material MaterialAditivo(Color tinte)
    {
        var mat = new Material(materialAditivo) { mainTexture = Procedural.TexturaBrillo() };
        mat.SetColor("_TintColor", tinte);
        return mat;
    }

    // Brillo que aparece alrededor del planeta seleccionado (lo maneja PlanetInfo).
    void CrearHalo(PlanetInfo info, Transform cuerpo, float tamano, Color color)
    {
        if (materialAditivo == null) return;
        var halo = Procedural.CrearParticulas("Halo", cuerpo.parent, MaterialAditivo(new Color(0.5f, 0.5f, 0.5f, 0.5f)), 1, false);
        halo.transform.localPosition = cuerpo.localPosition;
        info.halo = halo;
        info.tamanoHalo = tamano;
        info.colorHalo = Color.Lerp(color, Color.white, 0.35f);
    }

    void CrearEfectosSol(Transform ancla, float diametro)
    {
        // Luz puntual: ilumina el lado de los planetas que mira al Sol.
        var luzGo = new GameObject("LuzDelSol");
        luzGo.transform.SetParent(ancla, false);
        luzSol = luzGo.AddComponent<Light>();
        luzSol.type = LightType.Point;
        luzSol.color = new Color(1f, 0.95f, 0.85f);
        luzSol.intensity = 2f;
        luzSol.range = 0.9f;
        luzSol.renderMode = LightRenderMode.ForcePixel;
        luzSol.shadows = LightShadows.None;

        if (materialAditivo == null) return;

        // Resplandor: dos "sprites" que siempre miran a la cámara (uno intenso y otro amplio y suave).
        var resplandor = Procedural.CrearParticulas("Resplandor", ancla, MaterialAditivo(new Color(0.5f, 0.5f, 0.5f, 0.5f)), 2, false);
        Procedural.EmitirFija(resplandor, Vector3.zero, diametro * 2.4f, new Color(1f, 0.75f, 0.35f, 0.9f));
        Procedural.EmitirFija(resplandor, Vector3.zero, diametro * 5f, new Color(1f, 0.45f, 0.1f, 0.35f));

        // Corona: chispas que salen de la superficie y se apagan.
        var corona = Procedural.CrearParticulas("Corona", ancla, MaterialAditivo(new Color(0.5f, 0.5f, 0.5f, 0.5f)), 120, true);
        var main = corona.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.003f, 0.008f);
        main.startSize = new ParticleSystem.MinMaxCurve(diametro * 0.2f, diametro * 0.45f);
        main.startColor = new Color(1f, 0.6f, 0.2f, 1f);
        var emision = corona.emission;
        emision.rateOverTime = 35f;
        var forma = corona.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = diametro * 0.48f;
        forma.radiusThickness = 0f;
        var color = corona.colorOverLifetime;
        color.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.2f), new GradientAlphaKey(0f, 1f) });
        color.color = g;
        var tamano = corona.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
    }

    void CrearAtmosfera(Transform cuerpo, Color color, float escala, float intensidad)
    {
        if (materialAtmosfera == null) return;
        var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "Atmosfera";
        QuitarCollider(esfera);
        esfera.transform.SetParent(cuerpo, false);
        esfera.transform.localScale = Vector3.one * escala;
        var mat = new Material(materialAtmosfera);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Intensidad", intensidad);
        esfera.GetComponent<Renderer>().material = mat;
    }

    void CrearNubes(Transform tierra)
    {
        if (materialTransparente == null || texturaNubes == null) return;
        var nubes = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nubes.name = "Nubes";
        QuitarCollider(nubes);
        nubes.transform.SetParent(tierra, false);
        nubes.transform.localScale = Vector3.one * 1.015f;
        nubes.GetComponent<Renderer>().material = new Material(materialTransparente) { mainTexture = texturaNubes, color = Color.white };
        // Las nubes se mueven un poco más rápido que la superficie.
        nubes.AddComponent<PlanetOrbit>().velocidadOrbita = 6f;
    }

    void CrearLuna(Transform anclaTierra, float diametroTierra)
    {
        var datos = PlanetData.Luna();
        float diametro = datos.tamanoRelativo * tamanoBase;

        var pivote = new GameObject("Orbita_Luna").transform;
        pivote.SetParent(anclaTierra, false);
        pivote.localRotation = Quaternion.Euler(datos.inclinacionEje, 0f, 0f);
        // Como la Luna cuelga del pivote, siempre muestra la misma cara a la Tierra (igual que en la realidad).
        var orbita = pivote.gameObject.AddComponent<PlanetOrbit>();
        orbita.velocidadOrbita = 45f;
        orbita.velocidadRotacion = 0f;

        var luna = CrearEsfera("Luna", pivote, diametro, 0f);
        luna.localPosition = new Vector3(diametroTierra * 1.25f, 0f, 0f);
        AsignarMaterial(luna, false, datos.color, texturaLuna);
        AgrandarZonaDeToque(luna, diametro);
        var info = luna.gameObject.AddComponent<PlanetInfo>();
        info.datos = datos;
        CrearHalo(info, luna, diametro * 3f, datos.color);
    }

    // Cometa Halley: núcleo pequeño con un brillo azulado (coma) y una cola de partículas que deja atrás.
    void CrearCometa()
    {
        var datos = PlanetData.Cometa();
        float diametro = datos.tamanoRelativo * tamanoBase;

        var plano = new GameObject("Cometa").transform;
        plano.SetParent(transform, false);
        plano.localRotation = Quaternion.Euler(18f, 40f, 0f); // su órbita está inclinada respecto a la de los planetas
        var ancla = new GameObject("Posicion_Cometa").transform;
        ancla.SetParent(plano, false);

        var nucleo = CrearEsfera(datos.nombre, ancla, diametro, 0f);
        AsignarMaterial(nucleo, false, datos.color, texturaLuna);
        AgrandarZonaDeToque(nucleo, diametro);
        var info = nucleo.gameObject.AddComponent<PlanetInfo>();
        info.datos = datos;
        CrearHalo(info, nucleo, diametro * 7f, datos.color);

        var cometa = plano.gameObject.AddComponent<Cometa>();
        cometa.ancla = ancla;
        cometa.perihelio = RadioOrbita(0.12f);
        cometa.afelio = RadioOrbita(1.55f);
        if (materialAditivo == null) return;

        var coma = Procedural.CrearParticulas("Coma", ancla, MaterialAditivo(new Color(0.5f, 0.5f, 0.5f, 0.5f)), 2, false);
        Procedural.EmitirFija(coma, Vector3.zero, diametro * 5f, new Color(0.6f, 0.85f, 1f, 0.8f));
        Procedural.EmitirFija(coma, Vector3.zero, diametro * 12f, new Color(0.3f, 0.6f, 1f, 0.3f));

        // La cola se simula en el mundo: las partículas quedan atrás mientras el núcleo avanza.
        var cola = Procedural.CrearParticulas("Cola", ancla, MaterialAditivo(new Color(0.5f, 0.5f, 0.5f, 0.5f)), 900, true);
        var main = cola.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.012f, 0.026f);
        main.startSize = new ParticleSystem.MinMaxCurve(diametro * 0.5f, diametro * 1.2f);
        var forma = cola.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 5f;
        forma.radius = diametro * 0.3f;
        var color = cola.colorOverLifetime;
        color.enabled = true;
        color.color = Procedural.Degradado(new Color(0.85f, 0.95f, 1f), new Color(0.3f, 0.55f, 1f), 0.7f, 0f);
        var tamano = cola.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 3f));
        cometa.cola = cola;
    }

    void CrearAnillo(Transform saturno, Color color)
    {
        var anillo = new GameObject("Anillo");
        anillo.transform.SetParent(saturno, false);
        // Radios reales del anillo: de 1,24 a 2,27 veces el radio de Saturno (la esfera mide 1 de diámetro).
        anillo.AddComponent<MeshFilter>().sharedMesh = Procedural.MallaAnillo(0.62f, 1.14f);
        var render = anillo.AddComponent<MeshRenderer>();
        var baseMat = materialTransparente != null ? materialTransparente : materialPlaneta;
        if (baseMat == null) return;
        render.material = new Material(baseMat)
        {
            mainTexture = texturaAnillo != null ? texturaAnillo : Procedural.TexturaAnilloRespaldo(color),
            color = Color.white,
        };
    }

    Transform CrearLineaOrbita(float radio)
    {
        if (materialOrbita == null) return null;
        const int segmentos = 128;
        var go = new GameObject("LineaOrbita");
        go.transform.SetParent(transform, false);
        var linea = go.AddComponent<LineRenderer>();
        linea.useWorldSpace = false;
        linea.loop = true;
        linea.positionCount = segmentos;
        linea.widthMultiplier = 0.0012f;
        linea.material = materialOrbita;
        linea.startColor = linea.endColor = new Color(0.7f, 0.8f, 1f, 0.22f);
        for (int i = 0; i < segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
            linea.SetPosition(i, new Vector3(Mathf.Cos(a) * radio, 0f, Mathf.Sin(a) * radio));
        }
        var ancho = go.AddComponent<AnchoSegunDistancia>();
        ancho.radioOrbita = radio;
        ancho.anchoPorMetro = 0.003f;
        ancho.anchoMaximo = 0.0012f;
        return go.transform;
    }

    // Estela que sigue al planeta: un arco de su órbita que se desvanece. Cuelga del pivote, así que gira con él
    // (no deja manchas si se mueve el cubo, como pasaría con un TrailRenderer).
    Transform CrearEstela(Transform pivote, Transform ancla, float radio, float diametro, Color color)
    {
        if (materialOrbita == null) return null;
        const int puntos = 32;
        const float arco = 55f * Mathf.Deg2Rad;
        var go = new GameObject("Estela");
        go.transform.SetParent(pivote, false);
        var linea = go.AddComponent<LineRenderer>();
        linea.useWorldSpace = false;
        linea.positionCount = puntos;
        linea.material = materialOrbita;
        linea.widthMultiplier = Mathf.Clamp(diametro * 0.22f, 0.0008f, 0.0025f);
        linea.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f);
        linea.colorGradient = Procedural.Degradado(Color.Lerp(color, Color.white, 0.3f), color, 0.75f, 0f);
        for (int i = 0; i < puntos; i++)
        {
            float a = arco * i / (puntos - 1);
            linea.SetPosition(i, new Vector3(Mathf.Cos(a) * radio, 0f, Mathf.Sin(a) * radio));
        }
        // El ancho de arriba es el que se ve desde la vista general (a unos 0,4 m).
        var ancho = go.AddComponent<AnchoSegunDistancia>();
        ancho.referencia = ancla;
        ancho.anchoMaximo = linea.widthMultiplier;
        ancho.anchoPorMetro = linea.widthMultiplier / 0.4f;
        return go.transform;
    }

    void CrearCinturonAsteroides()
    {
        if (materialAditivo == null) return;
        var pivote = new GameObject("CinturonAsteroides").transform;
        pivote.SetParent(transform, false);
        pivote.gameObject.AddComponent<PlanetOrbit>().velocidadOrbita = 5f;

        const int cantidad = 700;
        float rMin = RadioOrbita(0.575f), rMax = RadioOrbita(0.64f);
        var ps = Procedural.CrearParticulas("Asteroides", pivote, MaterialAditivo(new Color(0.5f, 0.5f, 0.5f, 0.5f)), cantidad, false);
        for (int i = 0; i < cantidad; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Lerp(rMin, rMax, (Random.value + Random.value) * 0.5f);
            var pos = new Vector3(Mathf.Cos(a) * r, Random.Range(-0.003f, 0.003f), Mathf.Sin(a) * r);
            float gris = Random.Range(0.45f, 0.8f);
            Procedural.EmitirFija(ps, pos, Random.Range(0.0012f, 0.003f), new Color(gris, gris * 0.92f, gris * 0.85f, 0.9f));
        }
        aparicion.Agregar(null, pivote);
    }

    // Estrellas que titilan en una esfera alrededor de todo el sistema (más allá de donde llega la cámara),
    // por delante del cielo estrellado de fondo.
    void CrearEstrellas()
    {
        if (materialAditivo == null) return;
        var ps = Procedural.CrearParticulas("Estrellas", transform, MaterialAditivo(new Color(0.5f, 0.5f, 0.5f, 0.5f)), 900, true);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.7f, 0.8f, 1f));
        var emision = ps.emission;
        emision.rateOverTime = 170f;
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 4f;
        forma.radiusThickness = 0.35f;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
        color.color = g;
    }
}

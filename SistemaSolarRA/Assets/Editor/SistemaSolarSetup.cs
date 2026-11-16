using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú "Sistema Solar" en la barra de Unity: arma la escena de realidad virtual y compila para PC y Android.
// Desde la terminal: Unity.exe -batchmode -quit -projectPath SistemaSolarRA -executeMethod SistemaSolarSetup.CompilarPC
//                    (o SistemaSolarSetup.CompilarAPK)
public static class SistemaSolarSetup
{
    const string RutaEscena = "Assets/Scenes/SistemaSolar.unity";
    const string RutaMateriales = "Assets/Materials";
    const string RutaTexturas = "Assets/Texturas";
    const string RutaExe = "Builds/PC/SistemaSolarVR.exe";
    const string RutaApk = "Builds/Android/SistemaSolarVR.apk";

    // Palabras para reconocer texturas en Assets/Texturas (sirven los nombres de Solar System Scope, p. ej. "2k_mars").
    static readonly string[][] ClavesTexturas =
    {
        new[] { "sun", "sol" }, new[] { "mercury", "mercurio" }, new[] { "venus" },
        new[] { "earth", "tierra" }, new[] { "mars", "marte" }, new[] { "jupiter" },
        new[] { "saturn", "saturno" }, new[] { "uranus", "urano" }, new[] { "neptune", "neptuno" },
    };
    static readonly string[] TexturasExcluidas =
        { "ring", "anillo", "cloud", "nubes", "night", "normal", "specular", "moon", "luna", "stars", "estrellas" };

    // Escena: cámara que flota en el espacio (CamaraExplorador) + sistema solar + interfaz.
    [MenuItem("Sistema Solar/1. Crear escena", priority = 1)]
    public static void CrearEscena()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var camara = Camera.main;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
        var cielo = BuscarTextura("estrellas", "stars");
        if (cielo != null)
        {
            RenderSettings.skybox = CrearMaterial("Mat_Cielo", "Skybox/Panoramic", m =>
            {
                m.SetTexture("_MainTex", cielo);
                m.SetFloat("_Exposure", 0.55f);
            });
            camara.clearFlags = CameraClearFlags.Skybox;
        }
        // Planos de recorte cercanos para poder acercarse mucho a los planetas pequeños.
        camara.nearClipPlane = 0.002f;
        camara.farClipPlane = 20f;
        camara.fieldOfView = 60f;
        // Punto de partida: un poco por encima del plano de las órbitas, mirando al Sol desde fuera de Neptuno.
        camara.transform.position = new Vector3(0f, 0.11f, -0.42f);
        camara.transform.LookAt(Vector3.zero);
        camara.gameObject.AddComponent<CamaraExplorador>();

        // La única luz es la del Sol (la crea SolarSystemBuilder).
        var luz = Object.FindObjectOfType<Light>();
        if (luz != null) Object.DestroyImmediate(luz.gameObject);

        int texturas = CrearSistemaSolar();
        new GameObject("GestorApp").AddComponent<AppUIBuilder>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(escena, RutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };

        Debug.Log($"Escena creada en {RutaEscena}. Texturas asignadas: {texturas} de 9.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Escena lista",
                $"Escena guardada en {RutaEscena}. Presionen Play para probarla.\n\n" +
                "WASD = moverse, Q/E = bajar/subir, arrastrar = mirar, clic en un planeta = ficha.\n" +
                $"Texturas asignadas: {texturas} de 9.\n\n" +
                "Siguiente paso: menú Sistema Solar > 2. Compilar para PC o 3. Compilar APK.", "OK");
    }

    [MenuItem("Sistema Solar/2. Compilar para PC (.exe)", priority = 2)]
    public static void CompilarPC()
    {
        if (!AsegurarEscena()) return;
        ConfigurarComun();
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        // Que no se congele si se hace clic en otra ventana (por ejemplo, en la exposición).
        PlayerSettings.runInBackground = true;
        var reporte = BuildPipeline.BuildPlayer(new[] { RutaEscena }, RutaExe,
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        Terminar(reporte, RutaExe, "Abran SistemaSolarVR.exe. Para pantalla completa: Alt + Enter.");
    }

    [MenuItem("Sistema Solar/3. Compilar APK (Android)", priority = 3)]
    public static void CompilarAPK()
    {
        if (!AsegurarEscena()) return;
        ConfigurarComun();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "pe.edu.unt.sistemasolarvr");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        // Horizontal: es como se mira mejor alrededor girando el teléfono.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle = false;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        System.IO.Directory.CreateDirectory("Builds/Android");
        var reporte = BuildPipeline.BuildPlayer(new[] { RutaEscena }, RutaApk, BuildTarget.Android, BuildOptions.None);
        Terminar(reporte, RutaApk, "Pásenlo al celular (cable, Drive o WhatsApp) e instálenlo.");
    }

    static void ConfigurarComun()
    {
        PlayerSettings.companyName = "UNT";
        PlayerSettings.productName = "SistemaSolarVR";
    }

    // Si la escena no existe, la crea (así los menús 2 y 3 funcionan aunque se salte el 1).
    static bool AsegurarEscena()
    {
        if (!System.IO.File.Exists(RutaEscena)) CrearEscena();
        return System.IO.File.Exists(RutaEscena);
    }

    static void Terminar(UnityEditor.Build.Reporting.BuildReport reporte, string ruta, string siguiente)
    {
        bool ok = reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        Debug.Log("RESULTADO_BUILD: " + reporte.summary.result + " -> " + ruta);
        if (Application.isBatchMode)
        {
            if (!ok) EditorApplication.Exit(1);
            return;
        }
        if (ok) EditorUtility.RevealInFinder(ruta);
        EditorUtility.DisplayDialog(ok ? "Compilación lista" : "Error al compilar",
            ok ? $"Se creó {ruta}.\n\n{siguiente}" : "Revisen la consola (Window > General > Console).", "OK");
    }

    static int CrearSistemaSolar()
    {
        var sistema = new GameObject("SistemaSolar");
        var builder = sistema.AddComponent<SolarSystemBuilder>();
        builder.materialPlaneta = CrearMaterial("Mat_Planeta", "Standard", m =>
        {
            m.SetFloat("_Glossiness", 0.15f);
            m.SetFloat("_Metallic", 0f);
        });
        builder.materialSol = CrearMaterial("Mat_Sol", "Unlit/Texture", null);
        builder.materialOrbita = CrearMaterial("Mat_Orbita", "Sprites/Default", null);
        builder.materialTransparente = CrearMaterial("Mat_Transparente", "Legacy Shaders/Transparent/Diffuse", null);
        builder.materialAditivo = CrearMaterial("Mat_Brillo", "Legacy Shaders/Particles/Additive", null);
        builder.materialAtmosfera = CrearMaterial("Mat_Atmosfera", "SistemaSolar/Atmosfera", null);

        builder.texturaAnillo = BuscarTextura("anillo", "ring");
        builder.texturaLuna = BuscarTextura("luna", "moon");
        builder.texturaNubes = BuscarTextura("nubes", "cloud");
        if (builder.texturaNubes != null) AlfaDesdeGrises(builder.texturaNubes);
        return AsignarTexturas(builder);
    }

    static Texture2D BuscarTextura(params string[] claves)
    {
        if (!AssetDatabase.IsValidFolder(RutaTexturas)) return null;
        var ruta = AssetDatabase.FindAssets("t:Texture2D", new[] { RutaTexturas })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(r => claves.Any(k => System.IO.Path.GetFileNameWithoutExtension(r).ToLower().Contains(k)));
        return ruta == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    // Las nubes vienen en blanco sobre negro: el gris se usa como transparencia.
    static void AlfaDesdeGrises(Texture2D textura)
    {
        var importador = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(textura)) as TextureImporter;
        if (importador == null || importador.alphaSource == TextureImporterAlphaSource.FromGrayScale) return;
        importador.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        importador.alphaIsTransparency = true;
        importador.SaveAndReimport();
    }

    static Material CrearMaterial(string nombre, string shader, System.Action<Material> ajustar)
    {
        System.IO.Directory.CreateDirectory(RutaMateriales);
        string ruta = $"{RutaMateriales}/{nombre}.mat";
        var sh = Shader.Find(shader);
        if (sh == null)
        {
            Debug.LogWarning($"Shader '{shader}' no encontrado; se usa Standard para {nombre}.");
            sh = Shader.Find("Standard");
        }
        // Si el material ya existe se actualiza (así los cambios llegan también a los materiales viejos).
        var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null)
        {
            existente.shader = sh;
            ajustar?.Invoke(existente);
            EditorUtility.SetDirty(existente);
            AssetDatabase.SaveAssets();
            return existente;
        }
        var mat = new Material(sh);
        ajustar?.Invoke(mat);
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    static int AsignarTexturas(SolarSystemBuilder builder)
    {
        if (!AssetDatabase.IsValidFolder(RutaTexturas)) return 0;
        var rutas = AssetDatabase.FindAssets("t:Texture2D", new[] { RutaTexturas })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(r => !TexturasExcluidas.Any(e => r.ToLower().Contains(e)))
            .ToArray();

        builder.texturas = new Texture2D[ClavesTexturas.Length];
        int asignadas = 0;
        for (int i = 0; i < ClavesTexturas.Length; i++)
        {
            string ruta = rutas.FirstOrDefault(r =>
                ClavesTexturas[i].Any(k => System.IO.Path.GetFileNameWithoutExtension(r).ToLower().Contains(k)));
            if (ruta == null) continue;
            builder.texturas[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            asignadas++;
        }
        return asignadas;
    }
}

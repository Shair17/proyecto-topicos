using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú "Sistema Solar" en la barra de Unity: arma la escena completa y configura Android.
public static class SistemaSolarSetup
{
    const string RutaEscena = "Assets/Scenes/SistemaSolar.unity";
    const string RutaMateriales = "Assets/Materials";
    const string RutaTexturas = "Assets/Texturas";

    // Palabras para reconocer texturas en Assets/Texturas (sirven los nombres de Solar System Scope, p. ej. "2k_mars").
    static readonly string[][] ClavesTexturas =
    {
        new[] { "sun", "sol" }, new[] { "mercury", "mercurio" }, new[] { "venus" },
        new[] { "earth", "tierra" }, new[] { "mars", "marte" }, new[] { "jupiter" },
        new[] { "saturn", "saturno" }, new[] { "uranus", "urano" }, new[] { "neptune", "neptuno" },
    };
    static readonly string[] TexturasExcluidas =
        { "ring", "anillo", "cloud", "nubes", "night", "normal", "specular", "moon", "luna", "stars", "estrellas" };

    [MenuItem("Sistema Solar/1. Crear escena", priority = 1)]
    public static void CrearEscena()
    {
        if (!VuforiaImportado())
        {
            EditorUtility.DisplayDialog("Falta Vuforia",
                "Primero importen el paquete de Vuforia Engine (add-vuforia-package-*.unitypackage) " +
                "y la base de datos del cubo. Luego vuelvan a usar este menú.", "Entendido");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        bool licenciaOk = CargarLicenciaVuforia();

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var camaraVieja = GameObject.Find("Main Camera");
        if (camaraVieja != null) Object.DestroyImmediate(camaraVieja);

        // Cámara AR y cubo (se crean con los mismos menús de Vuforia que se usarían a mano)
        var arCamera = CrearDesdeMenu("GameObject/Vuforia Engine/AR Camera");
        if (arCamera != null) arCamera.tag = "MainCamera";
        var multiTarget = CrearDesdeMenu("GameObject/Vuforia Engine/Multi Target");
        if (arCamera == null || multiTarget == null)
        {
            EditorUtility.DisplayDialog("No se pudo crear",
                "No se encontraron los menús de Vuforia (AR Camera / Multi Target). Revisen que Vuforia esté bien importado.", "OK");
            return;
        }
        bool filtroOk = FiltrarSoloTracked(multiTarget);

        // Sistema solar dentro del cubo
        int texturas = CrearSistemaSolar(multiTarget.transform);

        // Interfaz
        new GameObject("GestorApp").AddComponent<AppUIBuilder>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(escena, RutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
        Selection.activeGameObject = multiTarget;

        EditorUtility.DisplayDialog("Escena creada",
            "Escena guardada en " + RutaEscena + ".\n\n" +
            "Revisen en el Inspector del MultiTarget que esté elegida la base de datos del cubo.\n" +
            $"Texturas de planetas asignadas: {texturas} de 9 (opcionales, van en {RutaTexturas}).\n" +
            (filtroOk ? "" : "\nNo se pudo poner el filtro en 'Tracked': cámbienlo a mano en Default Observer Event Handler.\n") +
            (licenciaOk ? "" : "\nNo se cargó la licencia: péguenla a mano en Window > Vuforia Configuration.\n") +
            "\nSiguiente paso: menú Sistema Solar > 2. Configurar Android.", "OK");
    }

    const string RutaEscenaPC = "Assets/Scenes/VistaPreviaPC.unity";
    const string RutaExePC = "Builds/VistaPreviaPC/SistemaSolar.exe";

    // Escena sin Vuforia: cámara normal que gira alrededor del Sol. Sirve para probar en la PC.
    [MenuItem("Sistema Solar/0. Vista previa en PC (sin RA)", priority = 0)]
    public static void CrearVistaPreviaPC()
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
        camara.nearClipPlane = 0.01f;
        camara.farClipPlane = 20f;
        camara.gameObject.AddComponent<CamaraVistaPrevia>();

        // La única luz es la del Sol (la crea SolarSystemBuilder).
        var luz = Object.FindObjectOfType<Light>();
        if (luz != null) Object.DestroyImmediate(luz.gameObject);

        CrearSistemaSolar(null);
        new GameObject("GestorApp").AddComponent<AppUIBuilder>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(escena, RutaEscenaPC);
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Vista previa lista",
                "Escena guardada en " + RutaEscenaPC + ". Presionen Play.\n\n" +
                "Clic izquierdo en un planeta = ver su ficha. Clic derecho + arrastrar = girar. Rueda = zoom.", "OK");
    }

    // Para la terminal: Unity.exe -batchmode -quit -projectPath ... -executeMethod SistemaSolarSetup.CompilarVistaPreviaPC
    public static void CompilarVistaPreviaPC()
    {
        CrearVistaPreviaPC();
        PlayerSettings.productName = "SistemaSolarRA";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.resizableWindow = true;
        var reporte = BuildPipeline.BuildPlayer(new[] { RutaEscenaPC }, RutaExePC,
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("RESULTADO_BUILD: " + reporte.summary.result + " -> " + RutaExePC);
        if (Application.isBatchMode && reporte.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    static int CrearSistemaSolar(Transform padre)
    {
        var sistema = new GameObject("SistemaSolar");
        sistema.transform.SetParent(padre, false);
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

    [MenuItem("Sistema Solar/2. Configurar Android", priority = 2)]
    public static void ConfigurarAndroid()
    {
        ConfigurarAndroidSinDialogo();
        EditorUtility.DisplayDialog("Android listo",
            "Proyecto configurado para Android (IL2CPP, ARM64, vertical).\n\n" +
            "Siguiente paso: menú Sistema Solar > 3. Compilar APK (o File > Build And Run con el celular conectado).", "OK");
    }

    static void ConfigurarAndroidSinDialogo()
    {
        PlayerSettings.companyName = "UNT";
        PlayerSettings.productName = "SistemaSolarRA";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "pe.edu.unt.sistemasolarra");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    const string RutaApk = "Builds/Android/SistemaSolarRA.apk";

    // Compila el APK una sola vez; después solo se instala en el celular y se abre como cualquier app.
    [MenuItem("Sistema Solar/3. Compilar APK", priority = 3)]
    public static void CompilarAPK()
    {
        if (!System.IO.File.Exists(RutaEscena))
        {
            Debug.LogError("Falta la escena " + RutaEscena + ": usen primero el menú 1. Crear escena.");
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Falta la escena", "Usen primero el menú Sistema Solar > 1. Crear escena.", "OK");
            return;
        }
        ConfigurarAndroidSinDialogo();
        EditorUserBuildSettings.buildAppBundle = false;
        System.IO.Directory.CreateDirectory("Builds/Android");
        var reporte = BuildPipeline.BuildPlayer(new[] { RutaEscena }, RutaApk, BuildTarget.Android, BuildOptions.None);
        bool ok = reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        Debug.Log("RESULTADO_BUILD: " + reporte.summary.result + " -> " + RutaApk);
        if (Application.isBatchMode)
        {
            if (!ok) EditorApplication.Exit(1);
            return;
        }
        if (ok) EditorUtility.RevealInFinder(RutaApk);
        EditorUtility.DisplayDialog(ok ? "APK listo" : "Error al compilar",
            ok ? "Se creó " + RutaApk + ".\n\nPásenlo al celular (cable, Drive o WhatsApp) e instálenlo. " +
                 "Ya no hace falta compilar de nuevo para mostrarlo."
               : "Revisen la consola (Window > General > Console).", "OK");
    }

    const string RutaApkVistaPrevia = "Builds/Android/SistemaSolar_VistaPrevia.apk";

    // APK sin RA (no necesita Vuforia): el sistema solar en 3D con controles táctiles. Sirve para mostrarlo ya.
    // Usa otro identificador para poder tener instaladas las dos apps a la vez.
    public static void CompilarVistaPreviaAndroid()
    {
        CrearVistaPreviaPC();
        string idOriginal = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        string nombreOriginal = PlayerSettings.productName;
        ConfigurarAndroidSinDialogo();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "pe.edu.unt.sistemasolarra.vistaprevia");
        PlayerSettings.productName = "Sistema Solar 3D";
        EditorUserBuildSettings.buildAppBundle = false;
        System.IO.Directory.CreateDirectory("Builds/Android");
        var reporte = BuildPipeline.BuildPlayer(new[] { RutaEscenaPC }, RutaApkVistaPrevia, BuildTarget.Android, BuildOptions.None);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, idOriginal);
        PlayerSettings.productName = nombreOriginal;
        Debug.Log("RESULTADO_BUILD: " + reporte.summary.result + " -> " + RutaApkVistaPrevia);
        if (Application.isBatchMode && reporte.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
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

    static bool VuforiaImportado()
    {
        return System.AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetType("Vuforia.VuforiaBehaviour") != null);
    }

    // Lee la clave de licencia de "licencia-vuforia.txt" (junto a la carpeta SistemaSolarRA, fuera de git)
    // y la pone en VuforiaConfiguration. Usa reflexión para no depender de la API de Vuforia al compilar.
    // También se puede usar con -executeMethod SistemaSolarSetup.CargarLicenciaVuforia
    public static bool CargarLicenciaVuforia()
    {
        string ruta = System.IO.Path.GetFullPath(System.IO.Path.Combine("..", "licencia-vuforia.txt"));
        if (!System.IO.File.Exists(ruta)) { Debug.LogWarning("No existe " + ruta); return false; }
        string clave = System.IO.File.ReadAllText(ruta).Trim();
        if (clave.Length == 0) return false;

        var tipo = System.AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("Vuforia.VuforiaConfiguration")).FirstOrDefault(t => t != null);
        var config = tipo?.GetProperty("Instance")?.GetValue(null) as Object;
        var generica = tipo?.GetProperty("Vuforia")?.GetValue(config);
        var propClave = generica?.GetType().GetProperty("LicenseKey");
        if (config == null || propClave == null) { Debug.LogWarning("No se encontró VuforiaConfiguration"); return false; }

        propClave.SetValue(generica, clave);
        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(config)))
        {
            // Vuforia todavía no guardó su configuración (pasa si nunca se abrió en el editor)
            System.IO.Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.CreateAsset(config, "Assets/Resources/VuforiaConfiguration.asset");
        }
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("Licencia de Vuforia cargada en " + AssetDatabase.GetAssetPath(config));
        return true;
    }

    static GameObject CrearDesdeMenu(string menu)
    {
        Selection.activeGameObject = null;
        if (!EditorApplication.ExecuteMenuItem(menu)) return null;
        var creado = Selection.activeGameObject;
        if (creado != null && creado.transform.parent != null) creado.transform.SetParent(null, true);
        return creado;
    }

    // Pone "Consider target as visible if its status is: Tracked" en Default Observer Event Handler.
    static bool FiltrarSoloTracked(GameObject multiTarget)
    {
        foreach (var comp in multiTarget.GetComponents<MonoBehaviour>())
        {
            if (comp == null || comp.GetType().Name != "DefaultObserverEventHandler") continue;
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.propertyType != SerializedPropertyType.Enum || !prop.name.ToLower().Contains("statusfilter")) continue;
                int indice = System.Array.IndexOf(prop.enumNames, "Tracked");
                if (indice < 0) return false;
                prop.enumValueIndex = indice;
                so.ApplyModifiedProperties();
                return true;
            }
        }
        return false;
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

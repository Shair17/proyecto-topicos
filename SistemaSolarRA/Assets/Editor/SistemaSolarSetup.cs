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
    static readonly string[] TexturasExcluidas = { "ring", "anillo", "cloud", "night", "normal", "specular", "atmosphere" };

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
        var sistema = new GameObject("SistemaSolar");
        sistema.transform.SetParent(multiTarget.transform, false);
        var builder = sistema.AddComponent<SolarSystemBuilder>();
        builder.materialPlaneta = CrearMaterial("Mat_Planeta", "Standard", m => m.SetFloat("_Glossiness", 0.2f));
        builder.materialSol = CrearMaterial("Mat_Sol", "Standard", m =>
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.1f) * 0.7f);
        });
        builder.materialOrbita = CrearMaterial("Mat_Orbita", "Sprites/Default", null);
        int texturas = AsignarTexturas(builder);

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
            "\nSiguiente paso: menú Sistema Solar > 2. Configurar Android.", "OK");
    }

    [MenuItem("Sistema Solar/2. Configurar Android", priority = 2)]
    public static void ConfigurarAndroid()
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

        EditorUtility.DisplayDialog("Android listo",
            "Proyecto configurado para Android (IL2CPP, ARM64, vertical).\n\n" +
            "Para instalar en el celular: activen Depuración USB, conéctenlo y usen File > Build And Run.", "OK");
    }

    static bool VuforiaImportado()
    {
        return System.AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetType("Vuforia.VuforiaBehaviour") != null);
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
        var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;

        var sh = Shader.Find(shader);
        if (sh == null)
        {
            Debug.LogWarning($"Shader '{shader}' no encontrado; se usa Standard para {nombre}.");
            sh = Shader.Find("Standard");
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

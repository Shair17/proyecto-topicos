using System.Collections;
using System.IO;
using UnityEngine;

// Solo para revisar cómo se ve la app sin tocarla: si se abre con "-capturas <carpeta>",
// guarda varias capturas (la animación, el sistema completo y viajando a la Tierra, Saturno y el Sol) y se cierra.
// Sin ese argumento no hace nada (no afecta al APK).
public class CapturaAutomatica : MonoBehaviour
{
    string carpeta;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Revisar()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-capturas")
            {
                var go = new GameObject("CapturaAutomatica");
                go.AddComponent<CapturaAutomatica>().carpeta = args[i + 1];
            }
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(carpeta);
        yield return Capturar(0.9f, "1_aparicion");
        yield return Capturar(3.5f, "2_sistema");

        var selector = FindObjectOfType<PlanetSelector>();
        var camara = CamaraExplorador.Instancia;
        foreach (var nombre in new[] { "Tierra", "Saturno", "Sol" })
            foreach (var p in FindObjectsOfType<PlanetInfo>())
            {
                if (p.datos.nombre != nombre) continue;
                if (selector != null) selector.Seleccionar(p);
                if (camara != null) camara.Viajar(p.transform);
                yield return Capturar(camara != null ? camara.duracionViaje + 0.6f : 1f, "3_" + nombre);
            }

        // De vuelta a la vista general y avanzando hacia el Sol, como lo haría alguien con el teclado.
        if (selector != null) selector.Deseleccionar();
        if (camara != null) camara.VistaGeneral();
        yield return Capturar(camara != null ? camara.duracionViaje + 0.3f : 1f, "4_vista_general");
        Application.Quit();
    }

    IEnumerator Capturar(float espera, string nombre)
    {
        yield return new WaitForSeconds(espera);
        yield return new WaitForEndOfFrame();
        int ancho = 1280, alto = 720;
        RenderTexture rt = null;
        var cam = Camera.main;
        if (Application.isBatchMode && cam != null)
        {
            // Sin ventana Unity no dibuja la pantalla: se renderiza la cámara (y la interfaz) a una textura.
            foreach (var c in FindObjectsOfType<Canvas>())
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane * 2f;
            }
            rt = new RenderTexture(ancho, alto, 24);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
        }
        else { ancho = Screen.width; alto = Screen.height; }
        var tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        if (rt != null) rt.Release();
        File.WriteAllBytes(Path.Combine(carpeta, nombre + ".png"), tex.EncodeToPNG());
        Destroy(tex);
        yield return null;
    }
}

using System.Collections;
using System.IO;
using UnityEngine;

// Solo para revisar cómo se ve la app sin tocarla: si se abre con "-capturas <carpeta>",
// guarda varias capturas (animación, sistema, viajes, quiz y modo visor) y se cierra.
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
        float viaje = camara != null ? camara.duracionViaje : 1f;
        foreach (var nombre in new[] { "Tierra", "Saturno", "Sol", "Cometa Halley" })
            foreach (var p in FindObjectsOfType<PlanetInfo>())
            {
                if (p.datos.nombre != nombre) continue;
                if (selector != null) selector.Seleccionar(p);
                if (camara != null) camara.Viajar(p.transform);
                yield return Capturar(viaje + 0.6f, "3_" + nombre.Replace(' ', '_'));
            }
        if (selector != null) selector.Deseleccionar();

        // Quiz: se responde la primera opción y se captura el viaje a la respuesta.
        var quiz = FindObjectOfType<QuizPlanetas>();
        if (quiz != null)
        {
            quiz.Iniciar();
            yield return Capturar(0.5f, "4_quiz_pregunta");
            quiz.Responder(0);
            yield return Capturar(viaje + 0.6f, "5_quiz_respuesta");
            quiz.Cerrar();
        }

        // Modo visor (pantalla dividida para Cardboard) mirando hacia el Sol desde la vista general.
        if (camara != null) camara.VistaGeneral();
        yield return new WaitForSeconds(viaje + 0.3f);
        var visor = FindObjectOfType<ModoVisor>();
        if (visor != null)
        {
            visor.Entrar();
            yield return Capturar(1f, "6_visor");
            visor.Salir();
        }
        yield return Capturar(0.5f, "7_vista_general");

        // Voz: se enciende el micrófono (para comprobar que arranca) y se simulan frases como si se hubieran dicho.
        var voz = FindObjectOfType<ReconocimientoVoz>();
        if (voz != null)
        {
            voz.Iniciar();
            Debug.Log($"CAPTURA voz disponible={voz.Disponible} activa={voz.Activo}");
        }
        var comandos = FindObjectOfType<ComandosVoz>();
        if (comandos != null)
        {
            comandos.Interpretar("vamos a Saturno");
            yield return Capturar(viaje + 0.6f, "8_voz_saturno");
            comandos.Interpretar("¿Qué puedo decir?");
            comandos.Interpretar("quiz");
            yield return Capturar(1f, "9_voz_quiz");
        }
        if (voz != null) voz.Detener();
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

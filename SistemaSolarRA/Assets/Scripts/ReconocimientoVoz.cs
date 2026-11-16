using System.Collections.Generic;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

// Escucha el micrófono y entrega lo que se dijo como texto (ComandosVoz decide qué hacer).
//   Windows: KeywordRecognizer de Unity (reconocimiento de Windows, sin internet). Solo reconoce las frases
//            de la lista 'frasesWindows', en el idioma de voz de Windows (Configuración > Hora e idioma > Voz).
//   Android: SpeechRecognizer del teléfono (Google), en español de Perú; entiende frases libres. Necesita
//            permiso de micrófono y normalmente internet. Se reinicia solo después de cada frase.
// No escucha mientras el Narrador está hablando, para no reaccionar a su propia voz.
public class ReconocimientoVoz : MonoBehaviour
{
    public string[] frasesWindows;                 // las llena ComandosVoz
    public System.Action<string> alReconocer;      // se llama en el hilo principal de Unity
    public System.Action<string> alCambiarEstado;  // "Escuchando", "Sin micrófono", etc.

    public bool Activo { get; private set; }

    readonly Queue<string> pendientes = new Queue<string>();
    readonly object candado = new object();
    float ignorarHasta;

    public bool Disponible
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return PhraseRecognitionSystem.isSupported;
#else
            return false;
#endif
        }
    }

    public bool Alternar()
    {
        if (Activo) Detener();
        else Iniciar();
        return Activo;
    }

    // Llamado desde otros hilos (Android) o desde el reconocedor (Windows).
    void Recibir(string texto)
    {
        lock (candado) pendientes.Enqueue(texto);
    }

    void Estado(string texto)
    {
        lock (candado) pendientes.Enqueue("\u0001" + texto); // marca: es un estado, no una frase
    }

    void Update()
    {
        // El narrador habla: ignorar lo que se oiga hasta un momento después de que termine.
        bool hablando = Narrador.Instancia != null && Narrador.Instancia.Hablando;
        if (hablando) ignorarHasta = Time.unscaledTime + 0.6f;

        while (true)
        {
            string texto;
            lock (candado)
            {
                if (pendientes.Count == 0) break;
                texto = pendientes.Dequeue();
            }
            if (texto.Length > 0 && texto[0] == '\u0001') { alCambiarEstado?.Invoke(texto.Substring(1)); continue; }
            if (Time.unscaledTime < ignorarHasta) continue;
            alReconocer?.Invoke(texto);
        }
        ActualizarPlataforma(hablando);
    }

    void OnDestroy() => Detener();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    // ---------- Windows ----------
    KeywordRecognizer reconocedor;

    public void Iniciar()
    {
        if (Activo || !Disponible || frasesWindows == null || frasesWindows.Length == 0) { Estado("Voz no disponible en esta PC"); return; }
        try
        {
            if (reconocedor == null)
            {
                reconocedor = new KeywordRecognizer(frasesWindows, ConfidenceLevel.Low);
                reconocedor.OnPhraseRecognized += r => Recibir(r.text);
            }
            reconocedor.Start();
            Activo = true;
            Estado("Escuchando…");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Reconocimiento de voz: " + e.Message);
            Estado("No se pudo usar el micrófono");
        }
    }

    public void Detener()
    {
        Activo = false;
        if (reconocedor != null && reconocedor.IsRunning) reconocedor.Stop();
    }

    void OnApplicationQuit()
    {
        reconocedor?.Dispose();
        reconocedor = null;
    }

    void ActualizarPlataforma(bool hablando) { }

#elif UNITY_ANDROID && !UNITY_EDITOR
    // ---------- Android ----------
    AndroidJavaObject actividad, reconocedor;
    bool escuchandoAndroid;       // hay una sesión de reconocimiento abierta
    float reiniciarEn = -1f;
    bool esperandoPermiso;

    class Escucha : AndroidJavaProxy
    {
        readonly ReconocimientoVoz voz;
        public Escucha(ReconocimientoVoz v) : base("android.speech.RecognitionListener") { voz = v; }
        void onReadyForSpeech(AndroidJavaObject parametros) => voz.Estado("Escuchando…");
        void onBeginningOfSpeech() { }
        void onRmsChanged(float volumen) { }
        void onBufferReceived(AndroidJavaObject datos) { }
        void onEndOfSpeech() { }
        void onPartialResults(AndroidJavaObject resultados) { }
        void onEvent(int tipo, AndroidJavaObject parametros) { }
        void onError(int codigo) => voz.ErrorAndroid(codigo);
        void onResults(AndroidJavaObject resultados) => voz.ResultadosAndroid(resultados);
    }

    public void Iniciar()
    {
        if (Activo) return;
        Activo = true;
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            _ = Microphone.devices; // referencia a Microphone: Unity agrega el permiso al manifest
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            esperandoPermiso = true;
            Estado("Esperando permiso de micrófono…");
            return;
        }
        Arrancar();
    }

    void Arrancar()
    {
        actividad ??= new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
        using (var clase = new AndroidJavaClass("android.speech.SpeechRecognizer"))
            if (!clase.CallStatic<bool>("isRecognitionAvailable", actividad))
            {
                Activo = false;
                Estado("Este celular no tiene reconocimiento de voz");
                return;
            }
        EnHiloAndroid(() =>
        {
            if (reconocedor == null)
            {
                using var clase = new AndroidJavaClass("android.speech.SpeechRecognizer");
                reconocedor = clase.CallStatic<AndroidJavaObject>("createSpeechRecognizer", actividad);
                reconocedor.Call("setRecognitionListener", new Escucha(this));
            }
            EscucharUnaVez();
        });
    }

    // El reconocedor de Android solo se puede usar desde el hilo principal de Android (no el de Unity).
    void EnHiloAndroid(System.Action accion) => actividad.Call("runOnUiThread", new AndroidJavaRunnable(() =>
    {
        try { accion(); } catch (System.Exception e) { Debug.LogWarning("Voz: " + e.Message); }
    }));

    void EscucharUnaVez()
    {
        using var intent = new AndroidJavaObject("android.content.Intent", "android.speech.action.RECOGNIZE_SPEECH");
        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form").Dispose();
        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE", "es-PE").Dispose();
        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.MAX_RESULTS", 3).Dispose();
        reconocedor.Call("startListening", intent);
        escuchandoAndroid = true;
    }

    void ResultadosAndroid(AndroidJavaObject resultados)
    {
        escuchandoAndroid = false;
        using (var lista = resultados.Call<AndroidJavaObject>("getStringArrayList", "results_recognition"))
            if (lista != null)
            {
                // Se manda solo la opción más probable (la primera).
                int n = lista.Call<int>("size");
                if (n > 0)
                    using (var primero = lista.Call<AndroidJavaObject>("get", 0))
                        Recibir(primero.Call<string>("toString"));
            }
        ProgramarReinicio(0.2f);
    }

    void ErrorAndroid(int codigo)
    {
        escuchandoAndroid = false;
        switch (codigo)
        {
            case 9: Estado("Falta el permiso de micrófono"); lock (candado) Activo = false; return;      // INSUFFICIENT_PERMISSIONS
            case 2: case 1: Estado("Sin internet para reconocer la voz"); ProgramarReinicio(3f); return; // NETWORK / NETWORK_TIMEOUT
            case 8: ProgramarReinicio(0.8f); return;                                                       // RECOGNIZER_BUSY
            default: ProgramarReinicio(0.3f); return;                                                      // sin coincidencia, silencio, etc.
        }
    }

    void ProgramarReinicio(float segundos)
    {
        lock (candado) reiniciarEn = segundos;   // se convierte en tiempo real en Update (aquí estamos en otro hilo)
    }

    void ActualizarPlataforma(bool hablando)
    {
        if (esperandoPermiso && UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            esperandoPermiso = false;
            Arrancar();
        }

        float espera;
        lock (candado) { espera = reiniciarEn; reiniciarEn = -1f; }
        if (espera >= 0f) siguienteInicio = Time.unscaledTime + espera;

        // Volver a escuchar cuando toque, pero no mientras el narrador habla.
        if (Activo && !escuchandoAndroid && reconocedor != null && siguienteInicio > 0f &&
            Time.unscaledTime >= siguienteInicio && !hablando)
        {
            siguienteInicio = 0f;
            escuchandoAndroid = true;
            EnHiloAndroid(EscucharUnaVez);
        }
    }

    float siguienteInicio;

    public void Detener()
    {
        Activo = false;
        esperandoPermiso = false;
        siguienteInicio = 0f;
        if (reconocedor != null) EnHiloAndroid(() => reconocedor.Call("cancel"));
        escuchandoAndroid = false;
    }

    void OnApplicationQuit()
    {
        if (reconocedor != null) EnHiloAndroid(() => { reconocedor.Call("destroy"); reconocedor.Dispose(); reconocedor = null; });
    }

#else
    public void Iniciar() { Estado("Voz no disponible en esta plataforma"); }
    public void Detener() { Activo = false; }
    void ActualizarPlataforma(bool hablando) { }
#endif
}

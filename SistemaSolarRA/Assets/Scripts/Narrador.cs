using System.Text.RegularExpressions;
using UnityEngine;

// Lee textos en voz alta (texto a voz) en español:
//   Android: el motor de voz del teléfono (android.speech.tts.TextToSpeech, normalmente Google).
//   Windows: una voz en español instalada en el sistema (p. ej. "Microsoft Sabina"), a través de un
//            proceso de PowerShell que queda abierto y recibe cada frase (System.Speech).
// Se silencia junto con el botón "Sonido".
public class Narrador : MonoBehaviour
{
    public static Narrador Instancia { get; private set; }

    [Tooltip("Caracteres por segundo, para estimar cuánto dura cada frase (en Windows).")]
    public float caracteresPorSegundo = 14f;

    float finEstimado;
    bool disponible;

    // ¿Está leyendo algo ahora? (el reconocimiento de voz no escucha mientras tanto, para no oírse a sí mismo)
    public bool Hablando
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (tts != null && listo)
                try { return tts.Call<bool>("isSpeaking"); } catch { }
#endif
            return Time.unscaledTime < finEstimado;
        }
    }

    bool Silenciado => SonidoEspacial.Instancia != null && !SonidoEspacial.Instancia.Activo;

    void Awake()
    {
        Instancia = this;
        IniciarMotor();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        DetenerMotor();
    }

    public void Decir(string texto)
    {
        if (!disponible || Silenciado || string.IsNullOrWhiteSpace(texto)) return;
        texto = Limpiar(texto);
        finEstimado = Time.unscaledTime + 0.4f + texto.Length / caracteresPorSegundo;
        Enviar(texto);
    }

    public void Callar()
    {
        finEstimado = 0f;
        if (disponible) Enviar("");
    }

    // Quita las etiquetas de formato (<b>, <color>...) y cambia símbolos por palabras.
    static string Limpiar(string texto)
    {
        texto = Regex.Replace(texto, "<[^>]+>", "");
        return texto.Replace("≈", "aproximadamente ").Replace("°C", " grados").Replace("·", ",")
                    .Replace("\n", ". ").Replace("…", ".");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // ---------- Android ----------
    AndroidJavaObject tts;
    bool listo;

    class AlIniciar : AndroidJavaProxy
    {
        readonly Narrador narrador;
        public AlIniciar(Narrador n) : base("android.speech.tts.TextToSpeech$OnInitListener") { narrador = n; }
        void onInit(int estado) => narrador.MotorListo(estado == 0);
    }

    void IniciarMotor()
    {
        try
        {
            var actividad = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", actividad, new AlIniciar(this));
        }
        catch (System.Exception e) { Debug.LogWarning("Texto a voz no disponible: " + e.Message); }
    }

    // Lo llama Android (en otro hilo) cuando el motor terminó de cargar.
    void MotorListo(bool ok)
    {
        if (!ok || tts == null) return;
        // Español de Perú; si el motor no lo tiene, español general.
        foreach (var (idioma, pais) in new[] { ("es", "PE"), ("es", "US"), ("es", "ES"), ("es", "") })
        {
            using var locale = new AndroidJavaObject("java.util.Locale", idioma, pais);
            if (tts.Call<int>("setLanguage", locale) >= 0) break;
        }
        listo = true;
        disponible = true;
    }

    void Enviar(string texto)
    {
        if (tts == null || !listo) return;
        if (texto.Length == 0) { tts.Call<int>("stop"); return; }
        using var parametros = new AndroidJavaObject("android.os.Bundle");
        tts.Call<int>("speak", texto, 0 /* QUEUE_FLUSH: corta lo anterior */, parametros, "narrador");
    }

    void DetenerMotor()
    {
        if (tts == null) return;
        try { tts.Call("shutdown"); } catch { }
        tts.Dispose();
        tts = null;
    }

#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    // ---------- Windows ----------
    System.Diagnostics.Process proceso;

    // Script de PowerShell: carga una voz en español y lee cada línea que recibe (en base64, para no perder tildes).
    // Una línea vacía solo corta lo que se estaba diciendo.
    const string Script =
        "Add-Type -AssemblyName System.Speech\n" +
        "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer\n" +
        "$v = $s.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -like 'es*' } | Select-Object -First 1\n" +
        "if ($v) { $s.SelectVoice($v.VoiceInfo.Name); [Console]::Out.WriteLine('VOZ ' + $v.VoiceInfo.Name) } else { [Console]::Out.WriteLine('SIN_VOZ') }\n" +
        "$s.Rate = 1\n" +
        "while ($true) {\n" +
        "  $l = [Console]::In.ReadLine()\n" +
        "  if ($l -eq $null) { break }\n" +
        "  $s.SpeakAsyncCancelAll()\n" +
        "  if ($l.Length -gt 0) { $s.SpeakAsync([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($l))) | Out-Null }\n" +
        "}\n";

    void IniciarMotor()
    {
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe", // Windows PowerShell 5.1: siempre viene con Windows y trae System.Speech
                Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " +
                            System.Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(Script)),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };
            proceso = System.Diagnostics.Process.Start(info);
            proceso.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.Log("Narrador: " + e.Data); };
            proceso.BeginOutputReadLine();
            disponible = true;
        }
        catch (System.Exception e) { Debug.LogWarning("Texto a voz no disponible: " + e.Message); }
    }

    void Enviar(string texto)
    {
        if (proceso == null || proceso.HasExited) return;
        try
        {
            proceso.StandardInput.WriteLine(texto.Length == 0 ? "" :
                System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(texto)));
            proceso.StandardInput.Flush();
        }
        catch (System.Exception e) { Debug.LogWarning("Narrador: " + e.Message); }
    }

    void DetenerMotor()
    {
        if (proceso == null) return;
        try
        {
            if (!proceso.HasExited)
            {
                proceso.StandardInput.Close(); // el script termina al cerrarse la entrada
                if (!proceso.WaitForExit(500)) proceso.Kill();
            }
        }
        catch { }
        proceso.Dispose();
        proceso = null;
    }

#else
    void IniciarMotor() { }
    void Enviar(string texto) { }
    void DetenerMotor() { }
#endif
}

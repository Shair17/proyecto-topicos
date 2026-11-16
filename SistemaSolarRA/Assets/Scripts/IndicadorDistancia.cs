using System.Globalization;
using TMPro;
using UnityEngine;

// Muestra a qué distancia REAL del Sol está la cámara (en UA y en km).
// La escena no está a escala: se interpola entre las órbitas de la escena y las distancias reales de cada planeta.
public class IndicadorDistancia : MonoBehaviour
{
    public TextMeshProUGUI texto;

    // Distancia real al Sol en unidades astronómicas (1 UA = 149,6 millones de km), de Mercurio a Neptuno.
    static readonly float[] DistanciaRealUA = { 0.387f, 0.723f, 1f, 1.524f, 5.203f, 9.537f, 19.19f, 30.07f };
    const float RadioSolUA = 0.00465f;

    float[] radiosEscena;
    float[] radiosReales;
    float siguienteActualizacion;

    void Start()
    {
        var builder = FindObjectOfType<SolarSystemBuilder>();
        if (builder == null || builder.Planetas == null) { enabled = false; return; }
        int n = builder.Planetas.Length; // Sol + 8 planetas
        radiosEscena = new float[n];
        radiosReales = new float[n];
        radiosEscena[0] = builder.Planetas[0].tamanoRelativo * builder.tamanoBase * 0.5f;
        radiosReales[0] = RadioSolUA;
        for (int i = 1; i < n; i++)
        {
            radiosEscena[i] = builder.RadioOrbita(builder.Planetas[i].radioOrbitaRelativo);
            radiosReales[i] = DistanciaRealUA[i - 1];
        }
    }

    void Update()
    {
        if (texto == null || Time.unscaledTime < siguienteActualizacion) return;
        siguienteActualizacion = Time.unscaledTime + 0.1f;
        var cam = CamaraExplorador.Instancia;
        if (cam == null) return;

        float ua = AUnidadesAstronomicas(cam.transform.position.magnitude);
        float millonesKm = ua * 149.6f;
        string km = millonesKm >= 1f
            ? Num(millonesKm, millonesKm < 100f ? "0.0" : "#,0") + " millones de km"
            : Num(millonesKm * 1000f, "#,0") + " mil km";
        texto.text = $"Distancia al Sol: <b>{Num(ua, ua < 10f ? "0.00" : "0.0")} UA</b>  ·  {km}";
    }

    // Formato en español sin depender de la cultura del sistema: 1 432,5 (espacio para miles, coma decimal).
    static string Num(float valor, string formato) =>
        valor.ToString(formato, CultureInfo.InvariantCulture).Replace(",", " ").Replace(".", ",");

    float AUnidadesAstronomicas(float radio)
    {
        int n = radiosEscena.Length;
        if (radio <= radiosEscena[0]) return RadioSolUA;
        for (int i = 1; i < n; i++)
            if (radio <= radiosEscena[i])
                return Mathf.Lerp(radiosReales[i - 1], radiosReales[i],
                    Mathf.InverseLerp(radiosEscena[i - 1], radiosEscena[i], radio));
        // Más allá de Neptuno: se sigue con la misma proporción del último tramo.
        float pendiente = (radiosReales[n - 1] - radiosReales[n - 2]) / (radiosEscena[n - 1] - radiosEscena[n - 2]);
        return radiosReales[n - 1] + (radio - radiosEscena[n - 1]) * pendiente;
    }
}

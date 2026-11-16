using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Botones de la interfaz (y comandos de voz): pausar/reanudar y cambiar la velocidad de las órbitas.
public class SolarSystemController : MonoBehaviour
{
    public TextMeshProUGUI textoBotonPausa;
    [Tooltip("Slider de velocidad (opcional): se mueve cuando la velocidad cambia por voz.")]
    public Slider slider;

    bool pausado;
    float velocidad = 1f;

    void Start()
    {
        Aplicar();
    }

    // Conectar al OnClick del botón "Pausar".
    public void AlternarPausa()
    {
        pausado = !pausado;
        Aplicar();
    }

    public void Pausar(bool pausar)
    {
        pausado = pausar;
        Aplicar();
    }

    // Conectar al OnValueChanged del Slider (rango sugerido 0,1 a 5).
    public void CambiarVelocidad(float valor)
    {
        velocidad = valor;
        Aplicar();
    }

    // "Más rápido" / "más lento" por voz. Si hay slider, se mueve (y él llama a CambiarVelocidad).
    public void MultiplicarVelocidad(float factor)
    {
        pausado = false;
        float nueva = Mathf.Clamp(velocidad * factor, 0.1f, 5f);
        if (slider != null) slider.value = nueva;
        else CambiarVelocidad(nueva);
        Aplicar();
    }

    void Aplicar()
    {
        PlanetOrbit.multiplicadorVelocidad = pausado ? 0f : velocidad;
        if (textoBotonPausa != null)
            textoBotonPausa.text = pausado ? "Reanudar" : "Pausar";
    }
}

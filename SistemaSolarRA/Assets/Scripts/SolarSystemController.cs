using TMPro;
using UnityEngine;

// Botones de la interfaz: pausar/reanudar y cambiar la velocidad de las órbitas.
public class SolarSystemController : MonoBehaviour
{
    public TextMeshProUGUI textoBotonPausa;

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

    // Conectar al OnValueChanged del Slider (rango sugerido 0,1 a 5).
    public void CambiarVelocidad(float valor)
    {
        velocidad = valor;
        Aplicar();
    }

    void Aplicar()
    {
        PlanetOrbit.multiplicadorVelocidad = pausado ? 0f : velocidad;
        if (textoBotonPausa != null)
            textoBotonPausa.text = pausado ? "Reanudar" : "Pausar";
    }
}

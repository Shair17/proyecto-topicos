using UnityEngine;
using UnityEngine.EventSystems;

// Botón que actúa mientras se mantiene presionado (Avanzar = +1, Retroceder = -1).
// Lo crea AppUIBuilder en el celular; mueve la cámara a través de CamaraExplorador.avanceBotones.
public class BotonMantener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float valor = 1f;
    bool presionado;

    public void OnPointerDown(PointerEventData e)
    {
        presionado = true;
        CamaraExplorador.avanceBotones = valor;
    }

    public void OnPointerUp(PointerEventData e) => Soltar();
    public void OnPointerExit(PointerEventData e) => Soltar();
    void OnDisable() => Soltar();

    void Soltar()
    {
        if (!presionado) return;
        presionado = false;
        CamaraExplorador.avanceBotones = 0f;
    }
}

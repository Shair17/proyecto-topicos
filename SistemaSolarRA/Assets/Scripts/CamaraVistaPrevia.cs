using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Solo para la vista previa en PC (sin cubo ni Vuforia): gira la cámara alrededor del Sol.
// Clic derecho + arrastrar = girar, rueda del mouse = acercar/alejar.
// En el celular: arrastrar un dedo = girar, pellizcar con dos dedos = acercar/alejar.
// Si nadie la mueve por unos segundos, gira sola despacio (modo "vitrina").
public class CamaraVistaPrevia : MonoBehaviour
{
    public Vector3 centro = Vector3.zero;
    public float distancia = 0.55f;
    public float distanciaMin = 0.12f;
    public float distanciaMax = 1.5f;
    public float sensibilidad = 0.3f;
    public float giroAutomatico = 6f;      // grados por segundo
    public float esperaGiroAutomatico = 3f;

    float giroY = 0f;
    float giroX = 35f;
    float quieta;

    void LateUpdate()
    {
        LeerEntrada(out Vector2 arrastre, out float rueda);
        quieta = arrastre != Vector2.zero || rueda != 0f ? 0f : quieta + Time.deltaTime;
        if (quieta > esperaGiroAutomatico) giroY += giroAutomatico * Time.deltaTime;
        giroY += arrastre.x * sensibilidad;
        giroX = Mathf.Clamp(giroX - arrastre.y * sensibilidad, 5f, 89f);
        distancia = Mathf.Clamp(distancia * (1f - rueda * 0.1f), distanciaMin, distanciaMax);

        var rot = Quaternion.Euler(giroX, giroY, 0f);
        transform.position = centro + rot * new Vector3(0f, 0f, -distancia);
        transform.rotation = rot;
    }

    static void LeerEntrada(out Vector2 arrastre, out float rueda)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        arrastre = mouse != null && mouse.rightButton.isPressed ? mouse.delta.ReadValue() : Vector2.zero;
        rueda = mouse != null ? Mathf.Sign(mouse.scroll.ReadValue().y) * (mouse.scroll.ReadValue().y != 0 ? 1f : 0f) : 0f;
        var pantalla = Touchscreen.current;
        if (pantalla != null && pantalla.primaryTouch.press.isPressed &&
            !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            arrastre = pantalla.primaryTouch.delta.ReadValue() * 0.5f;
#else
        arrastre = Input.GetMouseButton(1)
            ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f
            : Vector2.zero;
        rueda = Mathf.Sign(Input.mouseScrollDelta.y) * (Input.mouseScrollDelta.y != 0 ? 1f : 0f);

        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            bool sobreUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
            if (t.phase == TouchPhase.Moved && !sobreUI) arrastre = t.deltaPosition * 0.5f;
        }
        else if (Input.touchCount == 2)
        {
            var a = Input.GetTouch(0);
            var b = Input.GetTouch(1);
            float antes = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
            float ahora = Vector2.Distance(a.position, b.position);
            rueda = (ahora - antes) * 0.02f;
        }
#endif
    }
}

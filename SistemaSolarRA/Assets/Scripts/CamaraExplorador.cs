using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Cámara de la realidad virtual: el usuario flota dentro del sistema solar y se mueve libremente.
//   PC:      WASD o flechas = moverse, Q/E = bajar/subir, Shift = más rápido,
//            arrastrar con el mouse = mirar alrededor, rueda = avanzar/retroceder.
//   Celular: girar el teléfono = mirar (giroscopio), arrastrar un dedo = mirar,
//            pellizcar = avanzar/retroceder, botones "Avanzar"/"Retroceder" (BotonMantener).
// Viajar(planeta) lleva la cámara volando hasta el planeta y luego lo sigue en su órbita.
public class CamaraExplorador : MonoBehaviour
{
    public static CamaraExplorador Instancia { get; private set; }

    // Lo escriben los botones Avanzar (+1) y Retroceder (-1) mientras se mantienen presionados.
    public static float avanceBotones;
    // Lo escribe InfoPanelUI: con la ficha abierta el planeta seguido se corre a la izquierda.
    public static bool fichaAbierta;
    float desplazamiento;
    Camera camara;

    [Tooltip("Metros por segundo. Todo el sistema solar mide aprox. 0,5 m de radio.")]
    public float velocidad = 0.12f;
    [Tooltip("Grados que gira la vista por cada píxel arrastrado.")]
    public float sensibilidad = 0.15f;
    [Tooltip("Distancia máxima al Sol, para no perderse en el espacio.")]
    public float limite = 1.6f;
    public float duracionViaje = 2.2f;

    enum Modo { Libre, Viajando, Siguiendo, Volviendo }
    Modo modo = Modo.Libre;

    float yaw, pitch;                 // giro horizontal y vertical (con giroscopio, yaw es el ajuste)
    Vector3 posicionInicial;
    float yawInicial, pitchInicial;

    Transform objetivo;
    float distanciaObjetivo;
    Vector3 inicioViaje;
    float yawInicioViaje, pitchInicioViaje;
    float tViaje;

    // Estado de la entrada
    Vector3 mouseAnterior;
    bool arrastrandoMouse;
    readonly HashSet<int> dedosSobreUI = new HashSet<int>();
    float distanciaDedosAnterior = -1f;

    public bool GiroscopioActivo { get; private set; }
    bool calibrarGiroscopio;
    float yawAlCalibrar;

    public bool GiroscopioDisponible
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return AttitudeSensor.current != null;
#else
            return SystemInfo.supportsGyroscope;
#endif
        }
    }

    public bool SiguiendoPlaneta => modo == Modo.Viajando || modo == Modo.Siguiendo;

    void Awake()
    {
        Instancia = this;
        camara = GetComponent<Camera>();
        posicionInicial = transform.position;
        var e = transform.eulerAngles;
        yawInicial = yaw = e.y;
        pitchInicial = pitch = Normalizar(e.x);
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        avanceBotones = 0f;
    }

    // ---------- Acciones públicas (botones de la interfaz) ----------

    public void Viajar(Transform planeta)
    {
        if (planeta == null) return;
        objetivo = planeta;
        // Distancia para ver el planeta completo: unas veces su diámetro (el Sol necesita menos).
        float diametro = planeta.lossyScale.x;
        var info = planeta.GetComponent<PlanetInfo>();
        bool esSol = info != null && info.datos != null && info.datos.nombre == "Sol";
        float veces = esSol ? 2.3f : planeta.Find("Anillo") != null ? 4.8f : 3.2f; // Saturno: que entre su anillo
        distanciaObjetivo = Mathf.Max(diametro * veces, 0.025f);
        IniciarViaje(Modo.Viajando);
    }

    public void VistaGeneral()
    {
        objetivo = null;
        IniciarViaje(Modo.Volviendo);
    }

    // Devuelve si quedó encendido.
    public bool AlternarGiroscopio()
    {
        if (!GiroscopioActivo && GiroscopioDisponible)
        {
            // Mantiene la dirección actual: al primer dato válido se calcula el ajuste (ver Rotacion).
            yawAlCalibrar = transform.eulerAngles.y;
            calibrarGiroscopio = true;
            GiroscopioActivo = true;
        }
        else
        {
            var e = transform.eulerAngles;
            yaw = e.y;
            pitch = Mathf.Clamp(Normalizar(e.x), -89f, 89f);
            GiroscopioActivo = false;
            calibrarGiroscopio = false;
        }
        return GiroscopioActivo;
    }

    void IniciarViaje(Modo nuevo)
    {
        modo = nuevo;
        tViaje = 0f;
        inicioViaje = transform.position;
        yawInicioViaje = yaw;
        pitchInicioViaje = pitch;
    }

    // ---------- Cada cuadro ----------

    void LateUpdate()
    {
        LeerEntrada(out Vector2 arrastre, out float impulso, out Vector3 mover, out bool rapido);
        float dt = Time.unscaledDeltaTime; // la cámara se mueve aunque el sistema esté en pausa

        yaw += arrastre.x * sensibilidad;
        if (!GiroscopioActivo) pitch = Mathf.Clamp(pitch - arrastre.y * sensibilidad, -89f, 89f);

        // Moverse con el teclado suelta el planeta que se estaba siguiendo.
        if (mover != Vector3.zero && modo != Modo.Libre) modo = Modo.Libre;

        Quaternion rot = Rotacion();
        float avanceContinuo = avanceBotones;

        switch (modo)
        {
            case Modo.Libre:
            {
                float v = velocidad * (rapido ? 3f : 1f);
                Vector3 paso = rot * mover * v * dt
                             + rot * Vector3.forward * (avanceContinuo * v * dt + impulso * velocidad * 0.4f);
                transform.position = Vector3.ClampMagnitude(transform.position + paso, limite);
                break;
            }
            case Modo.Siguiendo:
            {
                // Acercarse o alejarse del planeta con la rueda, el pellizco o los botones.
                float factor = Mathf.Exp(-(impulso * 0.25f + avanceContinuo * dt * 1.2f));
                distanciaObjetivo = Mathf.Clamp(distanciaObjetivo * factor, objetivo.lossyScale.x * 0.8f, 0.8f);
                transform.position = PosicionJuntoAlPlaneta(rot, dt);
                break;
            }
            case Modo.Viajando:
            case Modo.Volviendo:
            {
                tViaje += dt / duracionViaje;
                float p = Suave(Mathf.Clamp01(tViaje));
                Vector3 destino;
                if (modo == Modo.Viajando)
                {
                    if (objetivo == null) { modo = Modo.Libre; break; }
                    if (!GiroscopioActivo && DireccionIluminada(out float yawDestino, out float pitchDestino))
                    {
                        yaw = Mathf.LerpAngle(yawInicioViaje, yawDestino, p);
                        pitch = Mathf.Lerp(pitchInicioViaje, pitchDestino, p);
                        rot = Rotacion();
                    }
                    destino = PosicionJuntoAlPlaneta(rot, dt);
                }
                else
                {
                    destino = posicionInicial;
                    if (!GiroscopioActivo)
                    {
                        yaw = Mathf.LerpAngle(yawInicioViaje, yawInicial, p);
                        pitch = Mathf.Lerp(pitchInicioViaje, pitchInicial, p);
                        rot = Rotacion();
                    }
                }
                transform.position = Vector3.Lerp(inicioViaje, destino, p);
                if (tViaje >= 1f) modo = modo == Modo.Viajando ? Modo.Siguiendo : Modo.Libre;
                break;
            }
        }
        transform.rotation = rot;
    }

    // Detrás del planeta según hacia dónde se mira. Con la ficha abierta (a la derecha de la pantalla),
    // la cámara se corre a la derecha para que el planeta quede en la parte libre, a la izquierda.
    Vector3 PosicionJuntoAlPlaneta(Quaternion rot, float dt)
    {
        float correr = fichaAbierta ? 0.2f * (camara != null ? camara.aspect : 1.78f) : 0f;
        desplazamiento = Mathf.Lerp(desplazamiento, correr, 1f - Mathf.Exp(-4f * dt));
        return objetivo.position + rot * new Vector3(desplazamiento, 0f, -1f) * distanciaObjetivo;
    }

    // Dirección para mirar el planeta desde su lado de día: la cámara queda entre el planeta y el Sol,
    // un poco de costado y por encima. Devuelve false para el Sol (no tiene "lado de día").
    bool DireccionIluminada(out float yawDestino, out float pitchDestino)
    {
        yawDestino = yaw;
        pitchDestino = pitch;
        Vector3 haciaSol = objetivo.root.position - objetivo.position;
        haciaSol.y = 0f;
        if (haciaSol.sqrMagnitude < 1e-6f) return false;
        haciaSol.Normalize();
        Vector3 lateral = Vector3.Cross(Vector3.up, haciaSol);
        if (Vector3.Dot(lateral, inicioViaje - objetivo.position) < 0f) lateral = -lateral; // el costado más cercano
        Vector3 mirar = -(haciaSol * 0.75f + lateral * 0.55f + Vector3.up * 0.3f).normalized;
        yawDestino = Mathf.Atan2(mirar.x, mirar.z) * Mathf.Rad2Deg;
        pitchDestino = -Mathf.Asin(mirar.y) * Mathf.Rad2Deg;
        return true;
    }

    Quaternion Rotacion()
    {
        if (GiroscopioActivo && LeerGiroscopio(out Quaternion g))
        {
            if (calibrarGiroscopio)
            {
                yaw = yawAlCalibrar - g.eulerAngles.y;
                calibrarGiroscopio = false;
            }
            return Quaternion.Euler(0f, yaw, 0f) * g;
        }
        return calibrarGiroscopio ? transform.rotation : Quaternion.Euler(pitch, yaw, 0f);
    }

    // Convierte la orientación del teléfono (mano derecha) al sistema de Unity (mano izquierda).
    static bool LeerGiroscopio(out Quaternion rotacion)
    {
        rotacion = Quaternion.identity;
        Quaternion q;
#if ENABLE_INPUT_SYSTEM
        var sensor = AttitudeSensor.current;
        if (sensor == null) return false;
        if (!sensor.enabled) InputSystem.EnableDevice(sensor);
        q = sensor.attitude.ReadValue();
#else
        if (!SystemInfo.supportsGyroscope) return false;
        if (!Input.gyro.enabled) Input.gyro.enabled = true;
        q = Input.gyro.attitude;
#endif
        // Recién encendido, el sensor puede devolver un cuaternión vacío durante algunos cuadros.
        if (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w < 0.5f) return false;
        rotacion = Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);
        return true;
    }

    static float Suave(float p) => p * p * (3f - 2f * p);

    static float Normalizar(float angulo) => angulo > 180f ? angulo - 360f : angulo;

    static bool SobreUI(int id) =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id);

    // ---------- Entrada (los dos sistemas de input de Unity) ----------

    void LeerEntrada(out Vector2 arrastre, out float impulso, out Vector3 mover, out bool rapido)
    {
        arrastre = Vector2.zero;
        impulso = 0f;
        mover = Vector3.zero;
        rapido = false;

#if ENABLE_INPUT_SYSTEM
        var teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) mover.z += 1f;
            if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) mover.z -= 1f;
            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) mover.x += 1f;
            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) mover.x -= 1f;
            if (teclado.eKey.isPressed) mover.y += 1f;
            if (teclado.qKey.isPressed) mover.y -= 1f;
            rapido = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
        }

        var pantalla = Touchscreen.current;
        var dedos = new List<Vector2>();
        var deltas = new List<Vector2>();
        if (pantalla != null)
            foreach (var t in pantalla.touches)
            {
                if (!t.press.isPressed) { dedosSobreUI.Remove(t.touchId.ReadValue()); continue; }
                int id = t.touchId.ReadValue();
                if (t.press.wasPressedThisFrame && SobreUI(id)) dedosSobreUI.Add(id);
                if (dedosSobreUI.Contains(id)) continue;
                dedos.Add(t.position.ReadValue());
                deltas.Add(t.delta.ReadValue());
            }
        if (dedos.Count > 0) ProcesarDedos(dedos, deltas, ref arrastre, ref impulso);
        else
        {
            distanciaDedosAnterior = -1f;
            var mouse = Mouse.current;
            if (mouse != null && pantalla == null)
            {
                bool presionado = mouse.leftButton.isPressed || mouse.rightButton.isPressed;
                bool empezo = mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
                if (empezo) arrastrandoMouse = !SobreUI(-1);
                if (!presionado) arrastrandoMouse = false;
                if (arrastrandoMouse) arrastre = mouse.delta.ReadValue();
                float rueda = mouse.scroll.ReadValue().y;
                if (rueda != 0f && !SobreUI(-1)) impulso = Mathf.Sign(rueda);
            }
        }
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) mover.z += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) mover.z -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) mover.x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) mover.x -= 1f;
        if (Input.GetKey(KeyCode.E)) mover.y += 1f;
        if (Input.GetKey(KeyCode.Q)) mover.y -= 1f;
        rapido = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Input.touchCount > 0)
        {
            var dedos = new List<Vector2>();
            var deltas = new List<Vector2>();
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && SobreUI(t.fingerId)) dedosSobreUI.Add(t.fingerId);
                bool termino = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                if (dedosSobreUI.Contains(t.fingerId))
                {
                    if (termino) dedosSobreUI.Remove(t.fingerId);
                    continue;
                }
                if (termino) continue;
                dedos.Add(t.position);
                deltas.Add(t.deltaPosition);
            }
            if (dedos.Count > 0) ProcesarDedos(dedos, deltas, ref arrastre, ref impulso);
            else distanciaDedosAnterior = -1f;
        }
        else
        {
            dedosSobreUI.Clear();
            distanciaDedosAnterior = -1f;
            bool presionado = Input.GetMouseButton(0) || Input.GetMouseButton(1);
            bool empezo = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
            if (empezo)
            {
                arrastrandoMouse = !SobreUI(-1);
                mouseAnterior = Input.mousePosition;
            }
            if (!presionado) arrastrandoMouse = false;
            if (arrastrandoMouse)
            {
                arrastre = Input.mousePosition - mouseAnterior;
                mouseAnterior = Input.mousePosition;
            }
            float rueda = Input.mouseScrollDelta.y;
            if (rueda != 0f && !SobreUI(-1)) impulso = Mathf.Sign(rueda);
        }
#endif
    }

    // Un dedo = mirar alrededor. Dos dedos = pellizcar para avanzar (abrir) o retroceder (cerrar).
    void ProcesarDedos(List<Vector2> dedos, List<Vector2> deltas, ref Vector2 arrastre, ref float impulso)
    {
        if (dedos.Count == 1)
        {
            distanciaDedosAnterior = -1f;
            arrastre = deltas[0] * 0.6f;
            return;
        }
        float distancia = Vector2.Distance(dedos[0], dedos[1]);
        if (distanciaDedosAnterior > 0f)
            impulso = (distancia - distanciaDedosAnterior) / Mathf.Max(Screen.dpi, 160f) * 2f;
        distanciaDedosAnterior = distancia;
    }
}

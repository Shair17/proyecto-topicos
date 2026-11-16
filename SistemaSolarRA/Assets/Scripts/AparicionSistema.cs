using System.Collections.Generic;
using UnityEngine;

// Animación de entrada al abrir la app: el Sol "explota" desde cero y los planetas salen
// girando hacia sus órbitas uno por uno. Arranca cuando el Renderer del Sol está encendido.
public class AparicionSistema : MonoBehaviour
{
    class Brazo
    {
        public Transform ancla;          // se mueve desde el centro hasta su órbita
        public Vector3 destino;
        public Transform[] escalables;   // líneas que crecen junto con el planeta (órbita, estela, cinturón)
    }

    public float duracionSol = 0.9f;
    public float retrasoPlanetas = 0.5f;
    public float separacion = 0.12f;
    public float duracionPlaneta = 1.1f;

    readonly List<Brazo> brazos = new List<Brazo>();
    Renderer sol;
    Light luz;
    float rangoLuz;
    Vector3 escalaFinal;
    float t = -1f;
    bool visibleAntes;
    float tiempoOculto = 99f;

    void Awake()
    {
        escalaFinal = transform.localScale;
    }

    public void Configurar(Renderer rendererSol, Light luzSol)
    {
        sol = rendererSol;
        luz = luzSol;
        if (luz != null) rangoLuz = luz.range;
    }

    public void Agregar(Transform ancla, params Transform[] escalables)
    {
        brazos.Add(new Brazo
        {
            ancla = ancla,
            destino = ancla != null ? ancla.localPosition : Vector3.zero,
            escalables = escalables,
        });
    }

    void Update()
    {
        bool visible = sol != null && sol.enabled;
        if (visible && !visibleAntes && tiempoOculto > 1f) Iniciar();
        tiempoOculto = visible ? 0f : tiempoOculto + Time.deltaTime;
        visibleAntes = visible;

        if (t >= 0f)
        {
            t += Time.deltaTime;
            Animar(t);
            if (t > retrasoPlanetas + separacion * brazos.Count + duracionPlaneta) t = -1f;
        }
        // El rango de la luz no se escala solo con el objeto padre.
        if (luz != null) luz.range = rangoLuz * transform.lossyScale.x;
    }

    void Iniciar()
    {
        t = 0f;
        Animar(0f);
        if (SonidoEspacial.Instancia != null) SonidoEspacial.Instancia.Aparicion();
    }

    void Animar(float tiempo)
    {
        transform.localScale = escalaFinal * Mathf.Max(0.001f, SalidaConRebote(Mathf.Clamp01(tiempo / duracionSol)));

        for (int i = 0; i < brazos.Count; i++)
        {
            float p = Mathf.Clamp01((tiempo - retrasoPlanetas - separacion * i) / duracionPlaneta);
            float e = 1f - Mathf.Pow(1f - p, 3f);
            var b = brazos[i];
            if (b.ancla != null) b.ancla.localPosition = b.destino * e;
            if (b.escalables == null) continue;
            foreach (var esc in b.escalables)
                if (esc != null) esc.localScale = new Vector3(Mathf.Max(e, 0.001f), 1f, Mathf.Max(e, 0.001f));
        }
    }

    static float SalidaConRebote(float p)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
    }
}

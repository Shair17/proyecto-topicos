using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Nombre flotando sobre cada planeta (y el Sol, la Luna y el cometa). Siempre mira a la cámara y
// mantiene el mismo tamaño en pantalla aunque uno se acerque o se aleje. Se prende y apaga con un botón.
public class EtiquetasPlanetas : MonoBehaviour
{
    [Tooltip("Tamaño del texto en pantalla (proporcional a la distancia).")]
    public float tamano = 0.2f;

    class Etiqueta
    {
        public Transform planeta;
        public Transform texto;
    }

    readonly List<Etiqueta> etiquetas = new List<Etiqueta>();
    Transform contenedor;
    public bool Visibles { get; private set; } = true;

    // En Start: los planetas ya los creó SolarSystemBuilder en su Awake.
    void Start()
    {
        contenedor = new GameObject("Etiquetas").transform;
        foreach (var info in FindObjectsOfType<PlanetInfo>())
        {
            var go = new GameObject("Etiqueta_" + info.datos.nombre);
            go.transform.SetParent(contenedor, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = info.datos.nombre;
            tmp.fontSize = 1f;
            tmp.alignment = TextAlignmentOptions.Bottom;
            tmp.color = Color.Lerp(info.datos.color, Color.white, 0.55f);
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(20f, 2f);
            tmp.rectTransform.pivot = new Vector2(0.5f, 0f);
            etiquetas.Add(new Etiqueta { planeta = info.transform, texto = go.transform });
        }
    }

    public bool Alternar()
    {
        Visibles = !Visibles;
        if (contenedor != null) contenedor.gameObject.SetActive(Visibles);
        return Visibles;
    }

    void LateUpdate()
    {
        if (!Visibles || contenedor == null) return;
        var cam = CamaraExplorador.Instancia != null ? CamaraExplorador.Instancia.transform
                : Camera.main != null ? Camera.main.transform : null;
        if (cam == null) return;

        foreach (var e in etiquetas)
        {
            float radio = e.planeta.lossyScale.x * 0.5f;
            Vector3 arriba = e.planeta.position + cam.up * radio * 1.15f;
            float distancia = Vector3.Distance(cam.position, arriba);
            // Muy cerca, el nombre estorba: se oculta cuando el planeta llena la pantalla.
            bool mostrar = distancia > radio * 2.2f;
            if (e.texto.gameObject.activeSelf != mostrar) e.texto.gameObject.SetActive(mostrar);
            if (!mostrar) continue;
            e.texto.position = arriba;
            e.texto.rotation = cam.rotation;
            e.texto.localScale = Vector3.one * distancia * tamano;
        }
    }
}

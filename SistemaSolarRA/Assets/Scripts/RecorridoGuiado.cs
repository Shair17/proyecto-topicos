using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Recorrido automático: la cámara viaja sola del Sol a Neptuno (y al cometa) mostrando la ficha de cada uno.
// Ideal para la exposición: se activa con un botón y no hay que tocar nada más.
// Se detiene si el usuario toma el control (se mueve con el teclado o viaja a otro lado).
public class RecorridoGuiado : MonoBehaviour
{
    public PlanetSelector selector;
    [Tooltip("Texto donde se muestra el avance (se usa la barra de ayuda).")]
    public TextMeshProUGUI textoEstado;
    public float permanencia = 7f;   // segundos en cada parada, después de llegar
    public System.Action<bool> alCambiar;

    static readonly string[] Orden =
        { "Sol", "Mercurio", "Venus", "Tierra", "Luna", "Marte", "Júpiter", "Saturno", "Urano", "Neptuno", "Cometa Halley" };

    public bool Activo { get; private set; }
    string textoOriginal;

    public bool Alternar()
    {
        if (Activo) Detener();
        else Iniciar();
        return Activo;
    }

    public void Iniciar()
    {
        var camara = CamaraExplorador.Instancia;
        if (camara == null || Activo) return;
        Activo = true;
        if (textoEstado != null) textoOriginal = textoEstado.text;
        StartCoroutine(Recorrer(camara));
        alCambiar?.Invoke(true);
    }

    public void Detener()
    {
        if (!Activo) return;
        Activo = false;
        StopAllCoroutines();
        if (textoEstado != null) textoEstado.text = textoOriginal;
        alCambiar?.Invoke(false);
    }

    IEnumerator Recorrer(CamaraExplorador camara)
    {
        var porNombre = new Dictionary<string, PlanetInfo>();
        foreach (var p in FindObjectsOfType<PlanetInfo>()) porNombre[p.datos.nombre] = p;
        var paradas = new List<PlanetInfo>();
        foreach (var nombre in Orden)
            if (porNombre.TryGetValue(nombre, out var p)) paradas.Add(p);

        for (int i = 0; i < paradas.Count; i++)
        {
            var planeta = paradas[i];
            selector.Seleccionar(planeta);
            camara.Viajar(planeta.transform);
            if (textoEstado != null)
                textoEstado.text = $"<b>Recorrido guiado</b>  {i + 1} de {paradas.Count}:  {planeta.datos.nombre}";

            // Se queda al menos 'permanencia' segundos, y si el narrador sigue leyendo la ficha, espera a que termine.
            for (float t = 0f; t < camara.duracionViaje + permanencia || (Narrador.Instancia != null && Narrador.Instancia.Hablando);
                 t += Time.unscaledDeltaTime)
            {
                if (camara.Objetivo != planeta.transform) { Detener(); yield break; } // el usuario tomó el control
                yield return null;
            }
        }
        selector.Deseleccionar();
        camara.VistaGeneral();
        Detener();
    }
}

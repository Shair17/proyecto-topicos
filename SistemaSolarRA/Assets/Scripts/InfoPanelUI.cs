using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ficha con los datos del planeta seleccionado. Aparece con una animación corta (se agranda y se aclara).
public class InfoPanelUI : MonoBehaviour
{
    public TextMeshProUGUI titulo;
    public TextMeshProUGUI cuerpo;
    [Tooltip("Franja que toma el color del planeta (opcional).")]
    public Image acento;

    CanvasGroup grupo;

    public void Mostrar(PlanetData d)
    {
        titulo.text = d.nombre;
        cuerpo.text =
            $"<b>Tipo:</b> {d.tipo}\n" +
            $"<b>Diámetro:</b> {d.diametro}\n" +
            $"<b>Distancia{(d.nombre == "Luna" ? "" : " al Sol")}:</b> {d.distanciaSol}\n" +
            $"<b>Un día dura:</b> {d.duracionDia}\n" +
            $"<b>Un año dura:</b> {d.duracionAnio}\n" +
            $"<b>Lunas:</b> {d.lunas}\n\n" +
            $"<i>{d.datoCurioso}</i>";
        if (acento != null) acento.color = d.color;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Entrar());
    }

    public void Ocultar()
    {
        gameObject.SetActive(false);
    }

    IEnumerator Entrar()
    {
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        const float duracion = 0.25f;
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float p = 1f - Mathf.Pow(1f - t / duracion, 3f);
            grupo.alpha = p;
            transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, p);
            yield return null;
        }
        grupo.alpha = 1f;
        transform.localScale = Vector3.one;
    }
}

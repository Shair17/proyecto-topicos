using TMPro;
using UnityEngine;

// Ficha con los datos del planeta seleccionado. Va en el Panel del Canvas,
// que debe empezar desactivado en la escena.
public class InfoPanelUI : MonoBehaviour
{
    public TextMeshProUGUI titulo;
    public TextMeshProUGUI cuerpo;

    public void Mostrar(PlanetData d)
    {
        titulo.text = d.nombre;
        cuerpo.text =
            $"<b>Tipo:</b> {d.tipo}\n" +
            $"<b>Diámetro:</b> {d.diametro}\n" +
            $"<b>Distancia al Sol:</b> {d.distanciaSol}\n" +
            $"<b>Un día dura:</b> {d.duracionDia}\n" +
            $"<b>Un año dura:</b> {d.duracionAnio}\n" +
            $"<b>Lunas:</b> {d.lunas}\n\n" +
            $"<i>{d.datoCurioso}</i>";
        gameObject.SetActive(true);
    }

    public void Ocultar()
    {
        gameObject.SetActive(false);
    }
}

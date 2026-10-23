using UnityEngine;

// Va en cada planeta: guarda sus datos y lo resalta cuando se selecciona.
public class PlanetInfo : MonoBehaviour
{
    public PlanetData datos;

    Vector3 escalaOriginal;

    void Start()
    {
        escalaOriginal = transform.localScale;
    }

    public void Resaltar(bool activo)
    {
        transform.localScale = activo ? escalaOriginal * 1.4f : escalaOriginal;
    }
}

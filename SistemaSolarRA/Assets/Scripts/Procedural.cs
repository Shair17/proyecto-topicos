using UnityEngine;

// Texturas, mallas y partículas generadas por código (no hacen falta archivos extra).
public static class Procedural
{
    static Texture2D brillo;

    // Círculo difuso blanco: sirve para el resplandor del Sol, las estrellas y los halos.
    public static Texture2D TexturaBrillo()
    {
        if (brillo != null) return brillo;
        const int n = 128;
        brillo = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Brillo" };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float v = Mathf.Pow(1f - d, 2.4f);
                px[y * n + x] = new Color(v, v, v, v);
            }
        brillo.SetPixels(px);
        brillo.Apply();
        return brillo;
    }

    // Anillo de Saturno de respaldo (bandas) por si no está la textura descargada.
    public static Texture2D TexturaAnilloRespaldo(Color color)
    {
        const int n = 256;
        var tex = new Texture2D(n, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "AnilloRespaldo" };
        for (int x = 0; x < n; x++)
        {
            float u = x / (float)n;
            float bandas = 0.55f + 0.45f * Mathf.PerlinNoise(u * 40f, 0.3f);
            float hueco = (u > 0.62f && u < 0.67f) ? 0.1f : 1f; // división de Cassini
            float borde = Mathf.SmoothStep(0f, 1f, u * 8f) * Mathf.SmoothStep(0f, 1f, (1f - u) * 8f);
            tex.SetPixel(x, 0, new Color(color.r * bandas, color.g * bandas, color.b * bandas, bandas * hueco * borde * 0.9f));
        }
        tex.Apply();
        return tex;
    }

    // Disco con agujero (anillo plano) de doble cara. u = 0 en el borde interior y 1 en el exterior.
    public static Mesh MallaAnillo(float radioInterior, float radioExterior, int segmentos = 128)
    {
        int v = (segmentos + 1) * 2;
        var vertices = new Vector3[v * 2];
        var normales = new Vector3[v * 2];
        var uv = new Vector2[v * 2];
        var tris = new int[segmentos * 12];
        for (int i = 0; i <= segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int cara = 0; cara < 2; cara++)
            {
                int b = cara * v + i * 2;
                vertices[b] = dir * radioInterior;
                vertices[b + 1] = dir * radioExterior;
                normales[b] = normales[b + 1] = cara == 0 ? Vector3.up : Vector3.down;
                uv[b] = new Vector2(0f, 0.5f);
                uv[b + 1] = new Vector2(1f, 0.5f);
            }
        }
        int t = 0;
        for (int i = 0; i < segmentos; i++)
        {
            int a0 = i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
            // Cara de arriba
            tris[t++] = a0; tris[t++] = b1; tris[t++] = a1;
            tris[t++] = a0; tris[t++] = b0; tris[t++] = b1;
            // Cara de abajo (orden invertido)
            tris[t++] = v + a0; tris[t++] = v + a1; tris[t++] = v + b1;
            tris[t++] = v + a0; tris[t++] = v + b1; tris[t++] = v + b0;
        }
        var malla = new Mesh { name = "Anillo", vertices = vertices, normals = normales, uv = uv, triangles = tris };
        malla.RecalculateBounds();
        return malla;
    }

    // Sistema de partículas "quieto" (las partículas se colocan a mano con Emit) o continuo (emisión normal).
    // Escala con la jerarquía, así que los tamaños se dan en unidades locales del objeto padre.
    public static ParticleSystem CrearParticulas(string nombre, Transform padre, Material material, int maximo, bool continuo)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = continuo;
        main.playOnAwake = continuo;
        main.maxParticles = maximo;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        if (!continuo) main.startLifetime = 1e6f;

        var emision = ps.emission;
        emision.enabled = continuo;
        var forma = ps.shape;
        forma.enabled = continuo;

        var render = go.GetComponent<ParticleSystemRenderer>();
        render.material = material;
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.minParticleSize = 0f;
        render.maxParticleSize = 5f;

        if (continuo) ps.Play();
        return ps;
    }

    // Coloca una sola partícula fija (sirve como "sprite" que siempre mira a la cámara).
    public static void EmitirFija(ParticleSystem ps, Vector3 posicion, float tamano, Color color)
    {
        ps.Emit(new ParticleSystem.EmitParams
        {
            position = posicion,
            startSize = tamano,
            startColor = color,
            startLifetime = 1e6f,
            velocity = Vector3.zero,
            applyShapeToPosition = false,
        }, 1);
        if (!ps.isPlaying) ps.Play();
    }

    public static Gradient Degradado(Color inicio, Color fin, float alfaInicio, float alfaFin)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(inicio, 0f), new GradientColorKey(fin, 1f) },
            new[] { new GradientAlphaKey(alfaInicio, 0f), new GradientAlphaKey(alfaFin, 1f) });
        return g;
    }
}

using UnityEngine;

public static class MaterialHelper
{
    private static Shader s_CachedShader;

    public static Shader GetWorkingShader(bool unlit = false)
    {
        if (s_CachedShader != null && !unlit) return s_CachedShader;

        Shader s = null;

        // Try URP Shaders first (since the project uses URP)
        if (unlit)
        {
            s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
        }
        else
        {
            s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
        }

        // Try getting shader from any existing valid renderer in the scene
        if (s == null)
        {
            Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.shader != null)
                {
                    string name = r.sharedMaterial.shader.name;
                    if (!name.Contains("Error") && !name.Contains("InternalError") && !name.Contains("Hidden"))
                    {
                        s = r.sharedMaterial.shader;
                        break;
                    }
                }
            }
        }

        // Fallbacks for built-in pipeline
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Diffuse");
        if (s == null) s = Shader.Find("Mobile/Diffuse");
        if (s == null) s = Shader.Find("Sprites/Default");

        if (!unlit) s_CachedShader = s;
        return s;
    }

    public static Material CreateMaterial(Color color, float smoothness = 0.2f, bool transparent = false)
    {
        Shader shader = GetWorkingShader(transparent);
        Material mat = new Material(shader);

        // Set color for both URP (_BaseColor) and Standard/Legacy (_Color)
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }
        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }
        mat.color = color;

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothness);
        }

        if (transparent)
        {
            // Configure URP transparency
            mat.SetFloat("_Surface", 1); // 1 = Transparent in URP
            mat.SetFloat("_Blend", 0);   // Alpha blend
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        return mat;
    }

    public static Material CreateParticleMaterial(Color color, bool additive = false)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) s = GetWorkingShader(true);

        Material mat = new Material(s);

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        mat.color = color;

        // Ensure white texture is bound so particles never render pink or untextured
        if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null)
        {
            mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
        }
        if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") == null)
        {
            mat.SetTexture("_MainTex", Texture2D.whiteTexture);
        }

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // Transparent
        if (additive)
        {
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1); // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        else
        {
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");

        return mat;
    }

    public static void FixParticleSystem(ParticleSystem ps, Color? fallbackColor = null)
    {
        if (ps == null) return;
        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr == null) return;

        bool broken = false;
        Material mat = psr.sharedMaterial;
        if (mat == null || mat.shader == null)
        {
            broken = true;
        }
        else
        {
            string sName = mat.shader.name;
            if (sName.Contains("InternalError") || sName.Contains("Error") || sName.Contains("Hidden") ||
                (!sName.Contains("Universal Render Pipeline") && !sName.Contains("Particles/Standard Unlit") && !sName.Contains("Sprites/Default") && !sName.Contains("Unlit/Color")))
            {
                broken = true;
            }
        }

        if (broken)
        {
            Color col = fallbackColor ?? (mat != null && mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white);
            if (col == Color.clear || (col.r == 0 && col.g == 0 && col.b == 0 && col.a == 0)) col = fallbackColor ?? Color.white;
            psr.material = CreateParticleMaterial(col);
        }

        // Recursively fix child particle systems
        ParticleSystem[] children = ps.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != ps)
            {
                FixParticleSystem(children[i], fallbackColor);
            }
        }
    }

    public static void FixAllParticleSystemsInScene()
    {
        ParticleSystem[] allPS = Object.FindObjectsOfType<ParticleSystem>(true);
        for (int i = 0; i < allPS.Length; i++)
        {
            FixParticleSystem(allPS[i]);
        }
    }
}

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class FixProjectMaterials
{
    static FixProjectMaterials()
    {
        EditorApplication.delayCall += FixAll;
    }

    [MenuItem("Tanks/Fix All Materials for URP")]
    public static void FixAll()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        Shader urpParticles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpParticles == null) urpParticles = urpUnlit;
        if (urpParticles == null) urpParticles = Shader.Find("Sprites/Default");

        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        int fixedCount = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue; // Skip Packages
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string sName = mat.shader != null ? mat.shader.name : "";
            bool isParticle = path.ToLower().Contains("dust") || 
                              path.ToLower().Contains("smoke") || 
                              path.ToLower().Contains("explosion") ||
                              sName.ToLower().Contains("particle");

            if (sName.Contains("InternalErrorShader") || sName.Contains("Error") ||
                (!sName.StartsWith("Universal Render Pipeline/") && !sName.StartsWith("TextMeshPro/")))
            {
                Color preservedColor = Color.white;
                if (mat.HasProperty("_Color")) preservedColor = mat.GetColor("_Color");
                else if (mat.HasProperty("_BaseColor")) preservedColor = mat.GetColor("_BaseColor");

                Texture mainTex = mat.mainTexture;

                if (isParticle)
                {
                    mat.shader = urpParticles;
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // Transparent
                    if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);   // Alpha
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    if (path.ToLower().Contains("explosion"))
                    {
                        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1); // Additive for explosion
                    }
                }
                else
                {
                    mat.shader = urpLit != null ? urpLit : urpUnlit;
                }

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", preservedColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", preservedColor);
                if (mainTex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                }

                EditorUtility.SetDirty(mat);
                fixedCount++;
                Debug.Log($"[FixProjectMaterials] Fixed material {path} to shader {mat.shader.name}");
            }
        }

        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixProjectMaterials] Completed. Fixed {fixedCount} materials for URP.");
        }
    }
}

using UnityEditor;
using UnityEngine;
using System.Linq;

public class MaterialDowngrade : EditorWindow
{
    [MenuItem("Tools/Materials/Downgrade URP-HDRP to Standard")]
    static void ConvertAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int changed = 0;

        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string shName = mat.shader != null ? mat.shader.name : "";
            // URP/HDRP/Shader Graph 계열만 타겟
            if (!(shName.Contains("Universal") || shName.Contains("HDRP") || shName.Contains("Shader Graph") || shName.EndsWith("/Lit")))
                continue;

            // 텍스처 임시 보관
            Texture albedo = GetTex(mat, "_BaseMap", "_BaseColorMap", "_MainTex");
            Texture metallicGloss = GetTex(mat, "_MetallicGlossMap", "_MaskMap");
            Texture normal = GetTex(mat, "_NormalMap", "_BumpMap");
            Texture emission = GetTex(mat, "_EmissionMap");

            Color baseColor = GetColor(mat, "_BaseColor", "_Color");
            float metallic = GetFloat(mat, "_Metallic", "_Metalness");
            float smoothness = GetFloat(mat, "_Smoothness", "_Glossiness", "_Gloss");

            // 표준 셰이더로 변경
            mat.shader = Shader.Find("Standard");

            // Albedo
            if (albedo) mat.SetTexture("_MainTex", albedo);
            mat.SetColor("_Color", baseColor == default ? Color.white : baseColor);

            // Metallic/Smoothness
            if (metallicGloss)
            {
                mat.SetTexture("_MetallicGlossMap", metallicGloss);
                mat.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                mat.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            }
            mat.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));

            // Normal Map
            if (normal)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Emission
            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", emission);
                // 필요하면 강도 기본값
                mat.SetColor("_EmissionColor", Color.black); 
            }

            EditorUtility.SetDirty(mat);
            changed++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MaterialDowngrade] Converted: {changed} materials to Standard.");
    }

    static Texture GetTex(Material m, params string[] keys)
    {
        foreach (var k in keys) if (m.HasProperty(k)) { var t = m.GetTexture(k); if (t) return t; }
        return null;
    }
    static float GetFloat(Material m, params string[] keys)
    {
        foreach (var k in keys) if (m.HasProperty(k)) return m.GetFloat(k);
        return 0f;
    }
    static Color GetColor(Material m, params string[] keys)
    {
        foreach (var k in keys) if (m.HasProperty(k)) return m.GetColor(k);
        return Color.white;
    }
}

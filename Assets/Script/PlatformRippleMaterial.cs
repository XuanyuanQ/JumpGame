using UnityEngine;

public static class PlatformRippleMaterial
{
    private const string ShaderName = "Custom/PlatformRippleURP";
    private const string ResourceShaderName = "PlatformRippleShader";
    private static Shader platformShader;

    public static void Prepare(GameObject platform, Color baseColor)
    {
        Renderer renderer = platform.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        Shader shader = GetShader();
        if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        material.SetColor("_Color", baseColor);
        material.SetColor("_RippleColor", Color.white);
        material.SetFloat("_RippleStartTime", -100f);
        material.SetFloat("_RippleDuration", 0.85f);
        ApplySizeProperties(platform, material);
    }

    public static void Trigger(GameObject platform, Vector3 hitPoint)
    {
        Renderer renderer = platform.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        Shader shader = GetShader();
        if (shader != null && material.shader != shader)
        {
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            Prepare(platform, color);
            material = renderer.material;
        }

        Vector3 localHit = platform.transform.InverseTransformPoint(hitPoint);
        material.SetVector("_RippleCenterOS", new Vector4(localHit.x, localHit.y, localHit.z, 0f));
        material.SetFloat("_RippleStartTime", Time.time);
        ApplySizeProperties(platform, material);
    }

    private static void ApplySizeProperties(GameObject platform, Material material)
    {
        float radius = GetLocalSurfaceRadius(platform);
        float width = Mathf.Clamp(radius * 0.22f, 0.055f, 0.18f);
        float strength = Mathf.Lerp(0.78f, 0.55f, Mathf.InverseLerp(0.25f, 0.55f, radius));

        material.SetFloat("_RippleMaxRadius", radius);
        material.SetFloat("_RippleWidth", width);
        material.SetFloat("_RippleStrength", strength);
    }

    private static float GetLocalSurfaceRadius(GameObject platform)
    {
        MeshFilter meshFilter = platform.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return 0.55f;
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;
        float meshRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        float scaleRadius = Mathf.Min(Mathf.Abs(platform.transform.localScale.x), Mathf.Abs(platform.transform.localScale.z)) * 0.5f;
        float normalizedRadius = meshRadius > 0.001f ? meshRadius * 0.9f : 0.45f;

        // Keep object-space shader values proportional while still reacting to very small or large generated platforms.
        return Mathf.Clamp(normalizedRadius * Mathf.Lerp(0.82f, 1.08f, Mathf.InverseLerp(1.5f, 4f, scaleRadius)), 0.22f, 0.58f);
    }

    private static Shader GetShader()
    {
        if (platformShader != null)
        {
            return platformShader;
        }

        platformShader = Resources.Load<Shader>(ResourceShaderName);
        if (platformShader == null)
        {
            platformShader = Shader.Find(ShaderName);
        }

        if (platformShader == null)
        {
            Debug.LogWarning($"{ShaderName} was not found in Resources or by Shader.Find. Platform ripple shader effect is disabled.");
        }

        return platformShader;
    }
}

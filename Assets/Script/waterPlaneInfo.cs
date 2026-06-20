using UnityEngine;

public class waterPlaneInfo : MonoBehaviour
{
    public Material waterMaterial;

    private readonly Vector4[] emptyRipplePoints = new Vector4[10];

    public void Start()
    {
        if (waterMaterial == null)
        {
            return;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            return;
        }

        Bounds bounds = meshRenderer.bounds;
        waterMaterial.SetVector("_WaterMin", bounds.min);
        waterMaterial.SetVector("_WaterMax", bounds.max);
        waterMaterial.SetVectorArray("_InputCentre", emptyRipplePoints);
    }
}

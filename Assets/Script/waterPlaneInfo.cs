using UnityEngine;

public class waterPlaneInfo : MonoBehaviour
{
    public Material waterMaterial; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        MeshRenderer meshRenderer = transform.GetComponent<MeshRenderer>();
    // 获取物体的世界空间包围盒
        Bounds bounds = meshRenderer.bounds;

        waterMaterial.SetVector("_WaterMin", bounds.min); // 最小点坐标
        waterMaterial.SetVector("_WaterMax", bounds.max); // 最大点坐标 
        }

    // Update is called once per frame
    void Update()
    {
        
    }
}

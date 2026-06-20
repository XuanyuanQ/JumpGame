using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    public GameObject boxPrefab;
    public float minDistance = 10;
    public float maxDistance = 15;
    public float cylinderChance = 0.5f;
    public GameObject player;

    private bool isFoward = true;
    private Vector3 curBoxPos;
    private static Mesh cylinderMesh;

    private void Start()
    {
        minDistance = 10;
        maxDistance = 15;
    }

    public bool isFowardFunc()
    {
        return isFoward;
    }

    public Vector3 getCurBoxPos()
    {
        return curBoxPos;
    }

    public Vector3 SpawnNewBox()
    {
        int direction = Random.Range(0, 2);
        Vector3 spawnDir = direction == 0 ? transform.forward : -transform.right;
        isFoward = direction == 0;

        float distance = Random.Range(minDistance, maxDistance);
        Vector3 newPos = new Vector3(player.transform.position.x, 10.3f, player.transform.position.z) + spawnDir * distance;

        GameObject platform = Instantiate(boxPrefab, newPos, Quaternion.identity);
        platform.name = Random.value < cylinderChance ? "CylinderPlatform" : "BoxPlatform";

        Renderer renderer = platform.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Random.ColorHSV(
                0f, 1f,
                0.75f, 1f,
                0.85f, 1f
            );
        }

        float randomWidth = Random.Range(3f, 8f);
        float fixedHeight = 3.1f;

        if (platform.name == "CylinderPlatform")
        {
            MakeCylinderPlatform(platform);
            platform.transform.localScale = new Vector3(randomWidth, fixedHeight * 0.5f, randomWidth);
        }
        else
        {
            platform.transform.localScale = new Vector3(randomWidth, fixedHeight, randomWidth);
        }

        curBoxPos = newPos;
        return newPos;
    }

    private void MakeCylinderPlatform(GameObject platform)
    {
        Mesh cylinder = GetCylinderMesh();

        MeshFilter meshFilter = platform.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = cylinder;
        }

        BoxCollider boxCollider = platform.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Destroy(boxCollider);
        }

        MeshCollider meshCollider = platform.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = platform.AddComponent<MeshCollider>();
        }

        meshCollider.sharedMesh = cylinder;
        meshCollider.convex = false;
    }

    private static Mesh GetCylinderMesh()
    {
        if (cylinderMesh != null)
        {
            return cylinderMesh;
        }

        GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinderMesh = tempCylinder.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tempCylinder);
        return cylinderMesh;
    }
}

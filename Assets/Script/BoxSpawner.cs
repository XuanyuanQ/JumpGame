using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    public GameObject boxPrefab;    // 拖入你的 Box 预制体
    private Vector3 lastBoxPos;     // 记录上一个方块的位置
    public float minDistance = 10;  // 最小间距
    public float maxDistance = 15;  // 最大间距

    private bool isFoward = true;
    public GameObject player;
    private Vector3 curBoxPos;

    void Start()
    {
        // 游戏开始时，假设第一个方块在原点
        lastBoxPos = new Vector3(878.9f, 10.5f, 652.4f);
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
    
    // 提供给判定成功后调用的方法
    public Vector3 SpawnNewBox()
    {
        // 1. 随机决定方向：0 是向前，1 是向右
        int direction = Random.Range(0, 2);
        // direction = 1;
        Vector3 spawnDir = (direction == 0) ? transform.forward : -transform.right;
        if (direction == 0)
        {
            isFoward = true;
        }
        else
        {
            isFoward = false;
        }

        // 2. 随机决定距离
        float distance = Random.Range(minDistance, maxDistance);

        // 3. 计算新方块的位置
        
        Vector3 newPos = new Vector3(player.transform.position.x,10.3f,player.transform.position.z) + spawnDir * distance;
        // newPos = lastBoxPos + spawnDir * distance;
        
        // 4. 生成方块
        GameObject newBox = Instantiate(boxPrefab, newPos, Quaternion.identity);

        // 5. 更新“上一个方块”的位置，为下一次生成做准备
        lastBoxPos = newPos;
        // 可选：给方块一个随机的颜色或缩放，增加趣味性
        newBox.GetComponent<Renderer>().material.color = Random.ColorHSV();
        float randomWidth = Random.Range(3, 8.0f); // X方向宽度
        // float randomDepth = Random.Range(0.8f, 2.5f); // Z方向深度
        float fixedHeight = 3.1f;                    // Y方向高度保持固定
        newBox.transform.localScale = new Vector3(randomWidth, fixedHeight, randomWidth);
        curBoxPos = newPos;
        return newPos;
    }
}
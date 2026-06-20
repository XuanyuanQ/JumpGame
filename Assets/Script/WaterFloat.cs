using UnityEngine;

public class WaterFloat : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Vector3 initialPosition;
    // public GameObject waterPlane;
    private Rigidbody rb;
    public Material waterMaterial; // 拖入大水面的材质
    private Vector4[] ripplePoints = new Vector4[10];
    private int rippleIndex = 0;
    private Vector2 _oldInputCentre;

    void Start() {
    initialPosition = transform.position;
    rb = GetComponent<Rigidbody>();
    rb.isKinematic = true; // 必须是 Kinematic 才能由 MovePosition 控制
}

void FixedUpdate() {
    // 1. 使用 Time.time 才能让波浪“流”起来
    float t = Time.time;

    float offsetX = 0;
    float offsetZ = 0;
    float totalHeight = 0;

    // --- 第一层波浪 (大波浪) ---
    float A1 = 0.4f;   // 振幅
    float F1 = 0.2f;   // 频率
    float P1 = 1.5f;   // 相位速度
    Vector2 D1 = new Vector2(-1.0f, 0.0f).normalized;
    
    float angle1 = (D1.x * initialPosition.x + D1.y * initialPosition.z) * F1 + t * P1;
    
    totalHeight += A1 * Mathf.Sin(angle1); // 简化公式，Physics 不需要太复杂的 Pow
    offsetX += D1.x * A1 * 0.3f * Mathf.Cos(angle1); // 0.3 是陡度

    // --- 第二层波浪 (微小扰动 - 使用固定值代替 Random) ---
    float A2 = 0.15f;
    float F2 = 0.8f;
    float P2 = 2.5f;
    Vector2 D2 = new Vector2(0.7f, 0.7f).normalized;

    float angle2 = (D2.x * initialPosition.x + D2.y * initialPosition.z) * F2 + t * P2;

    totalHeight += A2 * Mathf.Sin(angle2);
    offsetX += D2.x * A2 * 0.2f * Mathf.Cos(angle2);
    offsetZ += D2.y * A2 * 0.2f * Mathf.Cos(angle2);

    // --- 3. 计算并应用 ---
    Vector3 targetPos = new Vector3(
        initialPosition.x + offsetX,
        initialPosition.y + totalHeight,
        initialPosition.z + offsetZ
    );

    // MovePosition 是物理引擎最喜欢的更新方式
    rb.MovePosition(targetPos);
}
}

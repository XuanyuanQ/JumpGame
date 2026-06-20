using UnityEngine;
using System.Collections;

public class CameraUpdate : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 5, -7);
    public float smoothTime = 0.3f;
    public float rotationSpeed = 5.0f; // 控制旋转平滑的速度
    
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 m_midPoint;
    private bool m_forward = true;
    private Coroutine currentRoutine;

    // 提供一个公开方法给别人调用
    public void UpdateTarget(Vector3 playerPos, Vector3 newPos, bool forward)
    {
        m_forward = forward;
        m_midPoint = (playerPos + newPos) * 0.5f;

        // --- 1. 完全保留你的位置计算逻辑，但存入 targetPosition ---
        // 注意：计算 viewDirection 时使用当前的 forward/right 作为参考系基准
        Vector3 viewDirection = -transform.forward.normalized;
        if (!forward)
        {
            viewDirection = -transform.right.normalized;
        }

        float distance = 5.0f; 
        Vector3 basePos = m_midPoint + (viewDirection * distance) + offset;

        if (!forward)
        {
            // 侧面逻辑：应用你调试好的绝对偏移
            targetPosition = new Vector3(basePos.x - 7.0f, basePos.y + 4.0f, basePos.z + 23.0f);
        }
        else
        {
            targetPosition = basePos;
        }

        // --- 2. 计算目标旋转 targetRotation ---
        // 先计算“看向中点”的基础旋转
        Vector3 directionToMid = m_midPoint - targetPosition;
        if (directionToMid != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(directionToMid);
            
            if (!forward)
            {
                // 如果是侧面，应用你调试好的固定 Y 轴角度
                targetRotation = Quaternion.Euler(targetRotation.eulerAngles.x, -179.7f, targetRotation.eulerAngles.z);
            }
        }

        // --- 3. 启动平滑协程 ---
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(MoveRoutine());
    }

IEnumerator MoveRoutine()
    {
        // 既然旋转是瞬间的，循环判断条件只需要关注“位置”是否到位即可
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            // 1. 位置保持平滑（使用 SmoothDamp）
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            
            // 2. 旋转直接同步（不使用 Slerp 插值，直接赋予最终目标值）
            transform.rotation = targetRotation;
            
            yield return null;
        }

        // 最后硬对齐，确保精度
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
}
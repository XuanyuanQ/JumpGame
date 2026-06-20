using UnityEngine;
using System.Collections;

public class CameraUpdate : MonoBehaviour
{
    public float transitionDuration = 1.0f;
    public float followDistance = 15.0f;
    public float followHeight = 11.0f;
    public float lookHeight = 1.8f;
    public float diagonalAngle = 35.0f;
    public Vector3 compositionOffset = Vector3.zero;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Coroutine currentRoutine;

    public void UpdateTarget(Vector3 playerPos, Vector3 newPos, bool forward)
    {
        Vector3 flatPlayerPos = new Vector3(playerPos.x, 0f, playerPos.z);
        Vector3 flatNewPos = new Vector3(newPos.x, 0f, newPos.z);
        Vector3 jumpDirection = flatNewPos - flatPlayerPos;

        if (jumpDirection.sqrMagnitude < 0.001f)
        {
            jumpDirection = forward ? Vector3.forward : Vector3.left;
        }

        jumpDirection.Normalize();

        Vector3 midPoint = (playerPos + newPos) * 0.5f;
        Vector3 cameraDirection = Quaternion.AngleAxis(diagonalAngle, Vector3.up) * -jumpDirection;
        Vector3 lookPoint = midPoint + Vector3.up * lookHeight;

        targetPosition = midPoint + cameraDirection.normalized * followDistance + Vector3.up * followHeight + compositionOffset;
        targetRotation = Quaternion.LookRotation(lookPoint - targetPosition, Vector3.up);

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float duration = Mathf.Max(0.05f, transitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        currentRoutine = null;
    }
}

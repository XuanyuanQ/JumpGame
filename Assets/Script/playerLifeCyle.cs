using UnityEngine;
using System.Collections.Generic;

public class playerLifeCyle : MonoBehaviour
{
    public GameObject currentPlatform;
    public PlayerMoveControl parent;
    public Material waterMaterial;

    private readonly Vector4[] ripplePoints = new Vector4[10];
    private int rippleIndex = 0;
    private Vector3 cur_hit;
    private readonly HashSet<Collider> currentColliders = new HashSet<Collider>();

    private void AddRipple(Vector3 worldPosition, float strength = 1.0f)
    {
        if (waterMaterial == null)
        {
            return;
        }

        ripplePoints[rippleIndex] = new Vector4(worldPosition.x, worldPosition.z, Time.time, strength);
        rippleIndex = (rippleIndex + 1) % ripplePoints.Length;

        ripplePoints[rippleIndex] = new Vector4(worldPosition.x, worldPosition.z, Time.time + 0.08f, strength * 0.45f);
        rippleIndex = (rippleIndex + 1) % ripplePoints.Length;

        waterMaterial.SetVectorArray("_InputCentre", ripplePoints);
        waterMaterial.SetVector("_ImpactPos", worldPosition);
        waterMaterial.SetFloat("_ImpactTime", Time.time);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        Vector3 pos = contact.point;

        if (collision.gameObject.CompareTag("Water"))
        {
            Debug.Log("Fell into water");
            AddRipple(pos, 1.35f);
            parent.updateResult(0, pos);
        }
        else if (collision.gameObject.CompareTag("ground"))
        {
            currentColliders.Add(collision.collider);
            AddRipple(pos, 1.0f);

            if (currentColliders.Count >= 2)
            {
                parent.updateResult(0, pos);
            }
            else if (collision.gameObject != currentPlatform)
            {
                currentPlatform = collision.gameObject;
                cur_hit = pos;
                Invoke(nameof(DelayedUpdate), 0.15f);
            }
        }
        else if (collision.gameObject.CompareTag("success"))
        {
            Debug.Log("Reached shore");
            parent.updateResult(2, pos);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            currentColliders.Remove(collision.collider);
        }
    }

    private void DelayedUpdate()
    {
        parent.updateResult(1, cur_hit);
        Debug.Log("Landed, spawning next platform");
    }
}

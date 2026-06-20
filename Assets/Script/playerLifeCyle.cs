using UnityEngine;
using System.Collections.Generic;

public class playerLifeCyle : MonoBehaviour
{

    public GameObject currentPlatform;
    public PlayerMoveControl parent;

    public Material waterMaterial; // 拖入大水面的材质
    private Vector4[] ripplePoints = new Vector4[10];
    private int rippleIndex = 0;
    private Vector2 _oldInputCentre;
    Vector3 cur_hit;
    private HashSet<Collider> currentColliders = new HashSet<Collider>();
    // 只要脚本挂在有刚体的物体上，系统会自动调用这里
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Water"))
        {
            ripplePoints[rippleIndex] = new Vector4(transform.position.x, transform.position.z, Time.time, 0);
            rippleIndex = (rippleIndex + 1) % ripplePoints.Length;
            _oldInputCentre = transform.position;

            //Set ripple centre (ray hit point) to ripple material
            waterMaterial.SetVectorArray("_InputCentre", ripplePoints);
            waterMaterial.SetVector("_ImpactPos", transform.position);
            waterMaterial.SetFloat("_ImpactTime", Time.time);


            Debug.Log("掉下去了！");
            ContactPoint contact = collision.contacts[0];
            Vector3 pos = contact.point;
            parent.updateResult(0, pos);
        }
        else if (collision.gameObject.CompareTag("ground"))

        {

            currentColliders.Add(collision.collider);
            ripplePoints[rippleIndex] = new Vector4(transform.position.x, transform.position.z, Time.time, 0);
            rippleIndex = (rippleIndex + 1) % ripplePoints.Length;
            _oldInputCentre = transform.position;

            //Set ripple centre (ray hit point) to ripple material
            waterMaterial.SetVectorArray("_InputCentre", ripplePoints);
            waterMaterial.SetVector("_ImpactPos", transform.position);
            waterMaterial.SetFloat("_ImpactTime", Time.time);

            ContactPoint contact = collision.contacts[0];
            Vector3 pos = contact.point;
            if(currentColliders.Count >= 2)
            {
                parent.updateResult(0, pos);
            } else  if (collision.gameObject != currentPlatform)
            {
                currentPlatform = collision.gameObject;
                cur_hit = pos;
                Invoke("DelayedUpdate", 0.15f);

                // parent.updateResult(1);
            }
        }
        else if (collision.gameObject.CompareTag("success"))
        {
            Debug.Log("上岸了！");
            ContactPoint contact = collision.contacts[0];
            Vector3 pos = contact.point;

            parent.updateResult(2, pos);
        }

    }
    
    void OnCollisionExit(Collision collision)
    {
        // 移除离开的物体
       if (collision.gameObject.CompareTag("ground"))
        {
            currentColliders.Remove(collision.collider);
        }
 
    }

    // void CheckCount()
    // {
    //     if (currentColliders.Count >= 2)
    //     {
    //         Debug.Log("满足条件！当前同时与 " + currentColliders.Count + " 个物体接触");
    //     }
    // }

    void DelayedUpdate()
    {
        // 这个时候小人应该已经站稳了，再生成新方块
        parent.updateResult(1,cur_hit);
        Debug.Log("站稳了，生成下一个！");
    }

    // void OnCollisionExit(Collision collision)
    // {

   
    // }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

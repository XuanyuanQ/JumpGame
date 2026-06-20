using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
 using UnityEngine.SceneManagement; // 必须引入命名空间

public class PlayerMoveControl : MonoBehaviour
{
    public ParticleSystem chargeParticle; // 拖入你的粒子系统
    private ParticleSystem.EmissionModule emissionLoop;

    public GameObject player;
    public GameObject head;
    public float flipDuration = 1f; // 旋转速度
    public float jumpSpeed = 3.0f; // 跳跃距离
    private bool isCharging = false;
    private float startTime;     // 记录按下的时刻
    private float chargeTime;    // 最终按下的时长（蓄力值）
    public CameraUpdate camScript;
    public BoxSpawner boxSpawner;
    public waterPlaneInfo waterPlaneInfo;
    private Material playerMaterial;
    public float maxChargeTime = 2.0f;
    private float head_y = 0;
    public TextMeshProUGUI scoreText;
    private int score = 0;
    private int maxScore = 0;
    private bool ongoing = true;

    void Start()
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        // 将重心向下移动（相对于物体中心）
        rb.centerOfMass = new Vector3(0, -1f, 0);
        // rb.constraints = RigidbodyConstraints.None;
        rb.isKinematic = false;
        playerMaterial = player.GetComponent<Renderer>().material;
        emissionLoop = chargeParticle.emission;
        emissionLoop.enabled = false; // 初始状态关闭
        scoreText.color = new Color(1f, 0.8f, 0f, 1f);
        scoreText.text="Score:0";
    }
    public Vector3 getPlaerPosition()
        {
        return player.transform.position;
        }
    void Update()
    {
        if (Keyboard.current.yKey.wasReleasedThisFrame & !ongoing)
        {
            Debug.Log("新系统：Y 键被按下");
            ResetGame();
            Start();
            waterPlaneInfo.Start();
            ongoing = true;
        }
        else if(!ongoing)
        {
            return;
        }
    
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("空格按下");
            startTime = Time.time; // 记下当前游戏跑了多少秒
            isCharging = true;
            head_y = head.transform.localPosition.y;
            Debug.Log("11head_y:"+head_y);

            Vector3 ParticlePos = chargeParticle.transform.position;
            Vector3 Particleoffset = new Vector3(0.08f, -0.2f, 1f);
            ParticlePos = player.transform.position+ Particleoffset;
            chargeParticle.transform.position = ParticlePos;
            
            var mainModule = chargeParticle.main;
            mainModule.startColor = Random.ColorHSV();
            emissionLoop.rateOverTime = 5;
            emissionLoop.enabled = true;
            chargeParticle.Play();
        }

        // 新版检测“持续按住”
        if (Keyboard.current.spaceKey.isPressed)
        {
            // 蓄力中...
            float chargePercent = (Time.time - startTime) / maxChargeTime;
            chargePercent = Mathf.Clamp01(chargePercent);

            // 关键：将值传给 Shader
            Vector3 currentPos = head.transform.localPosition;
            float targetY = head_y - (head_y * chargePercent * 0.1f);
            // 3. 赋值回去
            currentPos.y = targetY;
            head.transform.localPosition = currentPos;
            playerMaterial.SetFloat("_ChargeAmount", chargePercent);
        }

        // 新版检测“松开”瞬间
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            Debug.Log("空格松开");
            if (!isCharging)
            {
                return;
            }
            
            chargeTime = Time.time - startTime;
            isCharging = false;
            playerMaterial.SetFloat("_ChargeAmount", 0);
            Vector3 currentPos = head.transform.localPosition;
            Debug.Log("head_y:"+head_y);
            currentPos.y = head_y;
            head.transform.localPosition = currentPos;
             // 或者直接停止产生新粒子，允许现有粒子自然消失
            emissionLoop.rateOverTime = 0;
            emissionLoop.enabled = false;
            chargeParticle.Stop();

            // 触发翻转动作
            StartFlip();
        }
    }
    // 这是一个中转站，用来开启协程
    public void StartFlip()
    {
        // 开启协程，并传入你希望它转多久
        StartCoroutine(RotateOverTime(flipDuration));
        StartCoroutine(CheckResultRoutine(flipDuration));
    }


    // 真正的旋转逻辑，在后台异步运行
    IEnumerator RotateOverTime(float duration)
    {
        Vector3 startPos = player.transform.position;
        float jumpDistance = chargeTime * jumpSpeed;
        Vector3 newpos = boxSpawner.getCurBoxPos();
        Vector3 spawnDir = player.transform.forward;
        if (!boxSpawner.isFowardFunc())
        {
            Debug.Log("not FowardFunc");
            spawnDir = -player.transform.right;
        }
        if (score > 0)
        {
            spawnDir = (new Vector3(newpos.x,player.transform.position.y,newpos.z) - player.transform.position).normalized;
        }
        
        Vector3 endPos = startPos + spawnDir* jumpDistance; // 向前移动

        Quaternion startRotation = player.transform.rotation;
        // 目标是当前角度基础上绕X轴转180
        Quaternion middleRotation = startRotation * Quaternion.Euler(180, 0, 0);
        Quaternion endRotation = startRotation * Quaternion.Euler(360, 0, 0);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 随着时间推移，平滑改变角度
            // Debug.Log("RotateOverTime");
            float t = elapsed / duration;

            if (elapsed < duration / 2)
            {
                float ratio = elapsed / (duration / 2f);
                player.transform.rotation = Quaternion.Lerp(startRotation, middleRotation, ratio);
            }
            else
            {
                float ratio = (elapsed - duration / 2f) / (duration / 2f);
                player.transform.rotation = Quaternion.Lerp(middleRotation, endRotation, ratio);
            }
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            // 加上 Y 轴的高度曲线（让它看起来像在“跳”）
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 6.0f;
            player.transform.position = currentPos;

            elapsed += Time.deltaTime;
            // Debug.Log("elapsed: " + elapsed);
            yield return null; // 告诉 Unity：这一帧我转够了，下一帧再继续
        }
        player.transform.position = endPos;
        player.transform.rotation = endRotation; // 确保最后角度绝对精准
        Debug.Log("Rotate end");
    }
    IEnumerator CheckResultRoutine(float duration)
    {
        // 1. 等待跳跃动作完成（比如 0.8 秒）
        yield return new WaitForSeconds(duration);

        // 2. 等待物理引擎静止（或者检测速度是否接近 0）
        Rigidbody rb = player.GetComponent<Rigidbody>();
        yield return new WaitUntil(() => rb.linearVelocity.magnitude < 0.1f);
    }
    public void updateResult(int val,Vector3 pos)
    {
        if (val == 2)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            // rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ| RigidbodyConstraints.FreezeRotationY;
        
            Debug.Log("wing");
            scoreText.color = new Color(0f, 0.8f, 0f, 1f);
            scoreText.text = "you success";
            player.transform.position = pos;
            scoreText.text = $"you success!!\n your score is 30 \npress Y restart";
            ongoing = false;
        }
        else if (val == 1)
        {
            Debug.Log("ScoreUp");
            scoreText.color = new Color(1f, 0.8f, 0f, 1f);
            Vector3 newPos = boxSpawner.SpawnNewBox();
            camScript.UpdateTarget(player.transform.position, newPos, boxSpawner.isFowardFunc());
            score++;
            scoreText.text = $"Score: {score}";
            // player.transform.position = pos;
        }
        else if (val == 0)
        {
            ongoing = false;
            if (maxScore < score)
            {
                maxScore = score;
            }
            // player.transform.position = pos;
            scoreText.color = new Color(0.8f, 0f, 0f, 1f);
            scoreText.text = $"GameOver!!\n your score is {maxScore} \npress Y restart";
            Debug.Log("GameOver");
        }
    }
   

        public void ResetGame()
        {
            // 获取当前活动的场景名称并重新加载它
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
            
        }
    
    }

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMoveControl : MonoBehaviour
{
    public ParticleSystem chargeParticle;
    public GameObject player;
    public GameObject head;
    public float flipDuration = 1f;
    public float jumpSpeed = 3.0f;
    public CameraUpdate camScript;
    public BoxSpawner boxSpawner;
    public waterPlaneInfo waterPlaneInfo;
    public float maxChargeTime = 2.0f;
    public TextMeshProUGUI scoreText;

    private Image hudPanel;
    private TextMeshProUGUI detailText;
    private ParticleSystem.EmissionModule emissionLoop;
    private bool isCharging = false;
    private float startTime;
    private float chargeTime;
    private Material playerMaterial;
    private float head_y = 0;
    private int score = 0;
    private int maxScore = 0;
    private bool ongoing = true;

    private const string RuleText =
        "Hold Space to charge, release to jump.\nLand on the next platform. Press Y to restart after game over.";

    private void Start()
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -1f, 0);
        rb.isKinematic = false;

        playerMaterial = player.GetComponent<Renderer>().material;
        emissionLoop = chargeParticle.emission;
        emissionLoop.enabled = false;

        ShowPlayingText();
    }

    private void ResolveHud()
    {
        if (scoreText != null && detailText != null && hudPanel != null)
        {
            return;
        }

        CreateHud();
    }

    private void CreateHud()
    {
        GameObject canvasObject = new GameObject("Game UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("HUD Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        hudPanel = panelObject.AddComponent<Image>();
        hudPanel.color = new Color(0.02f, 0.025f, 0.035f, 0.82f);
        hudPanel.raycastTarget = false;

        RectTransform panelRect = hudPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(14f, -12f);
        panelRect.sizeDelta = new Vector2(340f, 116f);

        GameObject textObject = new GameObject("Score Text");
        textObject.transform.SetParent(panelObject.transform, false);

        scoreText = textObject.AddComponent<TextMeshProUGUI>();
        scoreText.fontSize = 30;
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.enableWordWrapping = false;
        scoreText.alignment = TextAlignmentOptions.TopLeft;
        scoreText.raycastTarget = false;

        Shadow scoreShadow = textObject.AddComponent<Shadow>();
        scoreShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        scoreShadow.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(1f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(16f, -12f);
        scoreRect.sizeDelta = new Vector2(-32f, 36f);

        GameObject detailObject = new GameObject("Hint Text");
        detailObject.transform.SetParent(panelObject.transform, false);

        detailText = detailObject.AddComponent<TextMeshProUGUI>();
        detailText.fontSize = 13;
        detailText.enableWordWrapping = true;
        detailText.alignment = TextAlignmentOptions.TopLeft;
        detailText.raycastTarget = false;

        RectTransform detailRect = detailText.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0f, 0f);
        detailRect.anchorMax = new Vector2(1f, 1f);
        detailRect.pivot = new Vector2(0f, 1f);
        detailRect.anchoredPosition = new Vector2(16f, -52f);
        detailRect.sizeDelta = new Vector2(-32f, -60f);
    }

    private void SetHudText(string scoreLine, string detailLine, Color accentColor, Color panelColor)
    {
        ResolveHud();
        hudPanel.color = panelColor;
        scoreText.color = accentColor;
        scoreText.text = scoreLine;
        detailText.color = new Color(1f, 1f, 1f, 0.72f);
        detailText.text = detailLine;
    }

    private void ShowPlayingText()
    {
        SetHudText($"Score {score}", RuleText, new Color(1f, 0.82f, 0.16f, 1f), new Color(0.02f, 0.025f, 0.035f, 0.82f));
    }

    public Vector3 getPlaerPosition()
    {
        return player.transform.position;
    }

    private void Update()
    {
        if (Keyboard.current.yKey.wasReleasedThisFrame && !ongoing)
        {
            ResetGame();
            return;
        }

        if (!ongoing)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            startTime = Time.time;
            isCharging = true;
            head_y = head.transform.localPosition.y;

            Vector3 particleOffset = new Vector3(0.08f, -0.2f, 1f);
            chargeParticle.transform.position = player.transform.position + particleOffset;

            var mainModule = chargeParticle.main;
            mainModule.startColor = Random.ColorHSV();
            emissionLoop.rateOverTime = 5;
            emissionLoop.enabled = true;
            chargeParticle.Play();
        }

        if (Keyboard.current.spaceKey.isPressed)
        {
            float chargePercent = (Time.time - startTime) / maxChargeTime;
            chargePercent = Mathf.Clamp01(chargePercent);

            Vector3 currentPos = head.transform.localPosition;
            currentPos.y = head_y - head_y * chargePercent * 0.1f;
            head.transform.localPosition = currentPos;
            playerMaterial.SetFloat("_ChargeAmount", chargePercent);
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            if (!isCharging)
            {
                return;
            }

            chargeTime = Time.time - startTime;
            isCharging = false;
            playerMaterial.SetFloat("_ChargeAmount", 0);

            Vector3 currentPos = head.transform.localPosition;
            currentPos.y = head_y;
            head.transform.localPosition = currentPos;

            emissionLoop.rateOverTime = 0;
            emissionLoop.enabled = false;
            chargeParticle.Stop();

            StartFlip();
        }
    }

    public void StartFlip()
    {
        StartCoroutine(RotateOverTime(flipDuration));
        StartCoroutine(CheckResultRoutine(flipDuration));
    }

    private IEnumerator RotateOverTime(float duration)
    {
        Vector3 startPos = player.transform.position;
        float jumpDistance = chargeTime * jumpSpeed;
        Vector3 newpos = boxSpawner.getCurBoxPos();
        Vector3 spawnDir = player.transform.forward;

        if (!boxSpawner.isFowardFunc())
        {
            spawnDir = -player.transform.right;
        }

        if (score > 0)
        {
            spawnDir = (new Vector3(newpos.x, player.transform.position.y, newpos.z) - player.transform.position).normalized;
        }

        Vector3 endPos = startPos + spawnDir * jumpDistance;
        Quaternion startRotation = player.transform.rotation;
        Quaternion middleRotation = startRotation * Quaternion.Euler(180, 0, 0);
        Quaternion endRotation = startRotation * Quaternion.Euler(360, 0, 0);
        float elapsed = 0f;

        while (elapsed < duration)
        {
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
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 6.0f;
            player.transform.position = currentPos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.transform.position = endPos;
        player.transform.rotation = endRotation;
    }

    private IEnumerator CheckResultRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        yield return new WaitUntil(() => rb.linearVelocity.magnitude < 0.1f);
    }

    public void updateResult(int val, Vector3 pos)
    {
        if (val == 2)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            player.transform.position = pos;
            SetHudText("Success", $"Final score: {score}\nPress Y to restart.", new Color(0.25f, 1f, 0.45f, 1f), new Color(0.02f, 0.12f, 0.07f, 0.72f));
            ongoing = false;
        }
        else if (val == 1)
        {
            Vector3 newPos = boxSpawner.SpawnNewBox();
            camScript.UpdateTarget(player.transform.position, newPos, boxSpawner.isFowardFunc());
            score++;
            ShowPlayingText();
        }
        else if (val == 0)
        {
            ongoing = false;
            if (maxScore < score)
            {
                maxScore = score;
            }

            SetHudText("Game Over", $"Score: {score}\nBest: {maxScore}\nPress Y to restart.", new Color(1f, 0.22f, 0.14f, 1f), new Color(0.18f, 0.035f, 0.035f, 0.74f));
        }
    }

    public void ResetGame()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}

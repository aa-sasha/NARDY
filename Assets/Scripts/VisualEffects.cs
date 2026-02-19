using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Визуальные эффекты: CRT сканлинии, виньетка, хроматическая аберрация,
/// пульсация элементов, тряска камеры и неоновое свечение.
/// Добавляется на главный Canvas или отдельный объект в сцене.
/// </summary>
public class VisualEffects : MonoBehaviour
{
    // ========================= СИНГЛТОН =========================

    public static VisualEffects Instance { get; private set; }

    // ========================= НАСТРОЙКИ CRT =========================

    [Header("CRT Эффекты")]
    [SerializeField] private bool enableScanlines = true;
    [SerializeField] private float scanlineOpacity = 0.08f;       // Прозрачность сканлиний (0-1)
    [SerializeField] private float scanlineSpacing = 3f;          // Расстояние между линиями в пикселях

    [Header("Виньетка")]
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private float vignetteStrength = 0.6f;       // Сила виньетки

    [Header("Пульсация")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseFrequency = 0.3f;         // Частота пульсации (Гц)
    [SerializeField] private float pulseAmplitude = 0.015f;       // Амплитуда пульсации яркости

    [Header("Тряска камеры")]
    [SerializeField] private float shakeDecay = 5f;               // Скорость затухания тряски

    // ========================= ВНУТРЕННИЕ ПЕРЕМЕННЫЕ =========================

    private Canvas crtCanvas;
    private RawImage scanlineImage;
    private RawImage vignetteImage;
    private Texture2D scanlineTex;
    private Texture2D vignetteTex;

    private Camera mainCamera;
    private Vector3 cameraOriginalPos;
    private float shakeIntensity;

    private float pulseTime;

    // ========================= ИНИЦИАЛИЗАЦИЯ =========================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        mainCamera = Camera.main;
        if (mainCamera != null)
            cameraOriginalPos = mainCamera.transform.localPosition;
    }

    private void Start()
    {
        CreateCRTCanvas();
    }

    /// <summary>
    /// Создаёт Canvas с CRT-эффектами поверх всего UI.
    /// Сканлинии и виньетка — полноэкранные RawImage с процедурными текстурами.
    /// </summary>
    private void CreateCRTCanvas()
    {
        GameObject canvasObj = new GameObject("CRT_Canvas");
        DontDestroyOnLoad(canvasObj);

        crtCanvas = canvasObj.AddComponent<Canvas>();
        crtCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        crtCanvas.sortingOrder = 999; // Поверх всего

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>().blockingMask = 0; // Не блокируем клики

        // Сканлинии
        if (enableScanlines)
        {
            GameObject scanObj = new GameObject("Scanlines");
            scanObj.transform.SetParent(canvasObj.transform, false);
            scanlineImage = scanObj.AddComponent<RawImage>();
            scanlineImage.raycastTarget = false;

            RectTransform scanRect = scanObj.GetComponent<RectTransform>();
            scanRect.anchorMin = Vector2.zero;
            scanRect.anchorMax = Vector2.one;
            scanRect.offsetMin = Vector2.zero;
            scanRect.offsetMax = Vector2.zero;

            // Процедурная текстура сканлиний
            scanlineTex = CreateScanlineTexture(1, 64, scanlineOpacity, scanlineSpacing);
            scanlineImage.texture = scanlineTex;
            scanlineImage.uvRect = new Rect(0, 0, 1, Screen.height / 64f);
        }

        // Виньетка
        if (enableVignette)
        {
            GameObject vigObj = new GameObject("Vignette");
            vigObj.transform.SetParent(canvasObj.transform, false);
            vignetteImage = vigObj.AddComponent<RawImage>();
            vignetteImage.raycastTarget = false;

            RectTransform vigRect = vigObj.GetComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;

            // Процедурная текстура виньетки
            vignetteTex = CreateVignetteTexture(256, 256, vignetteStrength);
            vignetteImage.texture = vignetteTex;
        }
    }

    // ========================= UPDATE =========================

    private void Update()
    {
        // Пульсация яркости сканлиний
        if (enablePulse && scanlineImage != null)
        {
            pulseTime += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(pulseTime * pulseFrequency * Mathf.PI * 2f) * pulseAmplitude;
            Color c = scanlineImage.color;
            c.a = pulse;
            scanlineImage.color = c;
        }

        // Тряска камеры
        if (shakeIntensity > 0f && mainCamera != null)
        {
            shakeIntensity -= Time.deltaTime * shakeDecay;
            shakeIntensity = Mathf.Max(0f, shakeIntensity);

            Vector3 shake = new Vector3(
                Random.Range(-1f, 1f) * shakeIntensity,
                Random.Range(-1f, 1f) * shakeIntensity,
                0f
            );
            mainCamera.transform.localPosition = cameraOriginalPos + shake;
        }
        else if (mainCamera != null)
        {
            mainCamera.transform.localPosition = cameraOriginalPos;
        }
    }

    // ========================= ПУБЛИЧНЫЕ МЕТОДЫ =========================

    /// <summary>
    /// Запускает тряску камеры. Вызывать при важных событиях (победа, битьё фишки).
    /// </summary>
    /// <param name="intensity">Сила тряски (0.05 — лёгкая, 0.2 — сильная)</param>
    public void ShakeCamera(float intensity = 0.08f)
    {
        shakeIntensity = intensity;
    }

    /// <summary>
    /// Анимация "pop" — элемент выпрыгивает и возвращается.
    /// Использует AnimationCurve с перерегулированием (overshoot).
    /// </summary>
    public static IEnumerator PopAnimation(Transform target, float duration = 0.35f)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Кривая с перерегулированием: 1.0 → 1.25 → 0.92 → 1.0
            float scale;
            if (t < 0.4f)
                scale = 1f + (t / 0.4f) * 0.25f;           // Рост до 1.25
            else if (t < 0.7f)
                scale = 1.25f - ((t - 0.4f) / 0.3f) * 0.33f; // Падение до 0.92
            else
                scale = 0.92f + ((t - 0.7f) / 0.3f) * 0.08f; // Возврат к 1.0

            target.localScale = originalScale * scale;
            yield return null;
        }

        target.localScale = originalScale;
    }

    /// <summary>
    /// Анимация покачивания — медленное вращение на ±angle градусов.
    /// Запускается как бесконечная корутина.
    /// </summary>
    public static IEnumerator SwayAnimation(Transform target, float angle = 0.5f, float period = 6f)
    {
        if (target == null) yield break;

        Quaternion originalRot = target.localRotation;
        float time = Random.Range(0f, period); // Случайный сдвиг фазы

        while (true)
        {
            time += Time.deltaTime;
            float sway = Mathf.Sin(time / period * Mathf.PI * 2f) * angle;
            target.localRotation = originalRot * Quaternion.Euler(0, 0, sway);
            yield return null;
        }
    }

    // ========================= ГЕНЕРАЦИЯ ТЕКСТУР =========================

    /// <summary>
    /// Создаёт текстуру сканлиний: чередование прозрачных и полупрозрачных строк.
    /// </summary>
    private Texture2D CreateScanlineTexture(int width, int height, float opacity, float spacing)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            // Каждые spacing пикселей — тёмная линия
            float alpha = (y % Mathf.Max(1, (int)spacing) == 0) ? opacity : 0f;
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = new Color(0f, 0f, 0f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Создаёт текстуру виньетки: радиальный градиент от прозрачного центра к тёмным краям.
    /// </summary>
    private Texture2D CreateVignetteTexture(int width, int height, float strength)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2(width / 2f, height / 2f);
        float maxDist = Mathf.Sqrt(center.x * center.x + center.y * center.y);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // Квадратичное затемнение к краям
                float alpha = Mathf.Clamp01(dist * dist * strength);
                pixels[y * width + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}

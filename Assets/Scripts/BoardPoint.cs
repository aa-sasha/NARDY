using UnityEngine;

/// <summary>
/// Представляет один пункт (треугольник) на доске нард.
/// Поддерживает плавную подсветку доступных ходов с интерполяцией цвета.
/// Клик обрабатывается через BoardSetup (raycast), делегируется в GameManager.
/// </summary>
public class BoardPoint : MonoBehaviour
{
    [SerializeField] private int pointIndex; // 1-24

    [Header("Настройки подсветки")]
    [SerializeField] private float highlightSpeed = 8f;  // Скорость плавного перехода цвета

    // Визуальные компоненты
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isHighlighted;

    // Плавная подсветка — целевой и текущий цвета
    private Color targetColor;
    private bool isTransitioning;

    // Дочерний маркер подсветки (круглый индикатор поверх треугольника)
    private GameObject highlightMarker;
    private SpriteRenderer markerRenderer;

    // ========================= ПУБЛИЧНЫЕ СВОЙСТВА =========================

    public int PointIndex => pointIndex;
    public bool IsHighlighted => isHighlighted;

    // ========================= ИНИЦИАЛИЗАЦИЯ =========================

    /// <summary>
    /// Инициализирует пункт: устанавливает номер, кэширует SpriteRenderer и цвет.
    /// </summary>
    public void Initialize(int index)
    {
        pointIndex = index;
        gameObject.name = $"Point_{index}";
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // ========================= ОБРАБОТКА КЛИКА =========================

    /// <summary>
    /// Вызывается из BoardSetup при клике на этот пункт.
    /// Делегирует всю логику в GameManager.
    /// </summary>
    public void OnClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPointClicked(pointIndex);
    }

    // ========================= ПОДСВЕТКА =========================

    /// <summary>
    /// Включает/выключает подсветку пункта.
    /// Для треугольников подсветка реализована через смену цвета спрайта
    /// и дополнительный маркер-индикатор.
    /// </summary>
    /// <param name="on">Включить (true) или выключить (false) подсветку</param>
    /// <param name="highlightColor">Цвет подсветки; null = зелёный по умолчанию</param>
    public void SetHighlight(bool on, Color? highlightColor = null)
    {
        if (spriteRenderer == null) return;

        isHighlighted = on;

        if (on)
        {
            Color hColor = highlightColor ?? new Color(0.2f, 0.8f, 0.2f, 0.9f);

            // Смешиваем цвет подсветки с оригинальным для мягкого эффекта
            targetColor = Color.Lerp(originalColor, hColor, 0.5f);
            targetColor.a = 1f;
            isTransitioning = true;

            // Показываем маркер поверх треугольника
            ShowHighlightMarker(hColor);
        }
        else
        {
            targetColor = originalColor;
            isTransitioning = true;

            // Скрываем маркер
            HideHighlightMarker();
        }
    }

    /// <summary>
    /// Показывает круглый маркер подсветки поверх пункта.
    /// Маркер — мягкий полупрозрачный круг для индикации доступного хода.
    /// </summary>
    private void ShowHighlightMarker(Color color)
    {
        if (highlightMarker == null)
        {
            highlightMarker = new GameObject("HighlightMarker");
            highlightMarker.transform.SetParent(transform);
            highlightMarker.transform.localPosition = new Vector3(0f, 0.3f, -0.01f);
            highlightMarker.transform.localScale = Vector3.one * 0.6f;

            markerRenderer = highlightMarker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = TextureGenerator.CreateHighlightMarker(64, color);
            markerRenderer.sortingOrder = 5;
        }

        // Обновляем цвет маркера
        markerRenderer.sprite = TextureGenerator.CreateHighlightMarker(64, color);
        markerRenderer.color = Color.white;
        highlightMarker.SetActive(true);
    }

    /// <summary>
    /// Скрывает маркер подсветки.
    /// </summary>
    private void HideHighlightMarker()
    {
        if (highlightMarker != null)
            highlightMarker.SetActive(false);
    }

    // ========================= ПЛАВНЫЙ ПЕРЕХОД =========================

    /// <summary>
    /// Плавно интерполирует цвет спрайта к целевому.
    /// Создаёт эффект мягкого включения/выключения подсветки.
    /// </summary>
    private void Update()
    {
        if (isTransitioning && spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * highlightSpeed);

            // Прекращаем интерполяцию, когда цвет достаточно близок к целевому
            if (ColorDistance(spriteRenderer.color, targetColor) < 0.01f)
            {
                spriteRenderer.color = targetColor;
                isTransitioning = false;
            }
        }
    }

    /// <summary>
    /// Вычисляет расстояние между двумя цветами (для определения момента остановки интерполяции).
    /// </summary>
    private float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
    }
}

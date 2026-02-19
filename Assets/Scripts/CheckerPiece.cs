using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент фишки (белая/чёрная) в стиле Balatro.
/// Хранит цвет и текущую позицию на доске.
/// Отвечает за анимацию движения, неоновую подсветку выбора и эффект вспышки при битье.
/// </summary>
public class CheckerPiece : MonoBehaviour
{
    [SerializeField] private bool isWhite;
    [SerializeField] private int currentPoint; // 1-24, 0=бар, 25=снята

    [Header("Настройки анимации")]
    [SerializeField] private float moveDuration = 0.3f;      // Длительность перемещения (секунды)
    [SerializeField] private float flashDuration = 0.15f;    // Длительность вспышки при битье

    [Header("Настройки выделения (Balatro)")]
    [SerializeField] private float selectScale = 1.12f;         // Масштаб при выборе
    [SerializeField] private float glowPulseSpeed = 3.0f;       // Скорость пульсации неонового свечения
    [SerializeField] private float glowPulseMin = 0.5f;         // Минимальная прозрачность свечения
    [SerializeField] private float glowPulseMax = 1.0f;         // Максимальная прозрачность свечения

    // Визуальные компоненты
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private GameObject selectionGlow;      // Дочерний объект — неоновое свечение
    private SpriteRenderer glowRenderer;   // Рендерер свечения
    private Vector3 baseScale;             // Исходный масштаб

    // Флаги состояния
    private bool isMoving;
    private bool isSelected;

    // ========================= ПУБЛИЧНЫЕ СВОЙСТВА =========================

    public bool IsWhite => isWhite;
    public int CurrentPoint => currentPoint;
    public bool IsMoving => isMoving;
    public bool IsSelected => isSelected;
    public float MoveDuration => moveDuration;

    // ========================= ИНИЦИАЛИЗАЦИЯ =========================

    /// <summary>
    /// Инициализация фишки: устанавливает цвет, начальную позицию,
    /// кэширует ссылки на визуальные компоненты и исходный масштаб.
    /// </summary>
    public void Initialize(bool white, int point)
    {
        isWhite = white;
        currentPoint = point;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        baseScale = transform.localScale;
    }

    /// <summary>
    /// Обновляет логическую позицию фишки на доске.
    /// </summary>
    public void SetPoint(int point)
    {
        currentPoint = point;
    }

    // ========================= ВЫБОР ФИШКИ =========================

    /// <summary>
    /// Визуально выделяет фишку: увеличивает масштаб и показывает неоновое свечение.
    /// Белые фишки — розовое свечение (#ff00c8), чёрные — голубое (#00f0ff).
    /// </summary>
    public void Select()
    {
        if (isSelected) return;
        isSelected = true;

        // Плавное увеличение масштаба
        transform.localScale = baseScale * selectScale;

        // Создаём неоновое свечение при первом вызове
        if (selectionGlow == null && spriteRenderer != null)
        {
            selectionGlow = new GameObject("SelectionGlow");
            selectionGlow.transform.SetParent(transform);
            selectionGlow.transform.localPosition = Vector3.zero;
            selectionGlow.transform.localScale = Vector3.one * 2.0f; // Большое свечение

            glowRenderer = selectionGlow.AddComponent<SpriteRenderer>();

            // Неоновый цвет свечения: розовый для белых, голубой для чёрных
            Color glowColor = isWhite
                ? new Color(1.0f, 0.0f, 0.78f, 0.7f)   // Неоново-розовый (#ff00c8)
                : new Color(0.0f, 0.94f, 1.0f, 0.7f);  // Неоново-голубой (#00f0ff)

            glowRenderer.sprite = TextureGenerator.CreateGlowSprite(128, glowColor);
            glowRenderer.color = Color.white;
            glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 3;
        }

        if (selectionGlow != null)
            selectionGlow.SetActive(true);

        // Запускаем pop-анимацию при выборе
        StartCoroutine(PopOnSelect());
    }

    /// <summary>
    /// Снимает визуальное выделение: возвращает масштаб и скрывает свечение.
    /// </summary>
    public void Deselect()
    {
        if (!isSelected) return;
        isSelected = false;

        // Возвращаем исходный масштаб
        transform.localScale = baseScale;

        if (selectionGlow != null)
            selectionGlow.SetActive(false);
    }

    /// <summary>
    /// Pop-анимация при выборе фишки: быстрое увеличение с перерегулированием.
    /// </summary>
    private IEnumerator PopOnSelect()
    {
        Vector3 targetScale = baseScale * selectScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Кривая с overshoot: 1.0 → 1.3 → 1.12
            float scale;
            if (t < 0.5f)
                scale = 1f + (t / 0.5f) * 0.18f;           // Рост до 1.18
            else
                scale = 1.18f - ((t - 0.5f) / 0.5f) * 0.06f; // Возврат к 1.12

            transform.localScale = baseScale * scale;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    // ========================= ПУЛЬСАЦИЯ СВЕЧЕНИЯ =========================

    /// <summary>
    /// Пульсация неонового свечения выбранной фишки — синусоидальное изменение прозрачности.
    /// </summary>
    private void Update()
    {
        if (isSelected && glowRenderer != null && selectionGlow != null && selectionGlow.activeSelf)
        {
            // Синусоидальная пульсация альфы
            float pulse = Mathf.Lerp(glowPulseMin, glowPulseMax,
                (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);
            Color c = glowRenderer.color;
            c.a = pulse;
            glowRenderer.color = c;
        }
    }

    // ========================= АНИМАЦИЯ ДВИЖЕНИЯ =========================

    /// <summary>
    /// Плавно перемещает фишку к целевой позиции.
    /// Использует SmoothStep для естественного ускорения/замедления.
    /// Фишка слегка приподнимается по дуге при движении.
    /// </summary>
    /// <param name="target">Целевая мировая позиция</param>
    /// <param name="duration">Длительность; -1 = использовать moveDuration</param>
    public IEnumerator MoveToPosition(Vector3 target, float duration = -1f)
    {
        if (duration < 0f) duration = moveDuration;
        if (isMoving) yield break; // Защита от повторного вызова

        isMoving = true;
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        // Высота дуги зависит от расстояния
        float distance = Vector3.Distance(startPos, target);
        float arcHeight = Mathf.Min(distance * 0.15f, 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // SmoothStep — плавное начало и конец движения
            float smooth = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(startPos, target, smooth);

            // Параболическая дуга
            float arc = 4f * arcHeight * t * (1f - t);
            pos.y += arc;

            transform.position = pos;
            yield return null;
        }

        transform.position = target;
        isMoving = false;
    }

    // ========================= ЭФФЕКТ ВСПЫШКИ =========================

    /// <summary>
    /// Кратковременная неоновая вспышка при битье фишки противника.
    /// Фишка на мгновение становится ярко-белой с неоновым оттенком.
    /// </summary>
    public IEnumerator Flash()
    {
        if (spriteRenderer == null) yield break;

        Color currentColor = spriteRenderer.color;
        Vector3 currentScale = transform.localScale;

        // Вспышка — яркий неоновый цвет + увеличение
        Color flashColor = isWhite
            ? new Color(1f, 0.5f, 1f)   // Розовая вспышка для белых
            : new Color(0.5f, 1f, 1f);  // Голубая вспышка для чёрных

        spriteRenderer.color = flashColor;
        transform.localScale = currentScale * 1.3f;

        yield return new WaitForSeconds(flashDuration * 0.5f);

        // Возврат к нормальному размеру
        transform.localScale = currentScale;

        yield return new WaitForSeconds(flashDuration * 0.5f);

        // Возврат цвета
        spriteRenderer.color = currentColor;

        // Тряска камеры при битье
        if (BalatroEffects.Instance != null)
            BalatroEffects.Instance.ShakeCamera(0.05f);
    }
}

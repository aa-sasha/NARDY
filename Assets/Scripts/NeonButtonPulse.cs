using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Добавляет неоновое мерцание и pop-анимацию к UI кнопкам.
/// Прикрепляется к GameObject с компонентом Button.
/// </summary>
[RequireComponent(typeof(Button))]
public class NeonButtonPulse : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Настройки пульсации")]
    [SerializeField] private float pulseSpeed = 1.5f;          // Скорость пульсации (Гц)
    [SerializeField] private float pulseMinAlpha = 0.75f;      // Минимальная яркость
    [SerializeField] private float pulseMaxAlpha = 1.0f;       // Максимальная яркость

    [Header("Настройки наведения")]
    [SerializeField] private float hoverScaleUp = 1.06f;       // Масштаб при наведении
    [SerializeField] private float hoverAnimSpeed = 8f;        // Скорость анимации наведения

    [Header("Настройки клика")]
    [SerializeField] private float clickScaleDown = 0.94f;     // Масштаб при нажатии

    // Компоненты
    private Image buttonImage;
    private Color originalColor;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isHovered;
    private float pulseTime;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
            originalColor = buttonImage.color;
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        // Пульсация яркости
        if (buttonImage != null)
        {
            pulseTime += Time.deltaTime;
            float pulse = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                (Mathf.Sin(pulseTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f);

            Color c = buttonImage.color;
            // Пульсируем яркостью, не альфой (чтобы не менять прозрачность)
            float brightness = isHovered ? 1.15f : pulse;
            buttonImage.color = new Color(
                Mathf.Clamp01(originalColor.r * brightness),
                Mathf.Clamp01(originalColor.g * brightness),
                Mathf.Clamp01(originalColor.b * brightness),
                originalColor.a
            );
        }

        // Плавная анимация масштаба
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale,
            Time.deltaTime * hoverAnimSpeed);
    }

    // ========================= СОБЫТИЯ МЫШИ =========================

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = originalScale * hoverScaleUp;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Pop-анимация при клике
        StartCoroutine(ClickAnimation());
    }

    /// <summary>
    /// Анимация нажатия: быстрое уменьшение и возврат с перерегулированием.
    /// </summary>
    private IEnumerator ClickAnimation()
    {
        // Сжатие
        float elapsed = 0f;
        float shrinkDuration = 0.08f;
        Vector3 startScale = transform.localScale;
        Vector3 shrinkScale = originalScale * clickScaleDown;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, shrinkScale, elapsed / shrinkDuration);
            yield return null;
        }

        // Возврат с overshoot
        elapsed = 0f;
        float returnDuration = 0.2f;
        Vector3 overshootScale = originalScale * (isHovered ? hoverScaleUp * 1.05f : 1.08f);

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            // Кривая с перерегулированием
            float scale = t < 0.5f
                ? Mathf.Lerp(clickScaleDown, overshootScale.x / originalScale.x, t * 2f)
                : Mathf.Lerp(overshootScale.x / originalScale.x, isHovered ? hoverScaleUp : 1f, (t - 0.5f) * 2f);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        targetScale = isHovered ? originalScale * hoverScaleUp : originalScale;
        transform.localScale = targetScale;
    }
}

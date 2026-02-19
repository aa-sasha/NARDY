using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Управление кубиками для длинных нард.
/// Два кубика (1-6). Дубль даёт 4 хода вместо 2.
/// Включает визуальные спрайты кубиков на доске, анимацию броска
/// с вращением, сменой граней и покачиванием после остановки.
/// </summary>
public class Dice : MonoBehaviour
{
    [SerializeField] private Text diceResultText;

    [Header("Настройки анимации броска")]
    [SerializeField] private float rollAnimDuration = 0.7f;    // Длительность анимации (секунды)
    [SerializeField] private float rollTickInterval = 0.06f;   // Интервал смены граней при броске
    [SerializeField] private float wobbleDuration = 0.4f;      // Длительность покачивания после остановки
    [SerializeField] private float wobbleAngle = 8f;           // Максимальный угол покачивания (градусы)

    [Header("Настройки визуала кубиков")]
    [SerializeField] private float diceSize = 0.7f;            // Размер кубика в мировых единицах
    [SerializeField] private float diceSpacing = 0.15f;        // Расстояние между кубиками
    [SerializeField] private Vector3 dicePosition = new Vector3(0f, 0f, -0.1f); // Позиция на доске (центр бара)

    // Логика кубиков
    private int die1;
    private int die2;
    private List<int> remainingValues = new List<int>();
    private bool hasRolled;
    private bool isRolling; // Флаг: анимация броска активна

    // Визуальные объекты кубиков на доске
    private GameObject dice1Object;
    private GameObject dice2Object;
    private SpriteRenderer dice1Renderer;
    private SpriteRenderer dice2Renderer;

    // Кэш спрайтов граней (6 штук, индекс = значение - 1)
    private Sprite[] faceSprites;

    // Цвета кубиков — тёмный фон + неоновые точки
    private readonly Color diceFaceColor = new Color(0.06f, 0.04f, 0.12f);  // Тёмно-фиолетовый фон
    private readonly Color diceDotColor = new Color(1.0f, 0.0f, 0.78f);     // Неоново-розовые точки

    // ========================= ПУБЛИЧНЫЕ СВОЙСТВА =========================

    public int Die1 => die1;
    public int Die2 => die2;
    public bool IsDouble => die1 == die2 && die1 > 0;
    public bool HasRolled => hasRolled;
    public bool AllUsed => hasRolled && remainingValues.Count == 0;
    public bool IsRolling => isRolling;

    /// <summary>
    /// Возвращает копию оставшихся (неиспользованных) значений кубиков
    /// </summary>
    public List<int> RemainingValues => new List<int>(remainingValues);

    // ========================= ИНИЦИАЛИЗАЦИЯ =========================

    private void Start()
    {
        // Создаём спрайты для всех 6 граней
        CreateFaceSprites();

        // Создаём визуальные объекты кубиков на доске
        CreateDiceVisuals();
    }

    public void SetDiceText(Text text)
    {
        diceResultText = text;
    }

    /// <summary>
    /// Генерирует 6 пиксельных спрайтов граней кубика.
    /// Тёмный фон с неоновыми точками.
    /// </summary>
    private void CreateFaceSprites()
    {
        faceSprites = new Sprite[6];
        int resolution = 64; // Пиксельное разрешение (меньше = более пиксельный вид)

        for (int i = 0; i < 6; i++)
        {
            faceSprites[i] = TextureGenerator.CreatePixelDiceFaceSprite(
                resolution, i + 1, diceFaceColor, diceDotColor);
        }
    }

    /// <summary>
    /// Создаёт два игровых объекта для визуального отображения кубиков на доске.
    /// Изначально скрыты, появляются при броске.
    /// </summary>
    private void CreateDiceVisuals()
    {
        // Кубик 1
        dice1Object = new GameObject("Dice1_Visual");
        dice1Object.transform.SetParent(transform);
        dice1Object.transform.localPosition = dicePosition + new Vector3(-(diceSize / 2f + diceSpacing / 2f), 0, 0);
        dice1Object.transform.localScale = Vector3.one * diceSize;

        dice1Renderer = dice1Object.AddComponent<SpriteRenderer>();
        dice1Renderer.sprite = faceSprites[0];
        dice1Renderer.sortingOrder = 40;
        dice1Object.SetActive(false);

        // Тень под кубиком 1
        CreateDiceShadow(dice1Object);

        // Кубик 2
        dice2Object = new GameObject("Dice2_Visual");
        dice2Object.transform.SetParent(transform);
        dice2Object.transform.localPosition = dicePosition + new Vector3(diceSize / 2f + diceSpacing / 2f, 0, 0);
        dice2Object.transform.localScale = Vector3.one * diceSize;

        dice2Renderer = dice2Object.AddComponent<SpriteRenderer>();
        dice2Renderer.sprite = faceSprites[0];
        dice2Renderer.sortingOrder = 40;
        dice2Object.SetActive(false);

        // Тень под кубиком 2
        CreateDiceShadow(dice2Object);
    }

    /// <summary>
    /// Добавляет мягкую тень под кубиком.
    /// </summary>
    private void CreateDiceShadow(GameObject diceObj)
    {
        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(diceObj.transform);
        shadow.transform.localPosition = new Vector3(0.06f, -0.08f, 0.01f);
        shadow.transform.localScale = Vector3.one * 1.1f;

        SpriteRenderer shadowSr = shadow.AddComponent<SpriteRenderer>();
        shadowSr.sprite = TextureGenerator.CreateShadowSprite(64, 0.3f);
        shadowSr.color = Color.white;
        shadowSr.sortingOrder = 39;
    }

    // ========================= БРОСОК =========================

    /// <summary>
    /// Бросок обоих кубиков. Значения определяются сразу,
    /// затем запускается визуальная анимация с вращением и сменой граней.
    /// Дубль даёт 4 одинаковых значения.
    /// </summary>
    public void RollDice()
    {
        die1 = Random.Range(1, 7);
        die2 = Random.Range(1, 7);
        hasRolled = true;

        remainingValues.Clear();
        if (IsDouble)
        {
            for (int i = 0; i < 4; i++)
                remainingValues.Add(die1);
        }
        else
        {
            remainingValues.Add(die1);
            remainingValues.Add(die2);
        }

        // Запускаем визуальную анимацию
        StartCoroutine(RollAnimation());
        Debug.Log($"Rolled: {die1} | {die2}" + (IsDouble ? " (Double!)" : ""));
    }

    /// <summary>
    /// Анимация броска: показываем кубики, быстро меняем грани (имитация вращения),
    /// затем показываем результат и выполняем покачивание.
    /// </summary>
    private IEnumerator RollAnimation()
    {
        isRolling = true;

        // Показываем кубики
        dice1Object.SetActive(true);
        dice2Object.SetActive(true);

        float elapsed = 0f;

        // Фаза 1: Быстрая смена граней (имитация вращения)
        while (elapsed < rollAnimDuration)
        {
            int fake1 = Random.Range(1, 7);
            int fake2 = Random.Range(1, 7);

            // Обновляем спрайты кубиков
            if (faceSprites != null)
            {
                dice1Renderer.sprite = faceSprites[fake1 - 1];
                dice2Renderer.sprite = faceSprites[fake2 - 1];
            }

            // Лёгкое вращение для эффекта подбрасывания
            float angle = Random.Range(-15f, 15f);
            dice1Object.transform.localRotation = Quaternion.Euler(0, 0, angle);
            dice2Object.transform.localRotation = Quaternion.Euler(0, 0, -angle);

            // Обновляем UI текст (как раньше)
            if (diceResultText != null)
                diceResultText.text = $"Dice: {fake1} | {fake2}";

            yield return new WaitForSeconds(rollTickInterval);
            elapsed += rollTickInterval;
        }

        // Фаза 2: Показываем реальный результат
        dice1Renderer.sprite = faceSprites[die1 - 1];
        dice2Renderer.sprite = faceSprites[die2 - 1];

        // Фаза 3: Покачивание после остановки (эффект упругости)
        yield return WobbleAnimation(dice1Object, dice2Object);

        // Финализация: ровное положение
        dice1Object.transform.localRotation = Quaternion.identity;
        dice2Object.transform.localRotation = Quaternion.identity;

        UpdateDiceUI();
        isRolling = false;
    }

    /// <summary>
    /// Анимация покачивания кубиков после остановки.
    /// Затухающие колебания — создают ощущение физики.
    /// </summary>
    private IEnumerator WobbleAnimation(GameObject d1, GameObject d2)
    {
        float elapsed = 0f;

        while (elapsed < wobbleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / wobbleDuration;

            // Затухающая синусоида — амплитуда уменьшается со временем
            float damping = 1f - t;
            float angle = Mathf.Sin(t * Mathf.PI * 6f) * wobbleAngle * damping;

            d1.transform.localRotation = Quaternion.Euler(0, 0, angle);
            d2.transform.localRotation = Quaternion.Euler(0, 0, -angle * 0.8f); // Асимметрия для реалистичности

            yield return null;
        }
    }

    // ========================= УПРАВЛЕНИЕ КУБИКАМИ =========================

    /// <summary>
    /// Проверяет, доступно ли конкретное значение кубика
    /// </summary>
    public bool CanUseDiceValue(int value)
    {
        return remainingValues.Contains(value);
    }

    /// <summary>
    /// Помечает одно значение кубика как использованное.
    /// Обновляет визуал — использованные кубики становятся полупрозрачными.
    /// </summary>
    public void UseDiceValue(int value)
    {
        int index = remainingValues.IndexOf(value);
        if (index >= 0)
        {
            remainingValues.RemoveAt(index);
            UpdateDiceUI();
            UpdateDiceVisualState();
            Debug.Log($"Used dice value: {value}. Remaining: [{string.Join(", ", remainingValues)}]");
        }
    }

    /// <summary>
    /// Возвращает уникальные доступные значения кубиков
    /// </summary>
    public List<int> GetAvailableValues()
    {
        HashSet<int> unique = new HashSet<int>(remainingValues);
        return new List<int>(unique);
    }

    /// <summary>
    /// Сброс кубиков для нового хода.
    /// Скрывает визуальные кубики.
    /// </summary>
    public void ResetDice()
    {
        die1 = 0;
        die2 = 0;
        hasRolled = false;
        remainingValues.Clear();
        UpdateDiceUI();

        // Скрываем визуальные кубики
        if (dice1Object != null) dice1Object.SetActive(false);
        if (dice2Object != null) dice2Object.SetActive(false);
    }

    /// <summary>
    /// Обновляет визуальное состояние кубиков на доске.
    /// Использованные кубики становятся полупрозрачными.
    /// </summary>
    private void UpdateDiceVisualState()
    {
        if (dice1Renderer == null || dice2Renderer == null) return;

        if (!IsDouble)
        {
            // Обычный бросок: проверяем каждый кубик отдельно
            bool die1Used = !remainingValues.Contains(die1);
            bool die2Used = !remainingValues.Contains(die2);

            // Если оба значения одинаковы, но не дубль — управляем по количеству
            if (die1 == die2)
            {
                die1Used = remainingValues.Count < 2;
                die2Used = remainingValues.Count < 1;
            }

            dice1Renderer.color = die1Used ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
            dice2Renderer.color = die2Used ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
        }
        else
        {
            // Дубль: оба кубика показывают одинаковое состояние
            float alpha = remainingValues.Count > 0 ? 1f : 0.3f;
            dice1Renderer.color = new Color(1f, 1f, 1f, alpha);
            dice2Renderer.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    /// <summary>
    /// Обновляет UI текст с текущими значениями кубиков.
    /// Использованные значения показываются в скобках.
    /// </summary>
    private void UpdateDiceUI()
    {
        if (diceResultText == null) return;

        if (!hasRolled)
        {
            diceResultText.text = "Dice: - | -";
            return;
        }

        if (IsDouble)
        {
            int usedCount = 4 - remainingValues.Count;
            string display = $"Dice: {die1} x4";
            if (usedCount > 0)
                display += $"  (used {usedCount})";
            diceResultText.text = display;
        }
        else
        {
            // Показываем каждый кубик; использованные — в скобках
            string d1Str = remainingValues.Contains(die1) ? die1.ToString() : $"({die1})";
            string d2Str = remainingValues.Contains(die2) ? die2.ToString() : $"({die2})";

            // Особый случай: оба кубика с одинаковым значением (но не дубль)
            if (die1 == die2)
            {
                d1Str = remainingValues.Count >= 1 ? die1.ToString() : $"({die1})";
                d2Str = remainingValues.Count >= 2 ? die2.ToString() : $"({die2})";
            }

            diceResultText.text = $"Dice: {d1Str} | {d2Str}";
        }
    }
}

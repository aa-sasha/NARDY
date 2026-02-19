using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Контроллер главного меню. Создаёт стилизованный UI с логотипом,
/// выбором сложности и кнопкой старта. Визуал в едином стиле с игрой:
/// тёмное дерево, золотые акценты, благородные цвета.
/// Устанавливает GameManager.SelectedDifficulty перед загрузкой игровой сцены.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    private GameManager.Difficulty selectedDifficulty = GameManager.Difficulty.Medium;

    // Ссылки на кнопки для подсветки активной сложности
    private Button btnEasy, btnMedium, btnHard;
    private Image imgEasy, imgMedium, imgHard;

    // ========================= ЦВЕТОВАЯ ПАЛИТРА BALATRO =========================

    // Цвета в стиле Balatro: тёмный фон + неоновые акценты
    private readonly Color bgColor = new Color(0.04f, 0.03f, 0.08f, 1f);                // Тёмно-синий фон (#0a0a14)
    private readonly Color panelColor = new Color(0.06f, 0.04f, 0.14f, 0.97f);          // Тёмно-фиолетовая панель
    private readonly Color goldText = new Color(0.0f, 0.94f, 1.0f);                     // Неоново-голубой (#00f0ff)
    private readonly Color lightText = new Color(0.85f, 0.80f, 0.95f);                  // Светло-фиолетовый текст
    private readonly Color subtitleText = new Color(0.50f, 0.45f, 0.65f);               // Приглушённый фиолетовый
    private readonly Color activeColor = new Color(0.0f, 1.0f, 0.5f, 0.95f);            // Неоново-зелёный — активный
    private readonly Color inactiveColor = new Color(0.08f, 0.05f, 0.18f, 0.90f);       // Тёмно-фиолетовый — неактивный
    private readonly Color startBtnColor = new Color(1.0f, 0.0f, 0.78f, 0.90f);         // Неоново-розовая кнопка «Start»
    private readonly Color goldAccent = new Color(0.4f, 0.0f, 0.8f, 0.6f);              // Фиолетовый акцент для линий
    private readonly Color buttonShadowColor = new Color(0f, 0f, 0f, 0.7f);             // Тень кнопок

    // ========================= ИНИЦИАЛИЗАЦИЯ =========================

    private void Start()
    {
        // Запускаем CRT-эффекты Balatro
        if (FindFirstObjectByType<BalatroEffects>() == null)
        {
            GameObject fxObj = new GameObject("BalatroEffects");
            fxObj.AddComponent<BalatroEffects>();
        }
        CreateUI();
    }

    private void CreateUI()
    {
        // Event System
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Canvas
        GameObject canvasObj = new GameObject("MenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Фоновая панель — тёмная текстура
        CreatePanel(canvasObj.transform, "Background",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, bgColor);

        // Декоративная линия по центру (горизонтальная)
        CreateDecorativeLine(canvasObj.transform, new Vector2(0, -30), 600f);

        // Центральная карточка — основная панель меню
        GameObject centerPanel = CreatePanel(canvasObj.transform, "CenterPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(520, 440), panelColor);

        // Золотая рамка панели
        Outline panelOutline = centerPanel.AddComponent<Outline>();
        panelOutline.effectColor = goldAccent;
        panelOutline.effectDistance = new Vector2(2, -2);

        // ========================= ЛОГОТИП =========================

        // Заголовок «NARDY» — крупный золотой
        Text titleText = CreateText(centerPanel.transform, "Title",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -50), new Vector2(400, 80), "NARDY", 64, goldText);
        titleText.fontStyle = FontStyle.Bold;

        // Декоративная линия под заголовком
        CreateDecorativeLine(centerPanel.transform, new Vector2(0, -95), 280f);

        // Подзаголовок
        CreateText(centerPanel.transform, "Subtitle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -125), new Vector2(400, 35), "[ LONG BACKGAMMON ]", 18, subtitleText);

        // ========================= ВЫБОР СЛОЖНОСТИ =========================

        // Метка «AI Difficulty»
        CreateText(centerPanel.transform, "DiffLabel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 30), new Vector2(300, 35), "AI Difficulty", 20, lightText);

        // Кнопки сложности — горизонтальный ряд
        btnEasy = CreateStyledButton(centerPanel.transform, "BtnEasy", "Easy",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-140, -20), new Vector2(125, 42),
            () => SelectDifficulty(GameManager.Difficulty.Easy), inactiveColor, lightText);
        imgEasy = btnEasy.GetComponent<Image>();

        btnMedium = CreateStyledButton(centerPanel.transform, "BtnMedium", "Medium",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -20), new Vector2(125, 42),
            () => SelectDifficulty(GameManager.Difficulty.Medium), inactiveColor, lightText);
        imgMedium = btnMedium.GetComponent<Image>();

        btnHard = CreateStyledButton(centerPanel.transform, "BtnHard", "Hard",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(140, -20), new Vector2(125, 42),
            () => SelectDifficulty(GameManager.Difficulty.Hard), inactiveColor, lightText);
        imgHard = btnHard.GetComponent<Image>();

        UpdateDifficultyHighlight();

        // ========================= КНОПКА СТАРТА =========================

        // Декоративная линия перед кнопкой старта
        CreateDecorativeLine(centerPanel.transform, new Vector2(0, -80), 350f);

        // Кнопка «Start Game» — крупная, зелёная
        CreateStyledButton(centerPanel.transform, "BtnStart", "Start Game",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 55), new Vector2(220, 55),
            OnStartGame, startBtnColor, goldText, 26);

        // ========================= ВЕРСИЯ =========================

        // Номер версии внизу
        CreateText(canvasObj.transform, "Version",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 15), new Vector2(200, 25), "v1.0", 14, subtitleText);
    }

    // ========================= ЛОГИКА =========================

    private void SelectDifficulty(GameManager.Difficulty diff)
    {
        selectedDifficulty = diff;
        UpdateDifficultyHighlight();
    }

    /// <summary>
    /// Обновляет визуальное выделение кнопок сложности.
    /// Активная кнопка — зелёная, остальные — неактивные.
    /// </summary>
    private void UpdateDifficultyHighlight()
    {
        if (imgEasy != null) imgEasy.color = (selectedDifficulty == GameManager.Difficulty.Easy) ? activeColor : inactiveColor;
        if (imgMedium != null) imgMedium.color = (selectedDifficulty == GameManager.Difficulty.Medium) ? activeColor : inactiveColor;
        if (imgHard != null) imgHard.color = (selectedDifficulty == GameManager.Difficulty.Hard) ? activeColor : inactiveColor;
    }

    private void OnStartGame()
    {
        GameManager.SelectedDifficulty = selectedDifficulty;
        SceneManager.LoadScene("GameScene");
    }

    // ========================= UI ХЕЛПЕРЫ =========================

    private GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = color;

        return obj;
    }

    private Text CreateText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size, string content, int fontSize, Color textColor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = content;

        // Мягкая тень
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(1, -1);

        return text;
    }

    /// <summary>
    /// Создаёт стилизованную кнопку с тенью и эффектом наведения.
    /// </summary>
    private Button CreateStyledButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction action, Color bgColor, Color textColor, int fontSize = 20)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = bgColor;

        // Тень кнопки
        Shadow btnShadow = obj.AddComponent<Shadow>();
        btnShadow.effectColor = buttonShadowColor;
        btnShadow.effectDistance = new Vector2(2, -2);

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        // Цвета состояний кнопки
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        // Неоновая пульсация кнопки (Balatro-стиль)
        obj.AddComponent<NeonButtonPulse>();

        // Текст кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text btnText = textObj.AddComponent<Text>();
        btnText.text = label;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = fontSize;
        btnText.color = textColor;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.fontStyle = FontStyle.Bold;

        return btn;
    }

    /// <summary>
    /// Создаёт декоративную горизонтальную линию (золотой акцент).
    /// Добавляет элегантность между секциями меню.
    /// </summary>
    private void CreateDecorativeLine(Transform parent, Vector2 position, float width)
    {
        GameObject line = new GameObject("DecorLine");
        line.transform.SetParent(parent, false);

        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, 1);

        Image img = line.AddComponent<Image>();
        img.color = goldAccent;
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Создаёт доску для длинных нард в неоновом стиле:
/// тёмный фон, неоновые треугольники, пиксельная эстетика.
///
/// Раскладка доски (перспектива белых):
/// ВЕРХ:   13 14 15 16 17 18 | БАР | 19 20 21 22 23 24
/// НИЗ:   12 11 10  9  8  7 | БАР |  6  5  4  3  2  1
/// </summary>
public class BoardSetup : MonoBehaviour
{
    // ========================= РАЗМЕРЫ ДОСКИ =========================

    private const float BoardWidth = 13f;     // Общая ширина игрового поля
    private const float BoardHeight = 9f;     // Общая высота игрового поля
    private const float PointWidth = 0.85f;   // Ширина основания треугольника
    private const float PointHeight = 3.2f;   // Высота треугольника
    private const float BarWidth = 1f;        // Ширина центрального бара
    private const float BorderSize = 0.5f;    // Толщина рамки

    // ========================= ЦВЕТОВАЯ ПАЛИТРА =========================

    // Фон доски — глубокий чёрный/тёмно-синий
    private readonly Color bgColor = new Color(0.04f, 0.03f, 0.08f);         // #0a0a14

    // Пункты — неоновые цвета
    private readonly Color neonCyan = new Color(0.0f, 0.94f, 1.0f);          // #00f0ff — неоново-голубой
    private readonly Color neonPink = new Color(1.0f, 0.0f, 0.78f);          // #ff00c8 — неоново-розовый
    private readonly Color darkPoint1 = new Color(0.07f, 0.04f, 0.14f);      // Тёмно-фиолетовый пункт
    private readonly Color darkPoint2 = new Color(0.04f, 0.06f, 0.14f);      // Тёмно-синий пункт

    // Рамка — тёмная с неоновой окантовкой
    private readonly Color borderColor = new Color(0.08f, 0.06f, 0.15f);     // Тёмно-фиолетовая рамка
    private readonly Color borderGlow = new Color(0.4f, 0.0f, 0.8f, 0.6f);  // Фиолетовое свечение рамки

    // Бар — тёмный с золотой линией
    private readonly Color barColor = new Color(0.06f, 0.04f, 0.12f);        // Тёмный бар
    private readonly Color barAccent = new Color(0.8f, 0.6f, 0.0f, 0.5f);   // Золотой акцент бара

    // Нумерация пунктов
    private readonly Color numberColor = new Color(0.5f, 0.5f, 0.7f, 0.7f); // Приглушённый синевато-серый

    // ========================= ДАННЫЕ ПУНКТОВ =========================

    private Dictionary<int, Vector3> pointPositions = new Dictionary<int, Vector3>();
    private Dictionary<int, bool> pointIsTop = new Dictionary<int, bool>();
    private Dictionary<int, BoardPoint> boardPoints = new Dictionary<int, BoardPoint>();

    // Спрайты треугольников (4 варианта: 2 цвета x 2 направления)
    private Sprite cyanTriangleUp;
    private Sprite cyanTriangleDown;
    private Sprite pinkTriangleUp;
    private Sprite pinkTriangleDown;

    // Публичный доступ для GameManager
    public Dictionary<int, Vector3> PointPositions => pointPositions;
    public Dictionary<int, bool> PointIsTop => pointIsTop;
    public Dictionary<int, BoardPoint> BoardPoints => boardPoints;

    // ========================= ИНИЦИАЛИЗАЦИЯ =========================

    private void Awake()
    {
        CreateTriangleSprites();
        CreateBoard();
    }

    private void Start()
    {
        // Запускаем визуальные эффекты
        if (VisualEffects.Instance == null)
        {
            GameObject fxObj = new GameObject("VisualEffects");
            fxObj.AddComponent<VisualEffects>();
        }
    }

    /// <summary>
    /// Создаёт 4 спрайта треугольников в пиксельном неоновом стиле.
    /// Голубые и розовые, направленные вверх и вниз.
    /// </summary>
    private void CreateTriangleSprites()
    {
        int triWidth = 64;
        int triHeight = 256;

        // Голубые треугольники (неоново-голубой)
        cyanTriangleUp = CreateNeonTriangleSprite(triWidth, triHeight, neonCyan, darkPoint1, false);
        cyanTriangleDown = CreateNeonTriangleSprite(triWidth, triHeight, neonCyan, darkPoint1, true);

        // Розовые треугольники (неоново-розовый)
        pinkTriangleUp = CreateNeonTriangleSprite(triWidth, triHeight, neonPink, darkPoint2, false);
        pinkTriangleDown = CreateNeonTriangleSprite(triWidth, triHeight, neonPink, darkPoint2, true);
    }

    /// <summary>
    /// Создаёт неоновый треугольник с тёмным заполнением и светящимися краями.
    /// Тёмная заливка + яркий неоновый контур.
    /// </summary>
    private Sprite CreateNeonTriangleSprite(int width, int height, Color neonColor, Color fillColor, bool flipped)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float halfW = width / 2f;
        float edgeGlowWidth = 3f; // Ширина неонового края в пикселях

        for (int y = 0; y < height; y++)
        {
            // Для перевёрнутого треугольника — инвертируем прогресс
            float progress = flipped ? (1f - (float)y / height) : (float)y / height;
            float halfWidth = halfW * (1f - progress);

            for (int x = 0; x < width; x++)
            {
                float distFromCenter = Mathf.Abs(x - halfW);

                if (distFromCenter <= halfWidth)
                {
                    // Антиалиасинг края
                    float edgeDist = halfWidth - distFromCenter;
                    float alpha = Mathf.Clamp01(edgeDist * 2f);

                    // Расстояние от края треугольника
                    float edgeProximity = Mathf.Clamp01(1f - edgeDist / edgeGlowWidth);

                    // Тёмная заливка с неоновым свечением у краёв
                    Color pixel;
                    if (edgeDist <= edgeGlowWidth)
                    {
                        // Неоновый край — яркий цвет с затуханием
                        float glowFactor = 1f - (edgeDist / edgeGlowWidth);
                        glowFactor = glowFactor * glowFactor; // Квадратичное затухание
                        pixel = Color.Lerp(fillColor, neonColor, glowFactor * 0.85f);
                    }
                    else
                    {
                        // Тёмная заливка с лёгким градиентом
                        float centerBlend = 1f - (distFromCenter / Mathf.Max(halfWidth - edgeGlowWidth, 0.001f));
                        pixel = Color.Lerp(fillColor, fillColor * 1.3f, centerBlend * 0.2f);
                    }

                    // Лёгкая пиксельная текстура (шум)
                    float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.05f;
                    pixel.r = Mathf.Clamp01(pixel.r + noise);
                    pixel.g = Mathf.Clamp01(pixel.g + noise);
                    pixel.b = Mathf.Clamp01(pixel.b + noise);

                    pixel.a = alpha;
                    pixels[y * width + x] = pixel;
                }
                else
                {
                    pixels[y * width + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // Пивот: для обычного — (0.5, 0), для перевёрнутого — (0.5, 1)
        Vector2 pivot = flipped ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        return Sprite.Create(tex, new Rect(0, 0, width, height), pivot, width);
    }

    // ========================= ПОСТРОЕНИЕ ДОСКИ =========================

    private void CreateBoard()
    {
        // 1. Фон доски (тёмный)
        CreateDarkBackground();

        // 2. Рамка с неоновым свечением
        CreateNeonBorder();

        // 3. Центральный бар
        CreateBar();

        // 4. Все 24 пункта
        CreateAllPoints();

        // 5. Виньетка по краям (эффект глубины)
        CreateEdgeVignette();
    }

    /// <summary>
    /// Создаёт тёмный фон доски — основа для неонового стиля.
    /// </summary>
    private void CreateDarkBackground()
    {
        Sprite bgSprite = TextureGenerator.CreateRectSprite();
        CreateColoredRect("Background", Vector3.zero,
            new Vector2(BoardWidth, BoardHeight), bgColor, -5, bgSprite);
    }

    /// <summary>
    /// Создаёт рамку с неоновым фиолетовым свечением.
    /// Несколько слоёв для эффекта bloom.
    /// </summary>
    private void CreateNeonBorder()
    {
        Sprite lineSprite = TextureGenerator.CreateRectSprite();
        float hw = BoardWidth / 2f;
        float hh = BoardHeight / 2f;
        float thick = 0.06f;

        // Основная рамка — тёмный фон
        float outerW = BoardWidth + BorderSize * 2 + 0.3f;
        float outerH = BoardHeight + BorderSize * 2 + 0.3f;
        CreateColoredRect("Border_BG", Vector3.zero,
            new Vector2(outerW, outerH), borderColor, -3, lineSprite);

        // Неоновые линии по периметру (несколько слоёв для bloom-эффекта)
        Color glowBright = new Color(borderGlow.r, borderGlow.g, borderGlow.b, 0.9f);
        Color glowMid = new Color(borderGlow.r, borderGlow.g, borderGlow.b, 0.4f);
        Color glowFar = new Color(borderGlow.r, borderGlow.g, borderGlow.b, 0.15f);

        // Верхняя линия (3 слоя)
        CreateColoredRect("Border_Top_1", new Vector3(0, hh, 0), new Vector2(BoardWidth, thick * 3), glowFar, -1, lineSprite);
        CreateColoredRect("Border_Top_2", new Vector3(0, hh, 0), new Vector2(BoardWidth, thick * 1.5f), glowMid, 0, lineSprite);
        CreateColoredRect("Border_Top_3", new Vector3(0, hh, 0), new Vector2(BoardWidth, thick), glowBright, 1, lineSprite);

        // Нижняя линия
        CreateColoredRect("Border_Bot_1", new Vector3(0, -hh, 0), new Vector2(BoardWidth, thick * 3), glowFar, -1, lineSprite);
        CreateColoredRect("Border_Bot_2", new Vector3(0, -hh, 0), new Vector2(BoardWidth, thick * 1.5f), glowMid, 0, lineSprite);
        CreateColoredRect("Border_Bot_3", new Vector3(0, -hh, 0), new Vector2(BoardWidth, thick), glowBright, 1, lineSprite);

        // Левая линия
        CreateColoredRect("Border_Left_1", new Vector3(-hw, 0, 0), new Vector2(thick * 3, BoardHeight), glowFar, -1, lineSprite);
        CreateColoredRect("Border_Left_2", new Vector3(-hw, 0, 0), new Vector2(thick * 1.5f, BoardHeight), glowMid, 0, lineSprite);
        CreateColoredRect("Border_Left_3", new Vector3(-hw, 0, 0), new Vector2(thick, BoardHeight), glowBright, 1, lineSprite);

        // Правая линия
        CreateColoredRect("Border_Right_1", new Vector3(hw, 0, 0), new Vector2(thick * 3, BoardHeight), glowFar, -1, lineSprite);
        CreateColoredRect("Border_Right_2", new Vector3(hw, 0, 0), new Vector2(thick * 1.5f, BoardHeight), glowMid, 0, lineSprite);
        CreateColoredRect("Border_Right_3", new Vector3(hw, 0, 0), new Vector2(thick, BoardHeight), glowBright, 1, lineSprite);
    }

    /// <summary>
    /// Создаёт центральный бар с тёмным фоном и золотыми акцентными линиями.
    /// </summary>
    private void CreateBar()
    {
        Sprite barSprite = TextureGenerator.CreateRectSprite();
        CreateColoredRect("Bar", new Vector3(0, 0, 0.01f),
            new Vector2(BarWidth, BoardHeight), barColor, 0, barSprite);

        // Золотые линии по краям бара
        Sprite lineSprite = TextureGenerator.CreateRectSprite();
        float lineW = 0.04f;
        CreateColoredRect("BarLine_L", new Vector3(-BarWidth / 2f, 0, 0f), new Vector2(lineW, BoardHeight),
            barAccent, 1, lineSprite);
        CreateColoredRect("BarLine_R", new Vector3(BarWidth / 2f, 0, 0f), new Vector2(lineW, BoardHeight),
            barAccent, 1, lineSprite);
    }

    // ========================= ПУНКТЫ (ТРЕУГОЛЬНИКИ) =========================

    /// <summary>
    /// Размещает все 24 пункта на доске с чередованием голубого и розового.
    /// </summary>
    private void CreateAllPoints()
    {
        // Нижний ряд, правая сторона: пункты 1-6
        for (int i = 1; i <= 6; i++)
        {
            float x = BarWidth / 2f + 0.5f + (6 - i) * 1f;
            float y = -BoardHeight / 2f + BorderSize;
            CreatePoint(i, new Vector3(x, y, 0), i % 2 == 1, false);
        }

        // Нижний ряд, левая сторона: пункты 7-12
        for (int i = 7; i <= 12; i++)
        {
            float x = -(BarWidth / 2f + 0.5f + (i - 7) * 1f);
            float y = -BoardHeight / 2f + BorderSize;
            CreatePoint(i, new Vector3(x, y, 0), i % 2 == 1, false);
        }

        // Верхний ряд, левая сторона: пункты 13-18
        for (int i = 13; i <= 18; i++)
        {
            float x = -(BarWidth / 2f + 0.5f + (18 - i) * 1f);
            float y = BoardHeight / 2f - BorderSize;
            CreatePoint(i, new Vector3(x, y, 0), i % 2 == 1, true);
        }

        // Верхний ряд, правая сторона: пункты 19-24
        for (int i = 19; i <= 24; i++)
        {
            float x = BarWidth / 2f + 0.5f + (i - 19) * 1f;
            float y = BoardHeight / 2f - BorderSize;
            CreatePoint(i, new Vector3(x, y, 0), i % 2 == 1, true);
        }
    }

    /// <summary>
    /// Создаёт один пункт (треугольник) в неоновом стиле.
    /// Нечётные — голубые, чётные — розовые.
    /// </summary>
    private void CreatePoint(int index, Vector3 position, bool isCyan, bool isTop)
    {
        GameObject point = new GameObject($"Point_{index}");
        point.transform.SetParent(transform);
        point.transform.localPosition = position;

        SpriteRenderer sr = point.AddComponent<SpriteRenderer>();

        // Выбираем спрайт: голубой или розовый, вверх или вниз
        if (isTop)
            sr.sprite = isCyan ? cyanTriangleDown : pinkTriangleDown;
        else
            sr.sprite = isCyan ? cyanTriangleUp : pinkTriangleUp;

        sr.color = Color.white;
        sr.sortingOrder = 1;

        // Масштаб треугольника
        float scaleX = PointWidth;
        float scaleY = PointHeight / (256f / 64f);
        point.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // Коллайдер для кликов
        BoxCollider2D col = point.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.9f);
        col.offset = isTop ? new Vector2(0f, -0.3f) : new Vector2(0f, 0.3f);

        // Компонент логики пункта
        BoardPoint bp = point.AddComponent<BoardPoint>();
        bp.Initialize(index);

        // Сохраняем данные
        pointPositions[index] = position;
        pointIsTop[index] = isTop;
        boardPoints[index] = bp;

        // Добавляем номер пункта (отключено)
        // CreatePointNumber(index, position, isTop);
    }

    /// <summary>
    /// Создаёт номер пункта рядом с основанием треугольника.
    /// Приглушённый цвет, чтобы не отвлекать от игры.
    /// </summary>
    private void CreatePointNumber(int index, Vector3 position, bool isTop)
    {
        GameObject label = new GameObject($"Number_{index}");
        label.transform.SetParent(transform);

        float numberY = isTop ? position.y + 0.18f : position.y - 0.18f;
        label.transform.localPosition = new Vector3(position.x, numberY, -0.01f);

        TextMesh tm = label.AddComponent<TextMesh>();
        tm.text = index.ToString();
        tm.fontSize = 24;
        tm.characterSize = 0.10f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = numberColor;
        tm.fontStyle = FontStyle.Bold;

        MeshRenderer mr = label.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 3;
    }

    // ========================= ДЕКОРАТИВНЫЕ ЭЛЕМЕНТЫ =========================

    /// <summary>
    /// Создаёт виньетку по краям доски для ощущения глубины и CRT-эффекта.
    /// </summary>
    private void CreateEdgeVignette()
    {
        Sprite glowSprite = TextureGenerator.CreateRectSprite();
        Color vigColor = new Color(0f, 0f, 0f, 0.25f);

        float hw = BoardWidth / 2f;
        float hh = BoardHeight / 2f;
        float glowSize = 0.5f;

        CreateColoredRect("Vignette_Top", new Vector3(0, hh - glowSize / 2f, -0.005f),
            new Vector2(BoardWidth, glowSize), vigColor, 2, glowSprite);
        CreateColoredRect("Vignette_Bottom", new Vector3(0, -hh + glowSize / 2f, -0.005f),
            new Vector2(BoardWidth, glowSize), vigColor, 2, glowSprite);
        CreateColoredRect("Vignette_Left", new Vector3(-hw + glowSize / 2f, 0, -0.005f),
            new Vector2(glowSize, BoardHeight), vigColor, 2, glowSprite);
        CreateColoredRect("Vignette_Right", new Vector3(hw - glowSize / 2f, 0, -0.005f),
            new Vector2(glowSize, BoardHeight), vigColor, 2, glowSprite);
    }

    // ========================= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =========================

    private GameObject CreateColoredRect(string name, Vector3 position, Vector2 size,
        Color color, int sortingOrder, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return obj;
    }

    // ========================= ПОЗИЦИОНИРОВАНИЕ ФИШЕК =========================

    /// <summary>
    /// Возвращает Y-координату основания стопки фишек на указанном пункте.
    /// </summary>
    public float GetCheckerBaseY(int pointIndex)
    {
        if (pointIsTop.ContainsKey(pointIndex) && pointIsTop[pointIndex])
            return BoardHeight / 2f - BorderSize - 0.4f;
        else
            return -BoardHeight / 2f + BorderSize + 0.4f;
    }

    /// <summary>
    /// Направление наращивания стопки: -1 для верхних пунктов (вниз), +1 для нижних (вверх).
    /// </summary>
    public float GetStackDirection(int pointIndex)
    {
        if (pointIsTop.ContainsKey(pointIndex) && pointIsTop[pointIndex])
            return -1f;
        else
            return 1f;
    }

    // ========================= ОБРАБОТКА КЛИКОВ =========================

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
            if (hit.collider != null)
            {
                BoardPoint bp = hit.collider.GetComponent<BoardPoint>();
                if (bp != null)
                    bp.OnClicked();
            }
        }
    }
}

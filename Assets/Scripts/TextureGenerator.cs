using UnityEngine;

/// <summary>
/// Генератор процедурных текстур для визуального оформления нард.
/// Создаёт текстуры дерева, сукна, треугольников, фишек и кубиков.
/// Все текстуры генерируются в памяти без внешних файлов.
/// </summary>
public static class TextureGenerator
{
    // ========================= ТЕКСТУРА ДЕРЕВА =========================

    /// <summary>
    /// Создаёт текстуру дерева с продольными волокнами и лёгким шумом.
    /// Используется для рамки доски, фона панелей и кнопок.
    /// </summary>
    /// <param name="width">Ширина текстуры в пикселях</param>
    /// <param name="height">Высота текстуры в пикселях</param>
    /// <param name="baseColor">Основной цвет дерева</param>
    /// <param name="grainColor">Цвет прожилок (обычно темнее основного)</param>
    /// <param name="grainDensity">Плотность прожилок (чем больше, тем чаще)</param>
    public static Texture2D CreateWoodTexture(int width, int height, Color baseColor, Color grainColor, float grainDensity = 12f)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        // Смещение шума для уникальности каждой текстуры
        float offsetX = Random.Range(0f, 100f);
        float offsetY = Random.Range(0f, 100f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Продольные волокна — основная волна по горизонтали
                float grain = Mathf.PerlinNoise(
                    (x + offsetX) / (width / grainDensity),
                    (y + offsetY) / (height / 2f)
                );

                // Мелкий шум для естественности
                float fineNoise = Mathf.PerlinNoise(
                    (x + offsetX) * 0.3f,
                    (y + offsetY) * 0.3f
                ) * 0.15f;

                // Смешиваем основной цвет с прожилками
                float t = Mathf.Clamp01(grain * 0.6f + fineNoise);
                Color pixel = Color.Lerp(baseColor, grainColor, t);

                // Лёгкая вариация яркости для живости
                float brightness = 1f + (Mathf.PerlinNoise(x * 0.1f, y * 0.1f) - 0.5f) * 0.08f;
                pixel *= brightness;
                pixel.a = 1f;

                pixels[y * width + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // ========================= ТЕКСТУРА СУКНА =========================

    /// <summary>
    /// Создаёт текстуру сукна (ткани) с мелкой переплетённой структурой.
    /// Используется как фон игрового поля.
    /// </summary>
    /// <param name="width">Ширина текстуры</param>
    /// <param name="height">Высота текстуры</param>
    /// <param name="baseColor">Основной цвет сукна (тёмно-зелёный или тёмно-синий)</param>
    public static Texture2D CreateFeltTexture(int width, int height, Color baseColor)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float offsetX = Random.Range(0f, 100f);
        float offsetY = Random.Range(0f, 100f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Тканевая структура — мелкое переплетение
                float weave = Mathf.PerlinNoise(
                    (x + offsetX) * 0.5f,
                    (y + offsetY) * 0.5f
                );

                // Сверхмелкий шум для ворсистости
                float fuzz = Mathf.PerlinNoise(
                    (x + offsetX) * 2f,
                    (y + offsetY) * 2f
                ) * 0.06f;

                float variation = (weave - 0.5f) * 0.1f + fuzz;
                Color pixel = baseColor;
                pixel.r = Mathf.Clamp01(pixel.r + variation);
                pixel.g = Mathf.Clamp01(pixel.g + variation);
                pixel.b = Mathf.Clamp01(pixel.b + variation);
                pixel.a = 1f;

                pixels[y * width + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // ========================= СПРАЙТ ТРЕУГОЛЬНИКА =========================

    /// <summary>
    /// Создаёт спрайт равнобедренного треугольника (пункт доски).
    /// Основание внизу, остриё вверху. Заполнен указанным цветом с
    /// лёгким деревянным градиентом и антиалиасингом по краям.
    /// </summary>
    /// <param name="width">Ширина основания в пикселях</param>
    /// <param name="height">Высота треугольника в пикселях</param>
    /// <param name="color">Основной цвет заливки</param>
    /// <param name="darkerColor">Цвет для градиента по краям (создаёт объём)</param>
    public static Sprite CreateTriangleSprite(int width, int height, Color color, Color darkerColor)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float halfW = width / 2f;

        for (int y = 0; y < height; y++)
        {
            // Ширина треугольника на этой высоте (сужается к вершине)
            float progress = (float)y / height;
            float halfWidth = halfW * (1f - progress);

            for (int x = 0; x < width; x++)
            {
                float distFromCenter = Mathf.Abs(x - halfW);

                if (distFromCenter <= halfWidth)
                {
                    // Антиалиасинг — плавное затухание по краю (1 пиксель)
                    float edgeDist = halfWidth - distFromCenter;
                    float alpha = Mathf.Clamp01(edgeDist * 2f);

                    // Градиент от центра к краям — создаёт лёгкий объём
                    float centerBlend = distFromCenter / Mathf.Max(halfWidth, 0.001f);
                    Color pixel = Color.Lerp(color, darkerColor, centerBlend * 0.35f);

                    // Лёгкое затемнение к вершине
                    pixel = Color.Lerp(pixel, darkerColor, progress * 0.15f);

                    // Деревянная текстура — тонкие горизонтальные полосы
                    float grain = Mathf.PerlinNoise(x * 0.05f, y * 0.15f);
                    pixel = Color.Lerp(pixel, darkerColor, grain * 0.12f);

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

        // Пивот в середине основания (0.5, 0) — для правильного позиционирования
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), width);
    }

    // ========================= СПРАЙТ ФИШКИ =========================

    /// <summary>
    /// Создаёт объёмный спрайт фишки (шашки) с бликом, тенью и фаской.
    /// Имитирует 3D-вид в 2D через радиальные градиенты и освещение.
    /// </summary>
    /// <param name="resolution">Разрешение текстуры (квадрат)</param>
    /// <param name="baseColor">Основной цвет фишки</param>
    /// <param name="highlightColor">Цвет блика (обычно светлее основного)</param>
    /// <param name="shadowColor">Цвет тени (обычно темнее основного)</param>
    /// <param name="rimColor">Цвет ободка/фаски</param>
    public static Sprite CreateCheckerSprite(int resolution, Color baseColor, Color highlightColor,
        Color shadowColor, Color rimColor)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[resolution * resolution];
        float center = resolution / 2f;
        float radius = resolution / 2f - 2f;
        float rimWidth = resolution * 0.08f; // Ширина ободка (фаски)

        // Позиция блика — левый верхний угол (имитация света сверху-слева)
        Vector2 lightPos = new Vector2(center - radius * 0.3f, center + radius * 0.3f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist <= radius)
                {
                    // Антиалиасинг по краю
                    float edgeAlpha = Mathf.Clamp01((radius - dist) * 2f);

                    // Расстояние от центра (нормализованное 0-1)
                    float normalDist = dist / radius;

                    // Ободок (фаска) — кольцо по краю
                    float rimFactor = 0f;
                    if (dist > radius - rimWidth)
                    {
                        rimFactor = (dist - (radius - rimWidth)) / rimWidth;
                        rimFactor = Mathf.SmoothStep(0f, 1f, rimFactor);
                    }

                    // Блик — яркое пятно сверху-слева
                    float lightDist = Vector2.Distance(new Vector2(x, y), lightPos) / radius;
                    float highlightFactor = Mathf.Clamp01(1f - lightDist * 1.2f);
                    highlightFactor = highlightFactor * highlightFactor * 0.7f; // Квадратичное затухание

                    // Тень — затемнение снизу-справа
                    float shadowFactor = Mathf.Clamp01(normalDist * 0.4f + (center - y) / (radius * 3f));

                    // Собираем итоговый цвет
                    Color pixel = baseColor;

                    // Добавляем блик
                    pixel = Color.Lerp(pixel, highlightColor, highlightFactor);

                    // Добавляем тень
                    pixel = Color.Lerp(pixel, shadowColor, shadowFactor * 0.5f);

                    // Добавляем ободок
                    pixel = Color.Lerp(pixel, rimColor, rimFactor * 0.6f);

                    // Тонкая деревянная/каменная текстура
                    float noise = Mathf.PerlinNoise(x * 0.08f + 50f, y * 0.08f + 50f);
                    pixel = Color.Lerp(pixel, shadowColor, noise * 0.08f);

                    pixel.a = edgeAlpha;
                    pixels[y * resolution + x] = pixel;
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }

    // ========================= ТЕНЬ ПОД ФИШКОЙ =========================

    /// <summary>
    /// Создаёт мягкую тень-эллипс для размещения под фишкой.
    /// Полупрозрачная, с плавным затуханием к краям.
    /// </summary>
    /// <param name="resolution">Разрешение текстуры</param>
    /// <param name="opacity">Максимальная непрозрачность тени (0-1)</param>
    public static Sprite CreateShadowSprite(int resolution, float opacity = 0.35f)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];
        float center = resolution / 2f;
        float radius = resolution / 2f - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    // Плавное гауссово затухание от центра
                    float t = dist / radius;
                    float alpha = (1f - t * t) * opacity;
                    pixels[y * resolution + x] = new Color(0f, 0f, 0f, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }

    // ========================= СПРАЙТ ГРАНИ КУБИКА =========================

    /// <summary>
    /// Создаёт спрайт одной грани кубика с закруглёнными углами и точками.
    /// Имитирует деревянный кубик с углублёнными точками.
    /// </summary>
    /// <param name="resolution">Размер текстуры (квадрат)</param>
    /// <param name="value">Значение грани (1-6)</param>
    /// <param name="faceColor">Цвет поверхности кубика</param>
    /// <param name="dotColor">Цвет точек</param>
    /// <param name="cornerRadius">Радиус закругления углов (в пикселях)</param>
    public static Sprite CreateDiceFaceSprite(int resolution, int value, Color faceColor, Color dotColor,
        float cornerRadius = 0.15f)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];
        float cr = resolution * cornerRadius; // Радиус скругления в пикселях
        float dotRadius = resolution * 0.09f; // Радиус точки

        // Позиции точек (нормализованные 0-1 координаты)
        Vector2[] dotPositions = GetDiceDotPositions(value);

        // Смещение для шума дерева
        float noiseOffset = value * 13.7f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Проверяем, попадает ли пиксель в прямоугольник со скруглёнными углами
                float alpha = RoundedRectAlpha(x, y, resolution, resolution, cr);

                if (alpha > 0f)
                {
                    // Базовый цвет грани с деревянной текстурой
                    float woodGrain = Mathf.PerlinNoise(
                        (x + noiseOffset) * 0.06f,
                        (y + noiseOffset) * 0.12f
                    );
                    Color pixel = Color.Lerp(faceColor, faceColor * 0.85f, woodGrain * 0.3f);

                    // Лёгкий 3D-эффект: светлее сверху, темнее снизу
                    float vertGrad = (float)y / resolution;
                    pixel = Color.Lerp(pixel * 1.1f, pixel * 0.9f, vertGrad);

                    // Проверяем, попадает ли пиксель в точку
                    bool inDot = false;
                    float dotEdgeDist = float.MaxValue;
                    foreach (Vector2 dotPos in dotPositions)
                    {
                        float dotX = dotPos.x * resolution;
                        float dotY = dotPos.y * resolution;
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(dotX, dotY));

                        if (dist < dotRadius + 1f) // +1 для антиалиасинга
                        {
                            dotEdgeDist = Mathf.Min(dotEdgeDist, dist);
                            if (dist <= dotRadius)
                                inDot = true;
                        }
                    }

                    if (inDot)
                    {
                        // Точка с углублением (тень по краям, светлее в центре)
                        float dotBlend = Mathf.Clamp01((dotRadius - dotEdgeDist) / dotRadius);
                        Color dot = Color.Lerp(dotColor * 0.7f, dotColor, dotBlend * 0.5f);
                        // Антиалиасинг края точки
                        float dotAA = Mathf.Clamp01((dotRadius - dotEdgeDist) * 2f);
                        pixel = Color.Lerp(pixel, dot, dotAA);
                    }
                    else if (dotEdgeDist < dotRadius + 1f)
                    {
                        // Антиалиасинг за краем точки
                        float dotAA = Mathf.Clamp01(dotEdgeDist - dotRadius);
                        pixel = Color.Lerp(dotColor * 0.7f, pixel, dotAA);
                    }

                    pixel.a = alpha;
                    pixels[y * resolution + x] = pixel;
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }

    // ========================= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =========================

    /// <summary>
    /// Возвращает нормализованные позиции точек для грани кубика (1-6).
    /// Координаты в диапазоне 0-1, где (0,0) — левый нижний угол.
    /// </summary>
    private static Vector2[] GetDiceDotPositions(int value)
    {
        float L = 0.27f; // Левая колонка
        float C = 0.50f; // Центр
        float R = 0.73f; // Правая колонка
        float T = 0.73f; // Верхний ряд
        float M = 0.50f; // Средний ряд
        float B = 0.27f; // Нижний ряд

        switch (value)
        {
            case 1: return new[] { new Vector2(C, M) };
            case 2: return new[] { new Vector2(L, T), new Vector2(R, B) };
            case 3: return new[] { new Vector2(L, T), new Vector2(C, M), new Vector2(R, B) };
            case 4: return new[] { new Vector2(L, T), new Vector2(R, T), new Vector2(L, B), new Vector2(R, B) };
            case 5: return new[] { new Vector2(L, T), new Vector2(R, T), new Vector2(C, M), new Vector2(L, B), new Vector2(R, B) };
            case 6: return new[] { new Vector2(L, T), new Vector2(R, T), new Vector2(L, M), new Vector2(R, M), new Vector2(L, B), new Vector2(R, B) };
            default: return new[] { new Vector2(C, M) };
        }
    }

    /// <summary>
    /// Вычисляет альфа-канал для пикселя в прямоугольнике со скруглёнными углами.
    /// Возвращает 1 внутри, 0 снаружи, промежуточные значения на краю (антиалиасинг).
    /// </summary>
    private static float RoundedRectAlpha(int x, int y, int width, int height, float cornerRadius)
    {
        float margin = 2f; // Отступ от края текстуры

        // Ближайшая точка скруглённого прямоугольника
        float px = Mathf.Clamp(x, margin + cornerRadius, width - margin - cornerRadius);
        float py = Mathf.Clamp(y, margin + cornerRadius, height - margin - cornerRadius);

        // Если мы в угловой зоне — считаем расстояние до скруглённого угла
        bool inCorner = (x < margin + cornerRadius || x > width - margin - cornerRadius) &&
                        (y < margin + cornerRadius || y > height - margin - cornerRadius);

        if (inCorner)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(px, py));
            return Mathf.Clamp01((cornerRadius - dist) * 2f);
        }

        // Проверяем, внутри ли основного прямоугольника
        if (x >= margin && x < width - margin && y >= margin && y < height - margin)
        {
            // Антиалиасинг по прямым краям
            float edgeDist = Mathf.Min(
                Mathf.Min(x - margin, width - margin - x),
                Mathf.Min(y - margin, height - margin - y)
            );
            return Mathf.Clamp01(edgeDist * 2f);
        }

        return 0f;
    }

    // ========================= СПРАЙТ ПОДСВЕТКИ ПУНКТА =========================

    /// <summary>
    /// Создаёт полупрозрачный круглый маркер для подсветки доступных ходов.
    /// Мягкие края с плавным затуханием.
    /// </summary>
    /// <param name="resolution">Разрешение текстуры</param>
    /// <param name="color">Цвет маркера (зелёный для доступных ходов, жёлтый для выбранного)</param>
    public static Sprite CreateHighlightMarker(int resolution, Color color)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];
        float center = resolution / 2f;
        float radius = resolution / 2f - 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float t = dist / radius;
                    // Яркий центр, плавное затухание к краям
                    float alpha = (1f - t * t) * color.a;
                    pixels[y * resolution + x] = new Color(color.r, color.g, color.b, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }

    // ========================= СПРАЙТ СВЕЧЕНИЯ =========================

    /// <summary>
    /// Создаёт спрайт мягкого свечения для выделения фишки.
    /// Радиальный градиент от яркого центра к полной прозрачности.
    /// </summary>
    public static Sprite CreateGlowSprite(int resolution, Color color)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];
        float center = resolution / 2f;
        float radius = resolution / 2f - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float t = dist / radius;
                    // Кубическое затухание для мягкого свечения
                    float alpha = (1f - t) * (1f - t) * color.a;
                    pixels[y * resolution + x] = new Color(color.r, color.g, color.b, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }

    // ========================= ПРОСТЫЕ ФОРМЫ =========================

    /// <summary>
    /// Создаёт простой белый прямоугольный спрайт для тонирования через SpriteRenderer.color
    /// </summary>
    public static Sprite CreateRectSprite(int size = 4)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>
    /// Создаёт простой круглый спрайт с гладкими краями.
    /// </summary>
    public static Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.filterMode = FilterMode.Bilinear;

        float center = resolution / 2f;
        float radius = resolution / 2f - 1;
        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) * 2f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }

    // ========================= НЕОНОВАЯ ФИШКА =========================

    /// <summary>
    /// Создаёт фишку в неоновом стиле: тёмная заливка + яркий неоновый ободок.
    /// Имитирует светящийся диск из ретро-игровых автоматов.
    /// </summary>
    /// <param name="resolution">Разрешение текстуры (квадрат)</param>
    /// <param name="fillColor">Тёмный цвет заливки</param>
    /// <param name="rimColor">Цвет неонового ободка</param>
    /// <param name="highlightColor">Цвет блика (светлее заливки)</param>
    public static Sprite CreateNeonCheckerSprite(int resolution, Color fillColor, Color rimColor, Color highlightColor)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[resolution * resolution];
        float center = resolution / 2f;
        float radius = resolution / 2f - 2f;
        float rimWidth = resolution * 0.12f;     // Ширина неонового ободка
        float innerRimWidth = resolution * 0.06f; // Тонкая внутренняя линия

        // Позиция блика — левый верхний угол
        Vector2 lightPos = new Vector2(center - radius * 0.35f, center + radius * 0.35f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist <= radius)
                {
                    // Антиалиасинг по краю
                    float edgeAlpha = Mathf.Clamp01((radius - dist) * 2f);

                    Color pixel;

                    if (dist > radius - rimWidth)
                    {
                        // Неоновый ободок — яркий цвет с bloom-эффектом
                        float rimFactor = (dist - (radius - rimWidth)) / rimWidth;
                        rimFactor = Mathf.SmoothStep(0f, 1f, rimFactor);

                        // Несколько слоёв для bloom
                        Color outerGlow = new Color(rimColor.r, rimColor.g, rimColor.b, 0.6f);
                        pixel = Color.Lerp(fillColor, rimColor, rimFactor);
                    }
                    else if (dist > radius - rimWidth - innerRimWidth)
                    {
                        // Тонкая внутренняя линия ободка
                        float innerFactor = (dist - (radius - rimWidth - innerRimWidth)) / innerRimWidth;
                        Color innerRim = new Color(rimColor.r * 0.6f, rimColor.g * 0.6f, rimColor.b * 0.6f);
                        pixel = Color.Lerp(fillColor, innerRim, innerFactor * 0.5f);
                    }
                    else
                    {
                        // Тёмная заливка с лёгким блюром
                        pixel = fillColor;

                        // Блик сверху-слева
                        float lightDist = Vector2.Distance(new Vector2(x, y), lightPos) / radius;
                        float highlightFactor = Mathf.Clamp01(1f - lightDist * 1.5f);
                        highlightFactor = highlightFactor * highlightFactor * 0.4f;
                        pixel = Color.Lerp(pixel, highlightColor, highlightFactor);

                        // Лёгкий шум для текстуры
                        float noise = Mathf.PerlinNoise(x * 0.1f + 77f, y * 0.1f + 77f) * 0.04f;
                        pixel.r = Mathf.Clamp01(pixel.r + noise);
                        pixel.g = Mathf.Clamp01(pixel.g + noise);
                        pixel.b = Mathf.Clamp01(pixel.b + noise);
                    }

                    pixel.a = edgeAlpha;
                    pixels[y * resolution + x] = pixel;
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }

    // ========================= ПИКСЕЛЬНЫЙ КУБИК (BALATRO) =========================

    /// <summary>
    /// Создаёт пиксельный кубик в стиле ретро-игровых автоматов.
    /// Тёмный фон, неоновые точки, пиксельные края.
    /// </summary>
    /// <param name="resolution">Размер текстуры</param>
    /// <param name="value">Значение грани (1-6)</param>
    /// <param name="bgColor">Цвет фона кубика</param>
    /// <param name="dotColor">Цвет неоновых точек</param>
    public static Sprite CreatePixelDiceFaceSprite(int resolution, int value, Color bgColor, Color dotColor)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        tex.filterMode = FilterMode.Point; // Пиксельная фильтрация!
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[resolution * resolution];
        float cr = resolution * 0.12f; // Радиус скругления углов
        float dotRadius = resolution * 0.10f; // Радиус точки

        // Позиции точек
        Vector2[] dotPositions = GetDiceDotPositions(value);

        // Цвет рамки кубика — неоновый
        Color borderColor = new Color(dotColor.r * 0.7f, dotColor.g * 0.7f, dotColor.b * 0.7f, 1f);
        float borderThick = resolution * 0.06f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float alpha = RoundedRectAlpha(x, y, resolution, resolution, cr);

                if (alpha > 0f)
                {
                    // Проверяем, на рамке ли мы
                    float innerAlpha = RoundedRectAlpha(x, y, resolution, resolution, cr);
                    bool onBorder = (x < borderThick || x > resolution - borderThick ||
                                    y < borderThick || y > resolution - borderThick);

                    Color pixel;

                    if (onBorder)
                    {
                        // Неоновая рамка
                        pixel = borderColor;
                    }
                    else
                    {
                        // Тёмный фон с лёгким градиентом
                        float vertGrad = (float)y / resolution;
                        pixel = Color.Lerp(bgColor * 1.1f, bgColor * 0.9f, vertGrad);
                    }

                    // Проверяем точки
                    bool inDot = false;
                    float minDotDist = float.MaxValue;

                    foreach (Vector2 dotPos in dotPositions)
                    {
                        float dotX = dotPos.x * resolution;
                        float dotY = dotPos.y * resolution;
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(dotX, dotY));
                        minDotDist = Mathf.Min(minDotDist, dist);
                        if (dist <= dotRadius) inDot = true;
                    }

                    if (inDot)
                    {
                        // Неоновая точка с bloom-эффектом
                        float dotBlend = Mathf.Clamp01((dotRadius - minDotDist) / dotRadius);
                        // Центр точки ярче
                        Color dotCenter = new Color(
                            Mathf.Min(dotColor.r * 1.5f, 1f),
                            Mathf.Min(dotColor.g * 1.5f, 1f),
                            Mathf.Min(dotColor.b * 1.5f, 1f)
                        );
                        pixel = Color.Lerp(dotColor, dotCenter, dotBlend);
                    }
                    else if (minDotDist < dotRadius + dotRadius * 0.5f)
                    {
                        // Ореол вокруг точки (bloom)
                        float glowFactor = 1f - (minDotDist - dotRadius) / (dotRadius * 0.5f);
                        glowFactor = Mathf.Clamp01(glowFactor) * 0.3f;
                        pixel = Color.Lerp(pixel, dotColor, glowFactor);
                    }

                    pixel.a = alpha;
                    pixels[y * resolution + x] = pixel;
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }
}

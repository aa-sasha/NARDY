# NARDY — Отчёт о редизайне в стиле Balatro

## Статус: В ПРОЦЕССЕ
**Дата:** 2026-02-19  
**Проект:** `D:\NARDY\NARDY` (Unity 6.3 LTS)

---

## Что было сделано

### 1. Откат изменений стопок фишек
**Файл:** `GameManager.cs`

- Удалён метод `CalcCompressedOffset` (сложное двухпроходное сжатие)
- Удалено поле `maxStackHeight`
- `UpdateCheckerVisuals` упрощён до простой расстановки с `StackOffset`
- Сигнатура `CalcCheckerPos` изменена: убран параметр `totalOnPoint` (потом добавлен обратно — см. ниже)

---

### 2. Новые файлы

#### `BalatroEffects.cs` (НОВЫЙ)
- Singleton-компонент для CRT-эффектов
- **Сканлинии** — полупрозрачные горизонтальные полосы через весь экран
- **Виньетка** — затемнение по краям экрана
- **Пульсация** — синусоидальное мерцание яркости сканлиний
- **ShakeCamera(float intensity)** — тряска камеры (вызывается при битье фишки и победе)
- Рендерится через UI Canvas + RawImage + процедурные Texture2D (без Post Processing пакета)
- Автоматически создаётся в `GameManager.Start()` и `MainMenuManager.Start()` и `BoardSetup.Start()`

#### `NeonButtonPulse.cs` (НОВЫЙ)
- Компонент для кнопок: пульсация яркости + увеличение при наведении + pop-анимация при клике
- Автоматически добавляется к каждой кнопке через `CreateStyledButton()` в GameManager и MainMenuManager

---

### 3. Переписанные файлы

#### `BoardSetup.cs` (ПОЛНОСТЬЮ ПЕРЕПИСАН)
Старый стиль: дерево + сукно → **Новый стиль: тёмный неон Balatro**

**Цветовая палитра:**
- Фон: `#0a0a14` (тёмно-синий/чёрный)
- Треугольники нечётные: неоново-голубой `#00f0ff` (cyan)
- Треугольники чётные: неоново-розовый `#ff00c8` (magenta)
- Рамка: тёмно-фиолетовая с многослойным фиолетовым свечением (bloom-эффект)
- Бар: тёмный фон + золотые вертикальные линии
- Нумерация пунктов: **ОТКЛЮЧЕНА** (закомментирован вызов `CreatePointNumber`)

**Методы:**
- `CreateNeonTriangleSprite` — пиксельный треугольник с тёмной заливкой и ярким неоновым контуром
- `GetCheckerBaseY(pointIndex)` — базовая Y-позиция для стопки фишек
- `GetStackDirection(pointIndex)` — направление стопки (+1 вверх для нижних, -1 вниз для верхних)
- `PointPositions` — словарь X-позиций пунктов

#### `CheckerPiece.cs` (ПОЛНОСТЬЮ ПЕРЕПИСАН)
- **Выделение:** неоновое розовое свечение для белых, голубое для чёрных (вместо золотого кольца)
- **PopOnSelect:** эластичная анимация 1.0 → 1.25 → 0.92 → 1.0 при выборе
- **MoveToPosition:** плавное движение по дуге (без изменений)
- **Flash:** неоновая вспышка при битье + вызов `BalatroEffects.Instance.ShakeCamera(0.05f)`
- **Пульсация свечения:** синусоидальное мерцание glow-ободка

#### `TextureGenerator.cs` (ДОБАВЛЕНЫ МЕТОДЫ)
Два новых метода в конце файла:

```csharp
// Строка ~613
public static Sprite CreateNeonCheckerSprite(int resolution, Color fillColor, Color rimColor, Color highlightColor)
// Тёмная заливка + яркий неоновый ободок с bloom-эффектом + блик + шум

// Строка ~702  
public static Sprite CreatePixelDiceFaceSprite(int resolution, int value, Color bgColor, Color dotColor)
// Пиксельный кубик (FilterMode.Point), тёмный фон, неоновые точки с bloom
```

---

### 4. Изменённые файлы

#### `GameManager.cs`

**Цветовая палитра фишек (строки ~97-113):**
```csharp
// Белые фишки
whiteCheckerBase = new Color(1.0f, 0.97f, 0.91f)      // Слоновая кость
whiteCheckerHighlight = new Color(1f, 1f, 1f)           // Яркий блик
whiteCheckerShadow = new Color(0.78f, 0.60f, 0.75f)    // Тень с розовым
whiteCheckerRim = new Color(1.0f, 0.45f, 0.85f)        // Неоново-розовый ободок

// Чёрные фишки
blackCheckerBase = new Color(0.10f, 0.04f, 0.17f)      // Тёмно-фиолетовый
blackCheckerHighlight = new Color(0.25f, 0.55f, 0.90f) // Неоново-синий блик
blackCheckerShadow = new Color(0.03f, 0.01f, 0.07f)    // Глубокая тень
blackCheckerRim = new Color(0.0f, 0.94f, 1.0f)         // Неоново-голубой ободок

// Подсветка ходов
highlightMoveColor = new Color(0.0f, 1.0f, 0.5f, 0.85f)      // Неоново-зелёный
highlightSelectedColor = new Color(1.0f, 0.0f, 0.78f, 0.85f) // Неоново-розовый
```

**Размер фишек (строки ~120-121):**
```csharp
private const float CheckerSize = 1.0f;   // Размер фишки в мировых единицах
private const float StackOffset = 0.20f;  // Базовое расстояние между фишками в стопке
```

**CreateCheckerSprites (~строка 194):**
```csharp
// Теперь использует CreateNeonCheckerSprite вместо CreateCheckerSprite
whiteCheckerSprite = TextureGenerator.CreateNeonCheckerSprite(128, whiteCheckerBase, whiteCheckerRim, whiteCheckerHighlight);
blackCheckerSprite = TextureGenerator.CreateNeonCheckerSprite(128, blackCheckerBase, blackCheckerRim, blackCheckerHighlight);
```

**Ободок фишки (~строка 291):**
```csharp
// Неоновые цвета вместо деревянных
outlineSr.color = isWhite
    ? new Color(1.0f, 0.45f, 0.85f, 0.35f)  // Неоново-розовый
    : new Color(0.0f, 0.94f, 1.0f, 0.35f);  // Неоново-голубой
```

**CalcCheckerPos (~строка 886):**
```csharp
// Сигнатура: CalcCheckerPos(int pointIndex, int stackIndex, int totalOnPoint = 1)
// Использует CalcDynamicOffset для умного сжатия больших стопок
```

**CalcDynamicOffset (~строка 917):**
```csharp
// Логика: фиксированный StackOffset если стопка вмещается в 80% высоты
// Сжатие только если не вмещается (при 19+ фишках на одном пункте)
private float CalcDynamicOffset(int totalOnPoint, float targetFill = 0.80f)
{
    if (totalOnPoint <= 1) return StackOffset;
    float maxHeight = 4.5f * targetFill; // = 3.6 (80% от половины доски 4.5)
    float neededHeight = (totalOnPoint - 1) * StackOffset;
    if (neededHeight <= maxHeight) return StackOffset; // не растягиваем
    return maxHeight / (totalOnPoint - 1); // сжимаем
}
```

**UI цветовая палитра (~строка 1080):**
```csharp
uiPanelColor    = new Color(0.04f, 0.03f, 0.10f, 0.95f)  // Тёмно-фиолетовый
uiButtonColor   = new Color(0.08f, 0.05f, 0.20f, 0.95f)  // Фиолетовая кнопка
uiGoldText      = new Color(0.0f, 0.94f, 1.0f)            // Неоново-голубой
uiLightText     = new Color(0.85f, 0.80f, 0.95f)          // Светло-фиолетовый
uiAccentGreen   = new Color(0.0f, 1.0f, 0.5f, 0.90f)     // Неоново-зелёный
uiOverlayColor  = new Color(0.02f, 0.01f, 0.06f, 0.85f)  // Тёмный оверлей
```

**Тряска камеры при победе (~строка 822):**
```csharp
if (BalatroEffects.Instance != null)
    BalatroEffects.Instance.ShakeCamera(0.15f);
```

#### `Dice.cs`
```csharp
// Цвета кубиков (строки ~43-44)
diceFaceColor = new Color(0.06f, 0.04f, 0.12f)  // Тёмно-фиолетовый фон
diceDotColor  = new Color(1.0f, 0.0f, 0.78f)    // Неоново-розовые точки

// CreateFaceSprites теперь использует CreatePixelDiceFaceSprite
// resolution = 64 (пиксельный вид)
faceSprites[i] = TextureGenerator.CreatePixelDiceFaceSprite(64, i + 1, diceFaceColor, diceDotColor);
```

#### `MainMenuManager.cs`
```csharp
// Цветовая палитра (строки ~19-31)
bgColor       = new Color(0.04f, 0.03f, 0.08f)   // Тёмно-синий фон
panelColor    = new Color(0.06f, 0.04f, 0.14f)   // Тёмно-фиолетовая панель
goldText      = new Color(0.0f, 0.94f, 1.0f)     // Неоново-голубой заголовок
activeColor   = new Color(0.0f, 1.0f, 0.5f)      // Неоново-зелёный (активная сложность)
startBtnColor = new Color(1.0f, 0.0f, 0.78f)     // Неоново-розовая кнопка Start
goldAccent    = new Color(0.4f, 0.0f, 0.8f, 0.6f) // Фиолетовые декоративные линии

// Заголовок: "NARDY" (64pt, без пробелов)
// Подзаголовок: "[ LONG BACKGAMMON ]" (18pt)
// BalatroEffects создаётся в Start()
// NeonButtonPulse добавляется к каждой кнопке
```

---

## Текущее состояние (визуально)

На скриншоте из Unity видно:
- ✅ Тёмный фон (#0a0a14)
- ✅ Неоновые голубые и розовые треугольники
- ✅ Фиолетовая рамка с свечением
- ✅ Нумерация пунктов **убрана**
- ✅ Белые фишки на пункте 1 (правый нижний) — розовый неоновый ободок
- ✅ Чёрные фишки на пункте 13 (левый верхний) — голубой неоновый ободок
- ✅ CheckerSize = 1.0f (крупные фишки)
- ✅ Неоновый UI (голубой текст, кнопки Roll Dice / End Turn)

---

## Что ещё можно сделать

### Высокий приоритет
- [ ] **Настройка StackOffset** — пользователь хочет стопку на 80% высоты доски. Сейчас при 15 фишках стопка ~62%. Нужно либо убрать ограничение в `CalcDynamicOffset` (всегда растягивать до 80%), либо увеличить `StackOffset` до `~0.26f`
- [ ] **Проверить анимации** в игровом процессе (ход, битьё, снятие)
- [ ] **Кубики** — проверить что `CreatePixelDiceFaceSprite` работает корректно в игре

### Средний приоритет  
- [ ] **Звуки** — синтвейв/ретро-электронные звуки для ходов, бросков кубиков, победы
- [ ] **SwayAnimation** — медленное покачивание статичных элементов (из `BalatroEffects`)
- [ ] **PopAnimation** — применить к другим UI элементам

### Низкий приоритет
- [ ] **Pixel Perfect Camera** — настроить в Unity для чёткого пиксельного вида
- [ ] **Настройки** — переключатель CRT-эффектов
- [ ] **Шрифт** — пиксельный шрифт вместо LegacyRuntime.ttf

---

## Структура файлов

```
D:\NARDY\NARDY\Assets\Scripts\
├── AIPlayer.cs           — без изменений
├── BalatroEffects.cs     — НОВЫЙ (CRT-эффекты, тряска камеры)
├── BoardPoint.cs         — без изменений (поддерживает произвольные цвета подсветки)
├── BoardSetup.cs         — ПЕРЕПИСАН (неоновый стиль, нумерация отключена)
├── CheckerPiece.cs       — ПЕРЕПИСАН (неоновое свечение, pop-анимация)
├── Dice.cs               — изменён (пиксельные кубики, неоновые точки)
├── GameManager.cs        — изменён (размеры, цвета, CalcDynamicOffset)
├── MainMenuManager.cs    — изменён (Balatro-палитра, NeonButtonPulse)
├── NeonButtonPulse.cs    — НОВЫЙ (пульсация кнопок)
└── TextureGenerator.cs   — добавлены CreateNeonCheckerSprite, CreatePixelDiceFaceSprite
```

---

## Важные константы

```csharp
// GameManager.cs
CheckerSize = 1.0f      // Размер фишки (мировые единицы)
StackOffset = 0.20f     // Базовый offset между фишками
BoardHeight = 9f        // (в BoardSetup) высота доски
BorderSize  = 0.5f      // (в BoardSetup) толщина рамки
BarWidth    = 0.8f      // (в BoardSetup) ширина бара

// Половина доски = 4.5f
// 80% от половины = 3.6f
// При 15 фишках и StackOffset=0.20: высота = 14*0.20 = 2.8 (62%)
// Чтобы было 80%: нужен StackOffset = 3.6/14 ≈ 0.257f
```

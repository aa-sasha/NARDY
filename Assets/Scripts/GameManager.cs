using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Главный контроллер игры «Длинные нарды».
/// Управляет позициями фишек, ходами, валидацией правил, AI и UI.
///
/// Система прогресса:
///   Белые: пункт 1 (прогресс 1) → пункт 24 (прогресс 24) → снятие (25)
///   Чёрные: пункт 13 (прогресс 1) → пункт 24 (прогресс 12) → пункт 1 (прогресс 13) → пункт 12 (прогресс 24) → снятие (25)
///
/// Специальные позиции:
///   0 = бар (сбитая фишка)
///   25 = снята с доски (в «дом»)
/// </summary>
public class GameManager : MonoBehaviour
{
    // ========================= ПЕРЕЧИСЛЕНИЯ И СТРУКТУРЫ =========================

    public enum Difficulty { Easy, Medium, Hard }

    public struct MoveInfo
    {
        public int fromPoint;
        public int toPoint;
        public int diceValue;

        public MoveInfo(int from, int to, int dice)
        {
            fromPoint = from;
            toPoint = to;
            diceValue = dice;
        }

        public override string ToString()
        {
            string dest = toPoint == 25 ? "OFF" : toPoint.ToString();
            return $"{fromPoint}→{dest} (dice:{diceValue})";
        }
    }

    // ========================= СИНГЛТОН =========================

    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Устанавливается в MainMenuManager перед загрузкой игровой сцены.
    /// </summary>
    public static Difficulty SelectedDifficulty { get; set; } = Difficulty.Medium;

    [Header("Game State")]
    [SerializeField] private List<int> whitePositions = new List<int>();
    [SerializeField] private List<int> blackPositions = new List<int>();
    [SerializeField] private int currentPlayer = 0; // 0 = белые, 1 = чёрные

    [Header("AI")]
    [SerializeField] private Difficulty currentDifficulty = Difficulty.Hard;
    private AIPlayer aiPlayer;
    private bool isAIPlaying;

    [Header("References")]
    [SerializeField] private BoardSetup boardSetup;
    [SerializeField] private Dice dice;

    [Header("Prefabs")]
    [SerializeField] private GameObject whiteCheckerPrefab;
    [SerializeField] private GameObject blackCheckerPrefab;

    [Header("UI")]
    [SerializeField] private Text currentPlayerText;
    [SerializeField] private Text diceResultText;

    [Header("Настройки визуала")]
    [SerializeField] private float maxStackHeight = 3.0f; // Максимальная высота стопки фишек (вся высота пункта)

    // ========================= ПАНЕЛЬ ОКОНЧАНИЯ ИГРЫ =========================

    private GameObject gameOverPanel;
    private Text gameOverText;
    private Text gameOverStatsText;

    // ========================= СТАТИСТИКА ПАРТИИ =========================

    private int moveCount;
    private float gameStartTime;

    // ========================= СОСТОЯНИЕ ВЫБОРА =========================

    private int selectedPointIndex = -1;
    private CheckerPiece selectedCheckerPiece; // Визуально выделенная фишка
    private bool isGameOver;
    private bool isAnimating; // Флаг: анимация хода активна

    // ========================= ЦВЕТОВАЯ ПАЛИТРА ФИШЕК =========================

    // Белые фишки — слоновая кость с неоново-розовым отливом
    private readonly Color whiteCheckerBase = new Color(1.0f, 0.97f, 0.91f);         // Слоновая кость
    private readonly Color whiteCheckerHighlight = new Color(1f, 1f, 1f);             // Яркий блик
    private readonly Color whiteCheckerShadow = new Color(0.78f, 0.60f, 0.75f);      // Тень с розовым
    private readonly Color whiteCheckerRim = new Color(1.0f, 0.45f, 0.85f);          // Неоново-розовый ободок

    // Чёрные фишки — тёмно-фиолетовые с неоново-синим отливом
    private readonly Color blackCheckerBase = new Color(0.10f, 0.04f, 0.17f);        // Тёмно-фиолетовый
    private readonly Color blackCheckerHighlight = new Color(0.25f, 0.55f, 0.90f);   // Неоново-синий блик
    private readonly Color blackCheckerShadow = new Color(0.03f, 0.01f, 0.07f);      // Глубокая тень
    private readonly Color blackCheckerRim = new Color(0.0f, 0.94f, 1.0f);           // Неоново-голубой ободок

    // Подсветка ходов — неоновые цвета
    private readonly Color highlightMoveColor = new Color(0.0f, 1.0f, 0.5f, 0.85f);   // Неоново-зелёный
    private readonly Color highlightSelectedColor = new Color(1.0f, 0.0f, 0.78f, 0.85f); // Неоново-розовый

    // ========================= ИГРОВЫЕ ОБЪЕКТЫ =========================

    private List<GameObject> whiteCheckers = new List<GameObject>();
    private List<GameObject> blackCheckers = new List<GameObject>();

    private const float CheckerSize = 1.0f;     // Размер фишки
    private const float StackOffset = 0.20f;    // Расстояние между фишками в стопке
    private const int OFF_BOARD = 25;
    private const int BAR = 0;

    // Кэш спрайтов (создаются один раз)
    private Sprite whiteCheckerSprite;
    private Sprite blackCheckerSprite;
    private Sprite shadowSprite;
    private Sprite glowSprite;

    // UI — индикатор текущего игрока
    private Image whitePlayerIcon;
    private Image blackPlayerIcon;
    private Text moveCountText;
    private Text timerText;

    // ========================= ПУБЛИЧНЫЕ СВОЙСТВА =========================

    public List<int> WhitePositions => whitePositions;
    public List<int> BlackPositions => blackPositions;
    public int CurrentPlayer => currentPlayer;
    public Difficulty CurrentDifficulty => currentDifficulty;
    public bool IsAIPlaying => isAIPlaying;
    public bool IsAnimating => isAnimating;

    // ========================= ЖИЗНЕННЫЙ ЦИКЛ =========================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Запускаем CRT-эффекты
        if (FindFirstObjectByType<VisualEffects>() == null)
        {
            GameObject fxObj = new GameObject("VisualEffects");
            fxObj.AddComponent<VisualEffects>();
        }

        // Генерируем процедурные спрайты фишек
        CreateCheckerSprites();

        if (boardSetup == null)
            boardSetup = FindFirstObjectByType<BoardSetup>();
        if (dice == null)
            dice = FindFirstObjectByType<Dice>();

        // Находим или добавляем AI-игрока
        aiPlayer = GetComponent<AIPlayer>();
        if (aiPlayer == null)
            aiPlayer = gameObject.AddComponent<AIPlayer>();

        // Применяем сложность из главного меню
        currentDifficulty = SelectedDifficulty;

        // Инициализация статистики
        moveCount = 0;
        gameStartTime = Time.time;

        InitializePositions();
        CreateUI();
        SpawnCheckers();
        UpdatePlayerText();
    }

    /// <summary>
    /// Создаёт процедурные спрайты для фишек в неоновом стиле.
    /// Неоновые фишки: тёмная заливка + яркий неоновый ободок.
    /// Вызывается один раз при старте — все фишки используют общие спрайты.
    /// </summary>
    private void CreateCheckerSprites()
    {
        // Белые фишки: слоновая кость с неоново-розовым ободком
        whiteCheckerSprite = TextureGenerator.CreateNeonCheckerSprite(128,
            whiteCheckerBase, whiteCheckerRim, whiteCheckerHighlight);

        // Чёрные фишки: тёмно-фиолетовые с неоново-голубым ободком
        blackCheckerSprite = TextureGenerator.CreateNeonCheckerSprite(128,
            blackCheckerBase, blackCheckerRim, blackCheckerHighlight);

        // Мягкая тень под фишками (чуть менее заметная в тёмном стиле)
        shadowSprite = TextureGenerator.CreateShadowSprite(64, 0.25f);

        // Свечение для выделения — неоново-розовое
        glowSprite = TextureGenerator.CreateGlowSprite(128, new Color(1f, 0.0f, 0.78f, 0.5f));
    }

    // ========================= УПРАВЛЕНИЕ AI =========================

    public void SetAIPlaying(bool playing)
    {
        isAIPlaying = playing;
    }

    // ========================= ИНИЦИАЛИЗАЦИЯ =========================

    /// <summary>
    /// Начальные позиции длинных нард:
    /// Белые: все 15 на пункте 1, Чёрные: все 15 на пункте 13
    /// </summary>
    private void InitializePositions()
    {
        whitePositions.Clear();
        blackPositions.Clear();
        for (int i = 0; i < 15; i++)
        {
            whitePositions.Add(1);
            blackPositions.Add(13);
        }
    }

    /// <summary>
    /// Создаёт все фишки и расставляет по доске.
    /// Каждая фишка включает: основной спрайт, тень снизу и скрытое свечение.
    /// </summary>
    public void SpawnCheckers()
    {
        foreach (var c in whiteCheckers) if (c != null) Destroy(c);
        foreach (var c in blackCheckers) if (c != null) Destroy(c);
        whiteCheckers.Clear();
        blackCheckers.Clear();

        for (int i = 0; i < whitePositions.Count; i++)
            whiteCheckers.Add(CreateChecker(true, i));

        for (int i = 0; i < blackPositions.Count; i++)
            blackCheckers.Add(CreateChecker(false, i));

        UpdateCheckerVisuals();
        Debug.Log("SpawnCheckers: 15 white on point 1, 15 black on point 13");
    }

    /// <summary>
    /// Создаёт одну фишку с объёмным спрайтом, тенью и свечением.
    /// </summary>
    private GameObject CreateChecker(bool isWhite, int index)
    {
        GameObject obj = new GameObject(isWhite ? $"WhiteChecker_{index}" : $"BlackChecker_{index}");

        // Основной спрайт фишки (объёмный, с бликами)
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = isWhite ? whiteCheckerSprite : blackCheckerSprite;
        sr.color = Color.white; // Цвет уже в текстуре
        sr.sortingOrder = 10 + index;
        obj.transform.localScale = Vector3.one * CheckerSize;

        // Тень под фишкой (дочерний объект, смещён вправо-вниз)
        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(obj.transform);
        shadow.transform.localPosition = new Vector3(0.08f, -0.1f, 0f); // Смещение тени
        shadow.transform.localScale = Vector3.one * 1.2f; // Тень чуть больше фишки

        SpriteRenderer shadowSr = shadow.AddComponent<SpriteRenderer>();
        shadowSr.sprite = shadowSprite;
        shadowSr.color = Color.white;
        shadowSr.sortingOrder = sr.sortingOrder - 2; // Под фишкой

        // Ободок фишки (тонкое кольцо, создаёт ощущение толщины)
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(obj.transform);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localScale = Vector3.one * 1.08f;

        SpriteRenderer outlineSr = outline.AddComponent<SpriteRenderer>();
        outlineSr.sprite = isWhite ? whiteCheckerSprite : blackCheckerSprite;
        // Ободок в неоновом цвете
        outlineSr.color = isWhite
            ? new Color(1.0f, 0.45f, 0.85f, 0.35f)   // Неоново-розовый ободок
            : new Color(0.0f, 0.94f, 1.0f, 0.35f);   // Неоново-голубой ободок
        outlineSr.sortingOrder = sr.sortingOrder - 1;

        // Компонент CheckerPiece — данные и анимации
        CheckerPiece piece = obj.AddComponent<CheckerPiece>();
        int point = isWhite ? whitePositions[index] : blackPositions[index];
        piece.Initialize(isWhite, point);

        return obj;
    }

    // ========================= ОБРАБОТКА КЛИКОВ =========================

    /// <summary>
    /// Вызывается из BoardPoint.OnClicked(). Главная точка входа для взаимодействия.
    /// Блокируется во время хода AI, анимации и броска кубиков.
    /// </summary>
    public void OnPointClicked(int pointIndex)
    {
        // Блокировка ввода во время анимаций и хода AI
        if (isGameOver || isAIPlaying || isAnimating) return;
        if (dice != null && dice.IsRolling) return;

        if (dice == null || !dice.HasRolled)
        {
            Debug.Log("Roll the dice first!");
            return;
        }
        if (dice.AllUsed)
        {
            Debug.Log("All dice used. Press End Turn.");
            return;
        }

        // Если уже выбрана фишка — пытаемся переместить
        if (selectedPointIndex >= 0)
        {
            // Повторный клик на тот же пункт — снять выделение
            if (pointIndex == selectedPointIndex)
            {
                DeselectChecker();
                return;
            }

            // Клик на подсвеченный пункт назначения — выполнить ход
            if (boardSetup.BoardPoints.ContainsKey(pointIndex)
                && boardSetup.BoardPoints[pointIndex].IsHighlighted)
            {
                HandleMoveToPoint(pointIndex);
                return;
            }

            // Клик на неподсвеченный пункт — снять выделение и попробовать выбрать новый
            DeselectChecker();
        }

        // Пытаемся выбрать фишку на этом пункте
        SelectChecker(pointIndex);
    }

    /// <summary>
    /// Выбирает фишку текущего игрока на указанном пункте.
    /// Визуально подсвечивает фишку и все доступные ходы.
    /// </summary>
    private void SelectChecker(int pointIndex)
    {
        bool isWhite = (currentPlayer == 0);
        List<int> positions = isWhite ? whitePositions : blackPositions;

        // Проверяем, есть ли фишка текущего игрока на этом пункте
        if (!positions.Contains(pointIndex))
        {
            Debug.Log($"No {(isWhite ? "white" : "black")} checker on point {pointIndex}");
            return;
        }

        // Фишки на баре должны ходить первыми
        if (positions.Contains(BAR) && pointIndex != BAR)
        {
            Debug.Log("You have pieces on the bar! Move them first.");
            return;
        }

        selectedPointIndex = pointIndex;

        // Подсвечиваем выбранный пункт жёлтым
        if (boardSetup.BoardPoints.ContainsKey(pointIndex))
            boardSetup.BoardPoints[pointIndex].SetHighlight(true, highlightSelectedColor);

        // Визуально выделяем саму фишку (верхнюю в стопке)
        selectedCheckerPiece = FindTopCheckerOnPoint(pointIndex, isWhite);
        if (selectedCheckerPiece != null)
            selectedCheckerPiece.Select();

        // Подсвечиваем зелёным все доступные пункты назначения
        List<int> moves = GetAvailableMoves(pointIndex);
        foreach (int dest in moves)
        {
            if (dest >= 1 && dest <= 24 && boardSetup.BoardPoints.ContainsKey(dest))
                boardSetup.BoardPoints[dest].SetHighlight(true, highlightMoveColor);
        }

        if (moves.Count == 0)
            Debug.Log($"No available moves from point {pointIndex}");
        else
            Debug.Log($"Selected point {pointIndex}. Available moves: [{string.Join(", ", moves)}]");
    }

    /// <summary>
    /// Снимает выделение: убирает подсветку пунктов и визуальное выделение фишки.
    /// </summary>
    private void DeselectChecker()
    {
        selectedPointIndex = -1;
        ClearHighlights();

        // Снимаем визуальное выделение с фишки
        if (selectedCheckerPiece != null)
        {
            selectedCheckerPiece.Deselect();
            selectedCheckerPiece = null;
        }
    }

    /// <summary>
    /// Находит верхнюю фишку указанного цвета на данном пункте.
    /// «Верхняя» = последняя по индексу в массиве (с наибольшим stackIndex).
    /// </summary>
    private CheckerPiece FindTopCheckerOnPoint(int pointIndex, bool isWhite)
    {
        List<int> positions = isWhite ? whitePositions : blackPositions;
        List<GameObject> checkers = isWhite ? whiteCheckers : blackCheckers;

        int lastIndex = -1;
        for (int i = 0; i < positions.Count; i++)
        {
            if (positions[i] == pointIndex)
                lastIndex = i;
        }

        if (lastIndex >= 0 && lastIndex < checkers.Count)
            return checkers[lastIndex].GetComponent<CheckerPiece>();
        return null;
    }

    // ========================= ЛОГИКА ХОДОВ =========================

    /// <summary>
    /// Пытается переместить из selectedPointIndex в toPoint.
    /// Вычисляет нужное значение кубика для этого хода.
    /// </summary>
    private void HandleMoveToPoint(int toPoint)
    {
        bool isWhite = (currentPlayer == 0);
        int fromPoint = selectedPointIndex;

        int neededDice = CalcDiceNeeded(fromPoint, toPoint, isWhite);

        if (neededDice <= 0 || !dice.CanUseDiceValue(neededDice))
        {
            Debug.Log($"No valid dice value for move {fromPoint} → {toPoint}");
            DeselectChecker();
            return;
        }

        TryMove(fromPoint, toPoint, neededDice);
    }

    /// <summary>
    /// Валидирует и выполняет ход. Запускает анимацию перемещения.
    /// Проверки: границы, кубик, блокировка противником, битьё.
    /// </summary>
    public bool TryMove(int fromPoint, int toPoint, int diceValueUsed)
    {
        // Блокировка во время анимации или после окончания игры
        if (isAnimating || isGameOver) return false;

        bool isWhite = (currentPlayer == 0);
        List<int> myPositions = isWhite ? whitePositions : blackPositions;
        List<int> oppPositions = isWhite ? blackPositions : whitePositions;

        // Проверка: пункт назначения в допустимых пределах
        if (toPoint != OFF_BOARD && (toPoint < 1 || toPoint > 24))
        {
            Debug.Log($"TryMove FAILED: point {toPoint} out of bounds");
            return false;
        }

        // Проверка: значение кубика доступно
        if (!dice.CanUseDiceValue(diceValueUsed))
        {
            Debug.Log($"TryMove FAILED: dice value {diceValueUsed} not available");
            return false;
        }

        // Проверка: расстояние хода соответствует кубику
        int expectedDest = CalcDestination(fromPoint, diceValueUsed, isWhite);
        if (expectedDest != toPoint)
        {
            Debug.Log($"TryMove FAILED: dice {diceValueUsed} from {fromPoint} should go to {expectedDest}, not {toPoint}");
            return false;
        }

        // Проверка: для снятия все фишки должны быть в «доме»
        if (toPoint == OFF_BOARD)
        {
            if (!CanBearOff(isWhite))
            {
                Debug.Log("TryMove FAILED: not all pieces in home quadrant, can't bear off");
                return false;
            }
        }

        // Проверка блокировки и битья
        int hitCheckerIndex = -1;
        if (toPoint != OFF_BOARD)
        {
            int oppCount = CountPiecesOnPoint(oppPositions, toPoint);
            if (oppCount >= 2)
            {
                Debug.Log($"TryMove FAILED: point {toPoint} blocked by {oppCount} opponent pieces");
                return false;
            }

            // Битьё одиночной фишки противника — отправляем на бар
            if (oppCount == 1)
            {
                hitCheckerIndex = HitOpponentPiece(toPoint, isWhite);
            }
        }

        // Выполняем ход: обновляем позицию в списке
        int checkerIndex = myPositions.IndexOf(fromPoint);
        if (checkerIndex < 0)
        {
            Debug.Log($"TryMove FAILED: no piece at point {fromPoint}");
            return false;
        }
        myPositions[checkerIndex] = toPoint;

        // Отмечаем кубик как использованный
        dice.UseDiceValue(diceValueUsed);
        moveCount++;

        // Снимаем выделение и подсветку
        DeselectChecker();

        Debug.Log($"MOVE: {(isWhite ? "White" : "Black")} {fromPoint} → {(toPoint == OFF_BOARD ? "OFF" : toPoint.ToString())} (dice: {diceValueUsed})");

        // Обновляем счётчик ходов в UI
        UpdateStatsUI();

        // Запускаем анимацию перемещения (проверка победы — после анимации)
        StartCoroutine(AnimateMoveSequence(checkerIndex, isWhite, fromPoint, toPoint, hitCheckerIndex));

        return true;
    }

    // ========================= АНИМАЦИЯ ХОДА =========================

    /// <summary>
    /// Корутина анимации хода: плавно перемещает фишку, обрабатывает битьё
    /// с вспышкой, затем обновляет все позиции и проверяет победу.
    /// </summary>
    private IEnumerator AnimateMoveSequence(int checkerIndex, bool isWhite,
        int fromPoint, int toPoint, int hitCheckerIndex)
    {
        isAnimating = true;

        List<GameObject> myCheckers = isWhite ? whiteCheckers : blackCheckers;
        List<int> myPositions = isWhite ? whitePositions : blackPositions;
        GameObject movingObj = myCheckers[checkerIndex];
        CheckerPiece movingPiece = movingObj.GetComponent<CheckerPiece>();
        SpriteRenderer movingSr = movingObj.GetComponent<SpriteRenderer>();

        // Поднимаем sorting order, чтобы фишка была поверх остальных при движении
        int originalSortOrder = movingSr != null ? movingSr.sortingOrder : 10;
        if (movingSr != null) movingSr.sortingOrder = 50;

        // Вычисляем целевую позицию с учётом стека
        Vector3 targetPos;
        if (toPoint == OFF_BOARD)
        {
            // Снятая фишка — скрываем после анимации (двигаем за экран)
            targetPos = new Vector3(8f, 0f, 0f);
        }
        else
        {
            // Считаем позицию в стопке на целевом пункте
            int stackIndex = 0;
            for (int i = 0; i < checkerIndex; i++)
                if (myPositions[i] == toPoint) stackIndex++;
            int totalOnPoint = CountPiecesOnPoint(myPositions, toPoint);
            targetPos = CalcCheckerPos(toPoint, stackIndex, totalOnPoint);
        }

        // Обновляем логическую позицию в компоненте
        movingPiece.SetPoint(toPoint);

        // Плавное перемещение фишки
        yield return movingPiece.MoveToPosition(targetPos, 0.3f);

        // Обработка битья: вспышка + перемещение сбитой фишки на бар
        if (hitCheckerIndex >= 0)
        {
            List<GameObject> oppCheckers = isWhite ? blackCheckers : whiteCheckers;
            List<int> oppPositions = isWhite ? blackPositions : whitePositions;

            if (hitCheckerIndex < oppCheckers.Count)
            {
                GameObject hitObj = oppCheckers[hitCheckerIndex];
                CheckerPiece hitPiece = hitObj.GetComponent<CheckerPiece>();

                // Вспышка — визуальный эффект удара
                yield return hitPiece.Flash();

                // Вычисляем позицию на баре
                int barStackIndex = 0;
                for (int i = 0; i < hitCheckerIndex; i++)
                    if (oppPositions[i] == BAR) barStackIndex++;
                int barTotal = CountPiecesOnPoint(oppPositions, BAR);
                Vector3 barPos = CalcCheckerPos(BAR, barStackIndex, barTotal);

                // Перемещаем сбитую фишку на бар
                hitPiece.SetPoint(BAR);
                yield return hitPiece.MoveToPosition(barPos, 0.25f);
            }
        }

        // Восстанавливаем sorting order и обновляем все позиции
        if (movingSr != null) movingSr.sortingOrder = originalSortOrder;
        UpdateCheckerVisuals();

        isAnimating = false;

        // Проверяем победу после завершения анимации
        if (CheckWin())
            yield break;

        if (dice.AllUsed)
            Debug.Log("All dice used. Press End Turn.");
    }

    /// <summary>
    /// Отправляет одиночную фишку противника на бар (позиция 0).
    /// Возвращает индекс сбитой фишки в массиве для анимации.
    /// </summary>
    private int HitOpponentPiece(int point, bool attackerIsWhite)
    {
        List<int> oppPositions = attackerIsWhite ? blackPositions : whitePositions;
        int idx = oppPositions.IndexOf(point);
        if (idx >= 0)
        {
            oppPositions[idx] = BAR;
            Debug.Log($"HIT! {(attackerIsWhite ? "Black" : "White")} piece sent to bar from point {point}");
            return idx;
        }
        return -1;
    }

    /// <summary>
    /// Возвращает все допустимые пункты назначения для фишки на fromPoint.
    /// Учитывает все доступные значения кубиков.
    /// </summary>
    public List<int> GetAvailableMoves(int fromPoint)
    {
        bool isWhite = (currentPlayer == 0);
        List<int> oppPositions = isWhite ? blackPositions : whitePositions;
        List<int> moves = new List<int>();

        foreach (int diceVal in dice.GetAvailableValues())
        {
            int dest = CalcDestination(fromPoint, diceVal, isWhite);
            if (dest == OFF_BOARD)
            {
                if (CanBearOff(isWhite))
                    moves.Add(OFF_BOARD);
                continue;
            }
            if (dest < 1 || dest > 24) continue;

            int oppCount = CountPiecesOnPoint(oppPositions, dest);
            if (oppCount >= 2) continue;

            moves.Add(dest);
        }

        return moves;
    }

    /// <summary>
    /// Возвращает все возможные ходы для указанного игрока (0=белые, 1=чёрные).
    /// Используется AI для перебора ходов.
    /// </summary>
    public List<MoveInfo> GetAllPossibleMoves(int player)
    {
        bool isWhite = (player == 0);
        List<int> myPositions = isWhite ? whitePositions : blackPositions;
        List<int> oppPositions = isWhite ? blackPositions : whitePositions;
        List<MoveInfo> allMoves = new List<MoveInfo>();

        // Уникальные занятые пункты (включая бар)
        HashSet<int> occupiedPoints = new HashSet<int>();
        foreach (int p in myPositions)
            if (p != OFF_BOARD) occupiedPoints.Add(p);

        // Если есть фишки на баре — только они могут ходить
        bool hasBarPieces = occupiedPoints.Contains(BAR);

        foreach (int fromPoint in occupiedPoints)
        {
            if (hasBarPieces && fromPoint != BAR) continue;

            foreach (int diceVal in dice.GetAvailableValues())
            {
                int dest = CalcDestination(fromPoint, diceVal, isWhite);
                if (dest == OFF_BOARD)
                {
                    if (CanBearOff(isWhite))
                        allMoves.Add(new MoveInfo(fromPoint, OFF_BOARD, diceVal));
                    continue;
                }
                if (dest < 1 || dest > 24) continue;

                int oppCount = CountPiecesOnPoint(oppPositions, dest);
                if (oppCount >= 2) continue;

                allMoves.Add(new MoveInfo(fromPoint, dest, diceVal));
            }
        }

        return allMoves;
    }

    // ========================= СИСТЕМА КООРДИНАТ =========================

    /// <summary>
    /// Преобразует пункт доски (1-24) в линейный прогресс (1-24).
    /// Белые: прогресс = пункт. Чёрные: 13→1, 14→2, ..., 1→13, ..., 12→24.
    /// </summary>
    public int GetProgress(int point, bool isWhite)
    {
        if (point == BAR || point == OFF_BOARD) return point;
        if (isWhite) return point;
        return point >= 13 ? point - 12 : point + 12;
    }

    private int GetPointFromProgress(int progress, bool isWhite)
    {
        if (progress >= OFF_BOARD) return OFF_BOARD;
        if (progress <= 0) return BAR;
        if (isWhite) return progress;
        return progress <= 12 ? progress + 12 : progress - 12;
    }

    /// <summary>
    /// Вычисляет пункт назначения по начальному пункту и значению кубика.
    /// Возвращает OFF_BOARD (25), если фишка снимается.
    /// </summary>
    private int CalcDestination(int fromPoint, int diceValue, bool isWhite)
    {
        // Вход с бара
        if (fromPoint == BAR)
            return isWhite ? diceValue : 12 + diceValue;

        int progress = GetProgress(fromPoint, isWhite);
        int newProgress = progress + diceValue;

        if (newProgress >= OFF_BOARD)
            return OFF_BOARD;

        return GetPointFromProgress(newProgress, isWhite);
    }

    /// <summary>
    /// Вычисляет нужное значение кубика для хода из fromPoint в toPoint.
    /// Возвращает -1, если ход невозможен.
    /// </summary>
    private int CalcDiceNeeded(int fromPoint, int toPoint, bool isWhite)
    {
        if (fromPoint == BAR)
            return isWhite ? toPoint : toPoint - 12;

        int fromProgress = GetProgress(fromPoint, isWhite);
        int toProgress = (toPoint == OFF_BOARD) ? OFF_BOARD : GetProgress(toPoint, isWhite);

        int needed = toProgress - fromProgress;
        return needed > 0 ? needed : -1;
    }

    // ========================= ПРАВИЛА ИГРЫ =========================

    /// <summary>
    /// Проверяет, можно ли снимать фишки.
    /// Все фишки должны быть в «доме» (прогресс 19-24) или уже сняты.
    /// </summary>
    private bool CanBearOff(bool isWhite)
    {
        List<int> positions = isWhite ? whitePositions : blackPositions;
        foreach (int pos in positions)
        {
            if (pos == OFF_BOARD) continue;
            if (pos == BAR) return false;
            int progress = GetProgress(pos, isWhite);
            if (progress < 19) return false;
        }
        return true;
    }

    /// <summary>
    /// Проверяет победу: все 15 фишек текущего игрока сняты.
    /// Показывает панель окончания игры.
    /// </summary>
    private bool CheckWin()
    {
        bool isWhite = (currentPlayer == 0);
        List<int> positions = isWhite ? whitePositions : blackPositions;

        foreach (int pos in positions)
        {
            if (pos != OFF_BOARD) return false;
        }

        string winner = isWhite ? "You win!" : "AI wins!";
        Debug.Log($"=== {winner} ===");
        isGameOver = true;

        if (currentPlayerText != null)
            currentPlayerText.text = winner;

        // Тряска камеры при победе
        if (VisualEffects.Instance != null)
            VisualEffects.Instance.ShakeCamera(0.15f);

        ShowGameOverPanel(winner);
        return true;
    }

    /// <summary>
    /// Считает количество фишек из списка позиций на данном пункте
    /// </summary>
    public int CountPiecesOnPoint(List<int> positions, int point)
    {
        int count = 0;
        foreach (int p in positions)
            if (p == point) count++;
        return count;
    }

    // ========================= ВИЗУАЛ =========================

    /// <summary>
    /// Обновляет позиции ВСЕХ фишек на доске.
    /// Простая расстановка с фиксированным StackOffset (без сжатия).
    /// </summary>
    public void UpdateCheckerVisuals()
    {
        if (boardSetup == null) return;

        // Подсчёт количества фишек на каждом пункте
        Dictionary<int, int> whiteTotals = new Dictionary<int, int>();
        Dictionary<int, int> blackTotals = new Dictionary<int, int>();
        foreach (int pt in whitePositions) { if (!whiteTotals.ContainsKey(pt)) whiteTotals[pt] = 0; whiteTotals[pt]++; }
        foreach (int pt in blackPositions) { if (!blackTotals.ContainsKey(pt)) blackTotals[pt] = 0; blackTotals[pt]++; }

        // Расставляем белые фишки
        Dictionary<int, int> whiteStackCount = new Dictionary<int, int>();
        for (int i = 0; i < whitePositions.Count; i++)
        {
            int pt = whitePositions[i];
            if (!whiteStackCount.ContainsKey(pt)) whiteStackCount[pt] = 0;
            int total = whiteTotals.ContainsKey(pt) ? whiteTotals[pt] : 1;
            whiteCheckers[i].transform.position = CalcCheckerPos(pt, whiteStackCount[pt], total);
            whiteCheckers[i].GetComponent<SpriteRenderer>().sortingOrder = 10 + whiteStackCount[pt];
            whiteCheckers[i].SetActive(pt != OFF_BOARD);
            whiteStackCount[pt]++;
        }

        // Расставляем чёрные фишки
        Dictionary<int, int> blackStackCount = new Dictionary<int, int>();
        for (int i = 0; i < blackPositions.Count; i++)
        {
            int pt = blackPositions[i];
            if (!blackStackCount.ContainsKey(pt)) blackStackCount[pt] = 0;
            int total = blackTotals.ContainsKey(pt) ? blackTotals[pt] : 1;
            blackCheckers[i].transform.position = CalcCheckerPos(pt, blackStackCount[pt], total);
            blackCheckers[i].GetComponent<SpriteRenderer>().sortingOrder = 10 + blackStackCount[pt];
            blackCheckers[i].SetActive(pt != OFF_BOARD);
            blackStackCount[pt]++;
        }
    }

    /// <summary>
    /// Вычисляет мировую позицию фишки на пункте.
    /// Стопка растягивается на 80% высоты половины доски.
    /// </summary>
    private Vector3 CalcCheckerPos(int pointIndex, int stackIndex, int totalOnPoint = 1)
    {
        // Бар: фишки стопкой по центру доски
        if (pointIndex == BAR)
        {
            float barY = -2f + stackIndex * StackOffset;
            return new Vector3(0f, barY, 0f);
        }

        if (!boardSetup.PointPositions.ContainsKey(pointIndex))
            return Vector3.zero;

        Vector3 pointPos = boardSetup.PointPositions[pointIndex];
        float baseY = boardSetup.GetCheckerBaseY(pointIndex);
        float dir = boardSetup.GetStackDirection(pointIndex);

        // Динамический offset: стопка занимает 80% высоты половины доски
        float offset = CalcDynamicOffset(totalOnPoint);
        return new Vector3(pointPos.x, baseY + dir * stackIndex * offset, 0);
    }

    /// <summary>
    /// Вычисляет offset между фишками так, чтобы стопка всегда занимала
    /// 80% высоты половины доски (targetFill = 0.80).
    /// </summary>
    private float CalcDynamicOffset(int totalOnPoint, float targetFill = 0.80f)
    {
        if (totalOnPoint <= 1) return 0f;
        float maxHeight = 4.5f * targetFill; // 3.6 — 80% от половины доски
        return maxHeight / (totalOnPoint - 1);
    }

    /// <summary>
    /// Снимает подсветку со всех пунктов доски
    /// </summary>
    private void ClearHighlights()
    {
        if (boardSetup == null) return;
        foreach (var kvp in boardSetup.BoardPoints)
            kvp.Value.SetHighlight(false);
    }

    // ========================= УПРАВЛЕНИЕ ХОДАМИ =========================

    /// <summary>
    /// Обработчик кнопки «End Turn». Блокируется во время AI и анимации.
    /// </summary>
    public void OnEndTurnClicked()
    {
        if (isAIPlaying || isGameOver || isAnimating) return;
        EndTurn();
    }

    /// <summary>
    /// Завершает текущий ход: сбрасывает выделение и кубики, переключает игрока.
    /// Если ход переходит к чёрным (AI), автоматически запускает AI.
    /// </summary>
    public void EndTurn()
    {
        if (isGameOver) return;

        DeselectChecker();

        if (dice != null)
            dice.ResetDice();

        currentPlayer = 1 - currentPlayer;
        UpdatePlayerText();

        string playerName = currentPlayer == 0 ? "White" : "Black (AI)";
        Debug.Log($"Turn changed to {playerName}");

        // Автозапуск AI при ходе чёрных
        if (currentPlayer == 1 && aiPlayer != null)
        {
            aiPlayer.StartAITurn();
        }
    }

    /// <summary>
    /// Обработчик кнопки «Roll Dice». Разрешает бросок раз за ход.
    /// </summary>
    public void OnRollDiceClicked()
    {
        if (isGameOver || isAIPlaying || isAnimating) return;
        if (dice == null) return;
        if (dice.IsRolling) return;

        if (dice.HasRolled)
        {
            Debug.Log("Already rolled this turn!");
            return;
        }

        dice.RollDice();

        // Проверяем, есть ли вообще доступные ходы
        if (!HasAnyValidMove())
        {
            Debug.Log("No valid moves available! Press End Turn.");
        }
    }

    /// <summary>
    /// Проверяет, есть ли хотя бы один допустимый ход у текущего игрока
    /// </summary>
    private bool HasAnyValidMove()
    {
        bool isWhite = (currentPlayer == 0);
        List<int> positions = isWhite ? whitePositions : blackPositions;

        HashSet<int> occupiedPoints = new HashSet<int>();
        foreach (int p in positions)
            if (p != OFF_BOARD) occupiedPoints.Add(p);

        foreach (int fromPoint in occupiedPoints)
        {
            if (GetAvailableMoves(fromPoint).Count > 0)
                return true;
        }
        return false;
    }

    private void UpdatePlayerText()
    {
        if (currentPlayerText != null)
        {
            string playerName = currentPlayer == 0 ? "White (You)" : "Black (AI)";
            currentPlayerText.text = $"Turn: {playerName}";
        }

        // Обновляем подсветку иконок игроков
        UpdatePlayerIcons();
    }

    /// <summary>
    /// Обновляет визуальную подсветку иконок игроков (чей сейчас ход)
    /// </summary>
    private void UpdatePlayerIcons()
    {
        if (whitePlayerIcon != null)
        {
            Color wCol = currentPlayer == 0
                ? new Color(1f, 1f, 1f, 1f)     // Активный — яркий
                : new Color(0.5f, 0.5f, 0.5f, 0.4f); // Неактивный — приглушённый
            whitePlayerIcon.color = wCol;
        }

        if (blackPlayerIcon != null)
        {
            Color bCol = currentPlayer == 1
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 0.4f);
            blackPlayerIcon.color = bCol;
        }
    }

    /// <summary>
    /// Обновляет счётчик ходов и таймер в UI
    /// </summary>
    private void UpdateStatsUI()
    {
        if (moveCountText != null)
            moveCountText.text = $"Moves: {moveCount}";
    }

    private void Update()
    {
        // Обновляем таймер каждый кадр
        if (timerText != null && !isGameOver)
        {
            float elapsed = Time.time - gameStartTime;
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);
            timerText.text = $"{minutes}:{seconds:D2}";
        }
    }

    // ========================= СЛОЖНОСТЬ =========================

    public void SetDifficulty(Difficulty diff)
    {
        currentDifficulty = diff;
        Debug.Log($"AI Difficulty set to: {diff}");
    }

    // ========================= ОКОНЧАНИЕ ИГРЫ =========================

    /// <summary>
    /// Показывает панель окончания игры с результатом и статистикой.
    /// </summary>
    private void ShowGameOverPanel(string resultText)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (gameOverText != null)
            gameOverText.text = resultText;

        if (gameOverStatsText != null)
        {
            float elapsed = Time.time - gameStartTime;
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);
            gameOverStatsText.text = $"Moves: {moveCount}  |  Time: {minutes}:{seconds:D2}";
        }
    }

    private void OnPlayAgainClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // ========================= ЦВЕТОВАЯ ПАЛИТРА UI =========================

    // Цвета для интерфейса в неоновом стиле
    private readonly Color uiPanelColor = new Color(0.04f, 0.03f, 0.10f, 0.95f);        // Тёмно-синий фон
    private readonly Color uiButtonColor = new Color(0.10f, 0.05f, 0.20f, 0.95f);        // Тёмно-фиолетовая кнопка
    private readonly Color uiButtonHoverColor = new Color(0.20f, 0.08f, 0.35f, 0.95f);   // Фиолетовая подсветка
    private readonly Color uiGoldText = new Color(0.0f, 0.94f, 1.0f);                    // Неоново-голубой текст
    private readonly Color uiLightText = new Color(0.85f, 0.80f, 0.95f);                 // Светло-фиолетовый текст
    private readonly Color uiAccentGreen = new Color(0.0f, 1.0f, 0.5f, 0.95f);           // Неоново-зелёный акцент
    private readonly Color uiOverlayColor = new Color(0.02f, 0.01f, 0.05f, 0.88f);       // Тёмный оверлей

    // ========================= СОЗДАНИЕ UI =========================

    private void CreateUI()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Верхняя панель — индикатор игрока и статистика
        CreateTopPanel(canvasObj.transform);

        // Нижняя панель — кнопки управления
        CreateBottomPanel(canvasObj.transform);

        // Панель окончания игры (скрыта)
        CreateGameOverPanel(canvasObj.transform);

        if (dice != null)
            dice.SetDiceText(diceResultText);
    }

    /// <summary>
    /// Создаёт верхнюю панель с индикатором текущего игрока, результатом кубиков и статистикой.
    /// </summary>
    private void CreateTopPanel(Transform canvasParent)
    {
        // Полупрозрачная панель вверху
        GameObject topPanel = CreateUIPanel(canvasParent, "TopPanel",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0, 65), uiPanelColor);

        // Индикатор текущего игрока (по центру)
        currentPlayerText = CreateStyledText(topPanel.transform, "CurrentPlayerText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 5), new Vector2(300, 35), 24, uiGoldText);

        // Результат кубиков (правее центра)
        diceResultText = CreateStyledText(topPanel.transform, "DiceResultText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(280, 5), new Vector2(250, 30), 20, uiLightText);
        diceResultText.text = "Dice: - | -";

        // Метка сложности (слева)
        Text diffLabel = CreateStyledText(topPanel.transform, "DifficultyLabel",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(15, 5), new Vector2(180, 25), 16, uiLightText);
        diffLabel.text = $"AI: {currentDifficulty}";
        diffLabel.alignment = TextAnchor.MiddleLeft;

        // Статистика — счётчик ходов и таймер (правый край)
        moveCountText = CreateStyledText(topPanel.transform, "MoveCount",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-120, 5), new Vector2(100, 25), 16, uiLightText);
        moveCountText.text = "Moves: 0";
        moveCountText.alignment = TextAnchor.MiddleRight;

        timerText = CreateStyledText(topPanel.transform, "Timer",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-15, 5), new Vector2(70, 25), 16, uiLightText);
        timerText.text = "0:00";
        timerText.alignment = TextAnchor.MiddleRight;

        // Тонкая золотая линия внизу панели
        GameObject line = new GameObject("TopPanelLine");
        line.transform.SetParent(topPanel.transform, false);
        RectTransform lineRt = line.AddComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0f, 0f);
        lineRt.anchorMax = new Vector2(1f, 0f);
        lineRt.pivot = new Vector2(0.5f, 0f);
        lineRt.anchoredPosition = Vector2.zero;
        lineRt.sizeDelta = new Vector2(0, 2);
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = new Color(uiGoldText.r, uiGoldText.g, uiGoldText.b, 0.4f);
    }

    /// <summary>
    /// Создаёт нижнюю панель с кнопками «Roll Dice» и «End Turn».
    /// </summary>
    private void CreateBottomPanel(Transform canvasParent)
    {
        // Полупрозрачная панель внизу
        GameObject bottomPanel = CreateUIPanel(canvasParent, "BottomPanel",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0, 60), uiPanelColor);

        // Кнопка «Roll Dice» (по центру-лево)
        CreateStyledButton(bottomPanel.transform, "RollDiceButton", "Roll Dice",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-100, 0), new Vector2(160, 42),
            () => OnRollDiceClicked(), uiButtonColor, uiGoldText);

        // Кнопка «End Turn» (по центру-право)
        CreateStyledButton(bottomPanel.transform, "EndTurnButton", "End Turn",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(100, 0), new Vector2(160, 42),
            () => OnEndTurnClicked(), uiButtonColor, uiGoldText);

        // Тонкая золотая линия вверху панели
        GameObject line = new GameObject("BottomPanelLine");
        line.transform.SetParent(bottomPanel.transform, false);
        RectTransform lineRt = line.AddComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0f, 1f);
        lineRt.anchorMax = new Vector2(1f, 1f);
        lineRt.pivot = new Vector2(0.5f, 1f);
        lineRt.anchoredPosition = Vector2.zero;
        lineRt.sizeDelta = new Vector2(0, 2);
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = new Color(uiGoldText.r, uiGoldText.g, uiGoldText.b, 0.4f);
    }

    private void CreateGameOverPanel(Transform parent)
    {
        // Полупрозрачный оверлей
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(parent, false);

        RectTransform overlayRt = gameOverPanel.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayImg = gameOverPanel.AddComponent<Image>();
        overlayImg.color = uiOverlayColor;

        // Карточка по центру — стилизованная панель
        GameObject card = new GameObject("Card");
        card.transform.SetParent(gameOverPanel.transform, false);
        RectTransform cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(450, 320);
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = uiPanelColor;

        // Золотая рамка карточки
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(uiGoldText.r, uiGoldText.g, uiGoldText.b, 0.5f);
        cardOutline.effectDistance = new Vector2(2, -2);

        // Декоративная линия вверху карточки
        GameObject topLine = new GameObject("TopDecor");
        topLine.transform.SetParent(card.transform, false);
        RectTransform topLineRt = topLine.AddComponent<RectTransform>();
        topLineRt.anchorMin = new Vector2(0.1f, 1f);
        topLineRt.anchorMax = new Vector2(0.9f, 1f);
        topLineRt.pivot = new Vector2(0.5f, 1f);
        topLineRt.anchoredPosition = new Vector2(0, -8);
        topLineRt.sizeDelta = new Vector2(0, 2);
        Image topLineImg = topLine.AddComponent<Image>();
        topLineImg.color = new Color(uiGoldText.r, uiGoldText.g, uiGoldText.b, 0.5f);

        // Текст результата — крупный, золотой
        gameOverText = CreateStyledText(card.transform, "ResultText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -60), new Vector2(380, 60), 42, uiGoldText);
        gameOverText.text = "";

        // Текст статистики
        gameOverStatsText = CreateStyledText(card.transform, "StatsText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 15), new Vector2(380, 40), 22, uiLightText);
        gameOverStatsText.text = "";

        // Кнопка «Play Again»
        CreateStyledButton(card.transform, "BtnPlayAgain", "Play Again",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-110, 55), new Vector2(180, 48),
            () => OnPlayAgainClicked(), uiAccentGreen, uiLightText, 22);

        // Кнопка «Main Menu»
        CreateStyledButton(card.transform, "BtnMainMenu", "Main Menu",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(110, 55), new Vector2(180, 48),
            () => OnMainMenuClicked(), uiButtonColor, uiGoldText, 22);

        gameOverPanel.SetActive(false);
    }

    // ========================= UI ХЕЛПЕРЫ (СТИЛИЗОВАННЫЕ) =========================

    /// <summary>
    /// Создаёт стилизованный текстовый элемент с тенью и правильным шрифтом.
    /// </summary>
    private Text CreateStyledText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size, int fontSize, Color textColor)
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

        // Тень вместо простого Outline — выглядит мягче
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(1, -1);

        return text;
    }

    /// <summary>
    /// Создаёт стилизованную кнопку с деревянной текстурой и эффектом наведения.
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
        btnShadow.effectColor = new Color(0, 0, 0, 0.5f);
        btnShadow.effectDistance = new Vector2(2, -2);

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        // Настройка цветов при наведении и нажатии
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        // Неоновая пульсация кнопки
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
    /// Создаёт UI-панель (фон для группы элементов).
    /// </summary>
    private GameObject CreateUIPanel(Transform parent, string name,
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
}

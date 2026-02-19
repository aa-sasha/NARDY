using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AI-противник для длинных нард. Играет за чёрных (игрок 1).
/// Три уровня сложности с разными стратегиями выбора хода:
///   Easy   — случайный ход
///   Medium — простая эвристика (битьё, безопасность, прогресс)
///   Hard   — жадная оценка позиции (полный анализ доски для каждого хода)
/// </summary>
public class AIPlayer : MonoBehaviour
{
    [SerializeField] private float moveDelay = 0.15f; // Задержка между ходами (поверх анимации)

    private GameManager gm;
    private Dice dice;
    private Coroutine currentTurn;

    private const int BAR = 0;
    private const int OFF_BOARD = 25;

    private void Start()
    {
        gm = GameManager.Instance;
        dice = FindFirstObjectByType<Dice>();
    }

    /// <summary>
    /// Точка входа: запускает корутину хода AI.
    /// Вызывается из GameManager после переключения на чёрных.
    /// </summary>
    public void StartAITurn()
    {
        if (gm == null) gm = GameManager.Instance;
        if (dice == null) dice = FindFirstObjectByType<Dice>();

        if (currentTurn != null)
            StopCoroutine(currentTurn);
        currentTurn = StartCoroutine(AITurnCoroutine());
    }

    /// <summary>
    /// Основной цикл хода AI: бросает кубики, делает ходы с анимацией, завершает ход.
    /// Ждёт завершения анимаций перед каждым следующим действием.
    /// </summary>
    private IEnumerator AITurnCoroutine()
    {
        gm.SetAIPlaying(true);
        yield return new WaitForSeconds(0.3f);

        // Автоматический бросок кубиков
        dice.RollDice();
        Debug.Log("[AI] Rolled dice");

        // Ждём завершения анимации броска
        yield return new WaitUntil(() => !dice.IsRolling);
        yield return new WaitForSeconds(0.3f);

        // Делаем ходы, пока есть доступные кубики
        int safety = 0;
        while (!dice.AllUsed && safety < 10)
        {
            safety++;
            var moves = gm.GetAllPossibleMoves(1);
            if (moves.Count == 0)
            {
                Debug.Log("[AI] No valid moves available");
                break;
            }

            GameManager.MoveInfo chosen = ChooseMove(moves);
            Debug.Log($"[AI] {gm.CurrentDifficulty}: {chosen}");
            gm.TryMove(chosen.fromPoint, chosen.toPoint, chosen.diceValue);

            // Ждём завершения анимации хода перед следующим
            yield return new WaitUntil(() => !gm.IsAnimating);
            yield return new WaitForSeconds(moveDelay);
        }

        yield return new WaitForSeconds(0.2f);
        gm.SetAIPlaying(false);
        gm.EndTurn();
        currentTurn = null;
    }

    /// <summary>
    /// Выбирает стратегию хода в зависимости от уровня сложности
    /// </summary>
    private GameManager.MoveInfo ChooseMove(List<GameManager.MoveInfo> moves)
    {
        switch (gm.CurrentDifficulty)
        {
            case GameManager.Difficulty.Easy:
                return ChooseMoveEasy(moves);
            case GameManager.Difficulty.Medium:
                return ChooseMoveMedium(moves);
            case GameManager.Difficulty.Hard:
                return ChooseMoveHard(moves);
            default:
                return moves[0];
        }
    }

    // ========================= EASY =========================

    /// <summary>
    /// Лёгкий AI: выбирает полностью случайный ход.
    /// </summary>
    private GameManager.MoveInfo ChooseMoveEasy(List<GameManager.MoveInfo> moves)
    {
        return moves[Random.Range(0, moves.Count)];
    }

    // ========================= MEDIUM =========================

    /// <summary>
    /// Средний AI: оценивает каждый ход простой эвристикой и выбирает лучший.
    /// </summary>
    private GameManager.MoveInfo ChooseMoveMedium(List<GameManager.MoveInfo> moves)
    {
        float bestScore = float.MinValue;
        GameManager.MoveInfo bestMove = moves[0];

        foreach (var move in moves)
        {
            float score = ScoreMoveMedium(move);
            // Небольшой случайный разброс, чтобы равные ходы не были одинаковы
            score += Random.Range(0f, 0.5f);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    /// <summary>
    /// Эвристика среднего уровня: оценка одного хода без симуляции.
    /// +10 битьё, +5 безопасный пункт, +3*прогресс, +8 снятие.
    /// </summary>
    private float ScoreMoveMedium(GameManager.MoveInfo move)
    {
        float score = 0;
        List<int> whitePos = gm.WhitePositions;
        List<int> blackPos = gm.BlackPositions;

        // +10 за битьё фишки противника
        if (move.toPoint >= 1 && move.toPoint <= 24)
        {
            if (CountInList(whitePos, move.toPoint) == 1)
                score += 10f;
        }

        // +5 за создание безопасного пункта (2+ свои фишки после хода)
        if (move.toPoint >= 1 && move.toPoint <= 24)
        {
            int ownOnDest = CountInList(blackPos, move.toPoint);
            if (ownOnDest >= 1)
                score += 5f;
        }

        // +3 за каждый шаг прогресса к дому
        int fromProg = gm.GetProgress(move.fromPoint, false);
        int toProg = (move.toPoint == OFF_BOARD) ? 25 : gm.GetProgress(move.toPoint, false);
        score += (toProg - fromProg) * 3f;

        // +8 за снятие фишки
        if (move.toPoint == OFF_BOARD)
            score += 8f;

        // -3 за оставление одиночной фишки (блот) на исходном пункте
        if (move.fromPoint >= 1 && move.fromPoint <= 24)
        {
            int ownOnSource = CountInList(blackPos, move.fromPoint);
            if (ownOnSource == 2)
                score -= 3f;
        }

        return score;
    }

    // ========================= HARD =========================

    /// <summary>
    /// Сильный AI: симулирует каждый ход на копиях позиций,
    /// оценивает получившуюся доску и выбирает ход с наивысшей оценкой.
    /// </summary>
    private GameManager.MoveInfo ChooseMoveHard(List<GameManager.MoveInfo> moves)
    {
        float bestScore = float.MinValue;
        GameManager.MoveInfo bestMove = moves[0];

        foreach (var move in moves)
        {
            List<int> simWhite = new List<int>(gm.WhitePositions);
            List<int> simBlack = new List<int>(gm.BlackPositions);
            SimulateMove(move, simWhite, simBlack, false);

            float score = EvaluatePosition(simWhite, simBlack);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    /// <summary>
    /// Полная оценка позиции с точки зрения чёрных.
    /// Чем выше — тем лучше для чёрных.
    /// </summary>
    private float EvaluatePosition(List<int> whitePos, List<int> blackPos)
    {
        float score = 0;

        // === Фишки чёрных ===
        score += CountInList(blackPos, OFF_BOARD) * 20f;  // Снятые: +20
        score -= CountInList(blackPos, BAR) * 20f;         // На баре: -20

        // Анализ позиций на доске
        Dictionary<int, int> blackCounts = new Dictionary<int, int>();
        float totalProgress = 0;
        int activePieces = 0;

        foreach (int pos in blackPos)
        {
            if (pos == OFF_BOARD || pos == BAR) continue;
            activePieces++;

            int progress = gm.GetProgress(pos, false);
            totalProgress += progress;

            if (!blackCounts.ContainsKey(pos)) blackCounts[pos] = 0;
            blackCounts[pos]++;
        }

        // Средний прогресс: ближе к дому = лучше
        if (activePieces > 0)
            score += (totalProgress / activePieces) * 2f;

        // Безопасные пункты (2+ фишки): +8, блоты (1 фишка): -5
        foreach (var kvp in blackCounts)
        {
            if (kvp.Value >= 2)
                score += 8f;
            else if (kvp.Value == 1)
                score -= 5f;
        }

        // Фишки в домашнем квадранте (прогресс 19-24): +10
        foreach (int pos in blackPos)
        {
            if (pos == OFF_BOARD || pos == BAR) continue;
            if (gm.GetProgress(pos, false) >= 19)
                score += 10f;
        }

        // Блокирующие «праймы»: последовательные безопасные пункты (+12 за каждую пару)
        List<int> safeProgresses = new List<int>();
        foreach (var kvp in blackCounts)
        {
            if (kvp.Value >= 2)
                safeProgresses.Add(gm.GetProgress(kvp.Key, false));
        }
        safeProgresses.Sort();

        for (int i = 1; i < safeProgresses.Count; i++)
        {
            if (safeProgresses[i] == safeProgresses[i - 1] + 1)
                score += 12f;
        }

        // === Фишки белых (противник) ===
        score += CountInList(whitePos, BAR) * 15f;  // На баре: хорошо для чёрных

        // Блоты белых: возможности для битья (+3)
        Dictionary<int, int> whiteCounts = new Dictionary<int, int>();
        foreach (int pos in whitePos)
        {
            if (pos == OFF_BOARD || pos == BAR) continue;
            if (!whiteCounts.ContainsKey(pos)) whiteCounts[pos] = 0;
            whiteCounts[pos]++;
        }
        foreach (var kvp in whiteCounts)
        {
            if (kvp.Value == 1)
                score += 3f;
        }

        // Снятые белые: плохо для чёрных (-10)
        score -= CountInList(whitePos, OFF_BOARD) * 10f;

        return score;
    }

    /// <summary>
    /// Симулирует ход на копиях списков позиций (не затрагивает реальную игру).
    /// Обрабатывает битьё (одиночная фишка противника → бар).
    /// </summary>
    private void SimulateMove(GameManager.MoveInfo move, List<int> whitePos, List<int> blackPos, bool moverIsWhite)
    {
        List<int> myPos = moverIsWhite ? whitePos : blackPos;
        List<int> oppPos = moverIsWhite ? blackPos : whitePos;

        // Битьё: если на пункте одна фишка противника — на бар
        if (move.toPoint >= 1 && move.toPoint <= 24)
        {
            if (CountInList(oppPos, move.toPoint) == 1)
            {
                int hitIdx = oppPos.IndexOf(move.toPoint);
                if (hitIdx >= 0) oppPos[hitIdx] = BAR;
            }
        }

        // Перемещение фишки
        int idx = myPos.IndexOf(move.fromPoint);
        if (idx >= 0)
            myPos[idx] = move.toPoint;
    }

    private int CountInList(List<int> list, int value)
    {
        int count = 0;
        foreach (int v in list)
            if (v == value) count++;
        return count;
    }
}

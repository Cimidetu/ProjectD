using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // Подключаем DOTween

// ИСПРАВЛЕНО: Вернули глобальное объявление фаз хода
public enum TurnPhase
{
    PlayerTurn,
    EnemyTurn
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Current Battle Status")]
    public TurnPhase currentPhase = TurnPhase.PlayerTurn;

    [Header("Dependencies")]
    public DeckManager deckManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        currentPhase = TurnPhase.PlayerTurn;
        Debug.Log("<color=green>[РАУНД]</color> Начался Ход Игрока! Карты разблокированы.");

        if (ActionPointsManager.Instance != null)
        {
            ActionPointsManager.Instance.ResetEnergyForNewTurn();
        }

        if (deckManager != null)
        {
            for (int i = 0; i < 3; i++)
            {
                deckManager.DrawCard();
            }
        }
    }

    public void EndPlayerTurn()
    {
        if (currentPhase != TurnPhase.PlayerTurn) return;

        currentPhase = TurnPhase.EnemyTurn;
        Debug.Log("<color=red>[РАУНД]</color> Игрок завершил ход. Активация ИИ Противников...");

        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.ClearAllHovers();
        }

        // Собираем всех живых врагов на сцене
        GameObject[] enemiesGo = GameObject.FindGameObjectsWithTag("Enemy");
        List<EnemyHealth> activeEnemies = new List<EnemyHealth>();

        foreach (var go in enemiesGo)
        {
            EnemyHealth eh = go.GetComponent<EnemyHealth>();
            if (eh != null) activeEnemies.Add(eh);
        }

        // 1. Сначала у всех врагов одновременно тикают кулдауны способностей на -1 ход
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.TickAbilityTimers();
        }

        // 2. Спустя 0.5 секунды задержки для темпа боя враги по очереди активируют готовые действия
        DOVirtual.DelayedCall(0.5f, () =>
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null) enemy.ExecuteEnemyTurnAction();
            }
        });

        // Возвращаем ход игроку через 2 секунды общей фазы монстров
        Invoke(nameof(StartPlayerTurn), 2.0f);
    }

    public bool IsPlayerTurn()
    {
        return currentPhase == TurnPhase.PlayerTurn;
    }
}

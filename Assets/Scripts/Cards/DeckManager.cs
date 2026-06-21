using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Dependencies")]
    public HandManager handManager;       
    public GameObject cardPrefab;         
    
    [Tooltip("Перетащите сюда ваш InputManager")]
    public PlayerInputHandler inputHandler; 

    [Header("Spawn Settings")]
    public Transform deckSpawnPoint;      

    private List<CardData> drawPile = new List<CardData>(); // Активная колода добора

    private void Start()
    {
        LoadDeckFromTextFile();
    }

    private void LoadDeckFromTextFile()
    {
        TextAsset deckFile = Resources.Load<TextAsset>("StartingDeck");

        if (deckFile == null)
        {
            Debug.LogError("<color=red>[Колода]</color> Не удалось найти файл StartingDeck.txt в папке Assets/Resources/!");
            return;
        }

        string[] cardIDs = deckFile.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        Debug.Log($"<color=cyan>[Колода]</color> Чтение файла колоды... Найдено записей: {cardIDs.Length}. Начинаем точечную сборку ассетов.");

        foreach (string id in cardIDs)
        {
            string cleanID = id.Trim();
            CardData loadedCard = Resources.Load<CardData>($"Cards/{cleanID}");

            if (loadedCard != null)
            {
                drawPile.Add(loadedCard);
            }
            else
            {
                Debug.LogError($"<color=red>[ОШИБКА КОЛОДЫ]</color> Карта с ID '{cleanID}' не найдена в Resources/Cards/!");
            }
        }

        Debug.Log($"<color=green>[Колода]</color> Сборка завершена! В колоду добора успешно добавлено {drawPile.Count} карт.");
    }

    public void DrawCard()
    {
        if (cardPrefab == null || handManager == null)
        {
            Debug.LogWarning("DeckManager: Пропущены ссылки в инспекторе!");
            return;
        }

        if (drawPile.Count == 0)
        {
            Debug.LogWarning("Колода добора пуста!");
            return;
        }

        if (inputHandler != null)
        {
            inputHandler.ClearAllHovers();
        }

        int randomIndex = Random.Range(0, drawPile.Count);
        CardData randomCardData = drawPile[randomIndex];

        Vector3 spawnPosition = deckSpawnPoint != null ? deckSpawnPoint.position : Vector3.zero;
        GameObject newCardGo = Instantiate(cardPrefab, spawnPosition, Quaternion.identity);
        
        CardVisual3D newCardScript = newCardGo.GetComponent<CardVisual3D>();

        if (newCardScript != null)
        {
            newCardScript.cardData = randomCardData;
            newCardScript.LoadCardDataVisuals();

            newCardGo.name = $"Card_{randomCardData.cardName}";

            foreach (var existingCard in handManager.cardsInHand)
            {
                if (existingCard != null)
                {
                    existingCard.handSortIndex++;
                }
            }

            newCardScript.handSortIndex = 0;
            handManager.cardsInHand.Add(newCardScript);
            handManager.UpdateHandLayout();
        }
    }

    public void EndTurn()
    {
        // ИСПРАВЛЕНО: Кнопка интерфейса теперь пересылает сигнал в TurnManager для правильной смены фазы раунда
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndPlayerTurn();
        }
    }
}

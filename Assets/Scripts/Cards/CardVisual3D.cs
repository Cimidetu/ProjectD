using UnityEngine;

public class CardVisual3D : MonoBehaviour
{
    [Header("Card Asset Data")]
    [Tooltip("Сюда перетаскивается созданный через инструмент ассет CardData")]
    public CardData cardData; 

    [Header("3D Text Mesh Pro References")]
    [Tooltip("3D TextMeshPro для Названия карты")]
    public TMPro.TextMeshPro nameText;
    
    [Tooltip("3D TextMeshPro для цены AP (в углу карты)")]
    public TMPro.TextMeshPro apCostText;
    
    [Tooltip("3D TextMeshPro для силы эффекта (урон/щит)")]
    public TMPro.TextMeshPro effectValueText;

    [HideInInspector] public Vector3 originalPosition;
    [HideInInspector] public Vector3 originalRotation;
    [HideInInspector] public Vector3 originalScale; 

    [HideInInspector] public bool isDragging = false;
    [HideInInspector] public bool isHovered = false; 
    [HideInInspector] public int handSortIndex = 0; 

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Start()
    {
        // При появлении карты на сцене (или доборе) автоматически заполняем её тексты
        LoadCardDataVisuals();
    }

    // Метод, который переносит данные из ScriptableObject на 3D-тексты карты
    public void LoadCardDataVisuals()
    {
        if (cardData == null) 
        {
            Debug.LogWarning($"[Карта] На объекте {gameObject.name} не привязан ассет CardData!");
            return;
        }

        // Заполняем 3D тексты данными из файла ассета
        if (nameText != null) nameText.text = cardData.cardName;
        if (apCostText != null) apCostText.text = cardData.apCost.ToString();
        
        if (effectValueText != null)
        {
            // Автоматически смотрим на тип карты: если Атака — пишем урон, если Защита — щит
            if (cardData.cardType == CardType.Attack)
            {
                effectValueText.text = cardData.baseDamage.ToString();
            }
            else if (cardData.cardType == CardType.Defense)
            {
                effectValueText.text = cardData.baseShield.ToString();
            }
            else
            {
                effectValueText.text = "0"; // Для вспомогательных навыков
            }
        }
    }

    public void ResetToHand()
    {
        isDragging = false;
        isHovered = false;
    }
}

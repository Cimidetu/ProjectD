using System.Collections.Generic;
using UnityEngine;

public enum CardType { Attack, Defense, Skill, Ultimate }

// Типы эффектов вражеских способностей
public enum EnemyAbilityType
{
    HeavyAttack,     // Сильный одиночный удар (Множитель урона)
    MultiHit,        // Серия ударов (Комбо-урон)
    GainShield       // Враг накладывает щит на себя
}

// Структура данных отдельной способности врага
[System.Serializable]
public class EnemyAbility
{
    public string abilityName = "Power Smash";
    public EnemyAbilityType abilityType = EnemyAbilityType.HeavyAttack;
    
    [Tooltip("Сколько ходов заряжается способность до использования")]
    public int baseCooldown = 3;
    
    [HideInInspector] public int currentCooldownTimer; // Текущий остаток ходов в бою

    [Tooltip("Множитель эффекта (например, 2 для HeavyAttack означает Урон Врага * 2)")]
    public float multiplier = 2f;

    [Tooltip("Приоритет использования, если готовы несколько скиллов одновременно. Чем выше число, тем важнее.")]
    public int priority = 1;
}

[CreateAssetMenu(fileName = "NewCard", menuName = "ChaosGame/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Base Info")]
    public string cardID; public string cardName;
    [TextArea(3, 5)] public string description;
    [Header("Resources")]
    public int apCost; public CardType cardType; public string characterName; 
    [Header("Visuals")]
    public Sprite cardArt; 
    [Header("Effect Values")]
    public int baseDamage; public int baseShield; 
}

// Расширенный ScriptableObject для персонажей и монстров
[CreateAssetMenu(fileName = "NewCharacter", menuName = "ChaosGame/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character Info")]
    public string characterName = "Goblin";
    public GameObject visualPrefab; 
    public Sprite icon; 

    [Header("Combat Stats (For Enemies & Allies)")]
    public int maxHealth = 100;
    public int baseAttackDamage = 10; 

    [Header("Enemy AI Abilities")]
    [Tooltip("Список особых приемов этого врага с таймерами и приоритетами")]
    public List<EnemyAbility> enemyAbilities = new List<EnemyAbility>();

    [Header("Ally Deck (Only for Players)")]
    public List<CardData> characterCards = new List<CardData>();
}

public class BattleEffectManager : MonoBehaviour
{
    public static BattleEffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool PlayCardEffect(CardData cardData, GameObject target)
    {
        if (cardData == null)
        {
            if (target.CompareTag("Enemy"))
            {
                EnemyHealth enemy = target.GetComponent<EnemyHealth>();
                if (enemy == null) enemy = target.GetComponentInChildren<EnemyHealth>();
                if (enemy == null) enemy = target.GetComponentInParent<EnemyHealth>();
                if (enemy != null) { enemy.TakeDamage(25); return true; }
            }
            else if (target.CompareTag("Ally"))
            {
                PlayerAlly ally = target.GetComponent<PlayerAlly>();
                if (ally == null) ally = target.GetComponentInChildren<PlayerAlly>();
                if (ally == null) ally = target.GetComponentInParent<PlayerAlly>();
                if (ally != null) { ally.AddShield(15); return true; }
            }
            return false;
        }

        EnemyHealth enemyTarget = target.GetComponent<EnemyHealth>();
        if (enemyTarget == null) enemyTarget = target.GetComponentInChildren<EnemyHealth>();
        if (enemyTarget == null) enemyTarget = target.GetComponentInParent<EnemyHealth>();

        PlayerAlly allyTarget = target.GetComponent<PlayerAlly>();
        if (allyTarget == null) allyTarget = target.GetComponentInChildren<PlayerAlly>();
        if (allyTarget == null) allyTarget = target.GetComponentInParent<PlayerAlly>();

        switch (cardData.cardType)
        {
            case CardType.Attack:
                if (enemyTarget == null)
                {
                    Debug.LogWarning($"<color=red>[ОШИБКА ЦЕЛИ]</color> Нельзя атаковать союзного персонажа {target.name}!");
                    return false;
                }
                break;

            case CardType.Defense:
            case CardType.Skill:
                if (allyTarget == null)
                {
                    Debug.LogWarning($"<color=red>[ОШИБКА ЦЕЛИ]</color> Нельзя применять карту защиты/исцеления на врага {target.name}!");
                    return false;
                }
                break;
        }

        Debug.Log($"<color=yellow>[ЭФФЕКТЫ]</color> Успешная активация карты '{cardData.cardName}' на цель {target.name}");

        switch (cardData.cardType)
        {
            case CardType.Attack:
                enemyTarget.TakeDamage(cardData.baseDamage);
                break;

            case CardType.Defense:
                allyTarget.AddShield(cardData.baseShield);
                break;

            case CardType.Skill:
                allyTarget.Heal(15); 
                break;

            case CardType.Ultimate:
                Debug.Log($"[BattleEffectManager] Особый ультимейт на цель {target.name}");
                break;
        }

        return true;
    }
}

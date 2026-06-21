using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using DG.Tweening;

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Configuration Data")]
    [HideInInspector] public CharacterData enemyData;

    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("UI Component")]
    public Slider healthSlider;

    [Header("Juice Settings (Enemy Shake)")]
    public float shakeDuration = 0.25f;
    public float shakeStrength = 0.35f;
    public int shakeVibrato = 15;

    // Внутренний список абилок для независимого отсчета таймеров в бою
    private List<EnemyAbility> runtimeAbilities = new List<EnemyAbility>();

    private void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        // Клонируем абилки из ассета в память и заводим таймеры отсчета
        if (enemyData != null)
        {
            maxHealth = enemyData.maxHealth;
            currentHealth = maxHealth;
            if (healthSlider != null) { healthSlider.maxValue = maxHealth; healthSlider.value = maxHealth; }

            foreach (var originalAbility in enemyData.enemyAbilities)
            {
                EnemyAbility runtimeAbility = new EnemyAbility
                {
                    abilityName = originalAbility.abilityName,
                    abilityType = originalAbility.abilityType,
                    baseCooldown = originalAbility.baseCooldown,
                    currentCooldownTimer = originalAbility.baseCooldown, // Таймер тикает с начала битвы
                    multiplier = originalAbility.multiplier,
                    priority = originalAbility.priority
                };
                runtimeAbilities.Add(runtimeAbility);
            }
        }
    }

    // Метод снижения кулдаунов при завершении хода игрока
    public void TickAbilityTimers()
    {
        if (runtimeAbilities.Count == 0) return;

        Debug.Log($"<color=orange>[ИИ Врага]</color> {gameObject.name}: Снижение таймеров абилок на -1 ход.");

        foreach (var ability in runtimeAbilities)
        {
            ability.currentCooldownTimer--;
            ability.currentCooldownTimer = Mathf.Max(ability.currentCooldownTimer, 0);
            Debug.Log($"   -> Скилл '{ability.abilityName}' | Осталось ходов до зарядки: {ability.currentCooldownTimer}");
        }
    }

    // Логика выбора действия врага на основе таймеров и приоритета
    public void ExecuteEnemyTurnAction()
    {
        if (runtimeAbilities.Count == 0)
        {
            PerformDefaultAttack();
            return;
        }

        List<EnemyAbility> readyAbilities = new List<EnemyAbility>();

        // Ищем все скиллы, у которых таймер упал до 0
        foreach (var ability in runtimeAbilities)
        {
            if (ability.currentCooldownTimer <= 0)
            {
                readyAbilities.Add(ability);
            }
        }

        // Если ни один скилл не зарядился — бьем дефолтной атакой
        if (readyAbilities.Count == 0)
        {
            PerformDefaultAttack();
            return;
        }

        // СОРТИРОВКА ПО ПРИОРИТЕТУ: если готовы 2+ скилла, выбираем тот, у которого приоритет ВЫШЕ
        readyAbilities.Sort((a, b) => b.priority.CompareTo(a.priority));
        EnemyAbility activeAbility = readyAbilities[0];
        
        Debug.Log($"<color=red>[ИИ Врага]</color> {gameObject.name} ИСПОЛЬЗУЕТ АБИЛКУ: '{activeAbility.abilityName}' (Приоритет: {activeAbility.priority})!");

        PlayerAlly targetAlly = FindRandomLivingAlly();

        if (targetAlly != null)
        {
            int baseDmg = enemyData != null ? enemyData.baseAttackDamage : 10;

            switch (activeAbility.abilityType)
            {
                case EnemyAbilityType.HeavyAttack:
                    int heavyDmg = Mathf.RoundToInt(baseDmg * activeAbility.multiplier);
                    targetAlly.TakeDamage(heavyDmg);
                    break;

                case EnemyAbilityType.MultiHit:
                    int multiDmg = Mathf.RoundToInt(baseDmg * activeAbility.multiplier);
                    Debug.Log($"   -> Серия ударов комбо-множителя {activeAbility.multiplier}x!");
                    targetAlly.TakeDamage(multiDmg);
                    break;

                case EnemyAbilityType.GainShield:
                    int shieldAmount = Mathf.RoundToInt(baseDmg * activeAbility.multiplier);
                    Debug.Log($"   -> Враг применил защиту на {shieldAmount}!");
                    break;
            }

            // Выпад вперед в знак удара
            transform.DOMoveZ(transform.position.z - 0.5f, 0.1f).SetLoops(2, LoopType.Yoyo);
        }

        // Сброс кулдауна отработанного скилла на начальное значение
        activeAbility.currentCooldownTimer = activeAbility.baseCooldown;
    }

    private void PerformDefaultAttack()
    {
        PlayerAlly targetAlly = FindRandomLivingAlly();
        if (targetAlly != null)
        {
            int dmg = enemyData != null ? enemyData.baseAttackDamage : 10;
            Debug.Log($"<color=red>[ИИ Врага]</color> {gameObject.name} наносит обычный удар на {dmg} урона по {targetAlly.allyName}!");
            targetAlly.TakeDamage(dmg);

            transform.DOMoveZ(transform.position.z - 0.4f, 0.1f).SetLoops(2, LoopType.Yoyo);
        }
    }

    private PlayerAlly FindRandomLivingAlly()
    {
        GameObject[] allies = GameObject.FindGameObjectsWithTag("Ally");
        if (allies.Length == 0) return null;

        int rand = Random.Range(0, allies.Length);
        return allies[rand].GetComponent<PlayerAlly>();
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthSlider != null) healthSlider.DOValue(currentHealth, 0.2f);

        transform.DOKill();
        transform.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0, 0), shakeVibrato, 90, false, true);

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log($"<color=red>[БОЙ]</color> Враг {gameObject.name} повержен!");
        transform.DOKill();
        transform.DOScale(Vector3.zero, 0.3f).OnComplete(() => { Destroy(gameObject); });
    }
}

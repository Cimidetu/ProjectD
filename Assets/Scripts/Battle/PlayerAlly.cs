using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Подключаем DOTween

public class PlayerAlly : MonoBehaviour
{
    [Header("Ally Stats")]
    public string allyName = "Hero";
    public int maxHealth = 100;
    private int currentHealth;
    private int currentShield = 0;

    [Header("UI References")]
    [Tooltip("World Space Slider для отображения здоровья союзника")]
    public Slider healthSlider;

    [Header("Juice Settings (Shake)")]
    public float shakeDuration = 0.2f;
    public float shakeStrength = 0.25f;

    private Vector3 originalScale;

    private void Start()
    {
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        UpdateUI();
    }

    // Метод получения урона от будущих атак врагов
    public void TakeDamage(int amount)
    {
        if (currentShield > 0)
        {
            if (amount <= currentShield)
            {
                currentShield -= amount;
                amount = 0;
            }
            else
            {
                amount -= currentShield;
                currentShield = 0;
            }
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();

        // Тряска от получения урона (влево-вправо)
        transform.DOKill();
        transform.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0, 0), 15);
    }

    // Метод лечения (Heal), который будут вызывать наши карты
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateUI();

        // ИСПРАВЛЕНО: Мягкий подскок вверх при лечении с правильным синтаксисом Yoyo в DOTween
        transform.DOKill();
        transform.DOMoveY(transform.position.y + 0.3f, 0.15f).SetLoops(2, LoopType.Yoyo);
    }

    // Метод добавления брони/щита (Block), который будут вызывать наши карты
    public void AddShield(int amount)
    {
        currentShield += amount;
        UpdateUI();

        // ИСПРАВЛЕНО: Легкое сжатие модели (эффект укрепления брони) с правильным синтаксисом Yoyo в DOTween
        transform.DOKill();
        transform.localScale = originalScale; // Сбрасываем масштаб перед новой анимацией
        transform.DOScale(new Vector3(originalScale.x * 1.15f, originalScale.y * 0.85f, originalScale.z), 0.1f).SetLoops(2, LoopType.Yoyo);
    }

    private void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.DOValue(currentHealth, 0.15f);
        }
        Debug.Log($"<color=green>[СОЮЗНИК]</color> {allyName} | HP: {currentHealth}/{maxHealth} | Щит: {currentShield}");
    }
}

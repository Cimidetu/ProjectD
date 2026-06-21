using UnityEngine;

public class ActionPointsManager : MonoBehaviour
{
    // Синглтон для быстрого доступа изо всех скриптов ввода и эффектов
    public static ActionPointsManager Instance { get; private set; }

    [Header("AP Settings (Action Points)")]
    public int maxActionPoints = 3;
    private int currentActionPoints;

    [Header("SP Settings (Skill Points)")]
    public int maxSkillPoints = 100;
    private int currentSkillPoints = 0;

    [Header("UI References")]
    [Tooltip("Сюда перетащите объект AP_Text из Canvas")]
    public TMPro.TextMeshProUGUI apText;

    [Tooltip("Сюда перетащите объект SP_Text из Canvas")]
    public TMPro.TextMeshProUGUI spText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ResetEnergyForNewTurn();
    }

    // Метод для восполнения энергии в начале нового хода
    public void ResetEnergyForNewTurn()
    {
        currentActionPoints = maxActionPoints;
        UpdateUI();
    }

    // Проверка: хватает ли энергии на розыгрыш карты
    public bool HasEnoughEnergy(int cost)
    {
        return currentActionPoints >= cost;
    }

    // Трата энергии при успешном розыгрыше
    public void SpendEnergy(int cost)
    {
        currentActionPoints -= cost;
        currentActionPoints = Mathf.Max(currentActionPoints, 0); // Не даем уйти в минус
        
        // Каждая разыгранная карта дает игроку +10 SP!
        GainSP(10);
        
        UpdateUI();
    }

    // Добавление накопленного SP
    public void GainSP(int amount)
    {
        currentSkillPoints += amount;
        currentSkillPoints = Mathf.Clamp(currentSkillPoints, 0, maxSkillPoints);
        UpdateUI();
    }

    // Проверка: хватает ли SP на ультимейт
    public bool HasEnoughSP(int cost)
    {
        return currentSkillPoints >= cost;
    }

    // Трата SP
    public void SpendSP(int cost)
    {
        currentSkillPoints -= cost;
        currentSkillPoints = Mathf.Max(currentSkillPoints, 0);
        UpdateUI();
    }

    // Метод обновления текстового интерфейса на экране
    public void UpdateUI()
    {
        if (apText != null) apText.text = $"AP: {currentActionPoints}/{maxActionPoints}";
        if (spText != null) spText.text = $"SP: {currentSkillPoints}%";

        Debug.Log($"<color=cyan>[РЕСУРСЫ]</color> AP: {currentActionPoints}/{maxActionPoints} | SP: {currentSkillPoints}%");
    }
}

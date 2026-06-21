using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Allies Spawn Settings")]
    public List<Transform> allySpawnPoints = new List<Transform>();

    [Header("Enemies Spawn Settings")]
    public List<Transform> enemySpawnPoints = new List<Transform>();

    [Header("UI Templates (World Space Canvas)")]
    public UnityEngine.UI.Slider allyHealthSliderPrefab;
    public UnityEngine.UI.Slider enemyHealthSliderPrefab;

    private void Start()
    {
        SpawnPlayerParty();
        SpawnEnemyParty();
    }

    private void SpawnPlayerParty()
    {
        TextAsset partyFile = Resources.Load<TextAsset>("PlayerParty");
        if (partyFile == null) return;

        string[] characterIDs = partyFile.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        int spawnIndex = 0;

        foreach (string id in characterIDs)
            if (BuildUnitInstance(id, spawnIndex, allySpawnPoints, true)) spawnIndex++;
    }

    private void SpawnEnemyParty()
    {
        TextAsset enemyFile = Resources.Load<TextAsset>("EnemyParty");
        if (enemyFile == null) return;

        string[] enemyIDs = enemyFile.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        int spawnIndex = 0;

        foreach (string id in enemyIDs)
            if (BuildUnitInstance(id, spawnIndex, enemySpawnPoints, false)) spawnIndex++;
    }

    private bool BuildUnitInstance(string id, int index, List<Transform> points, bool isAlly)
    {
        string cleanID = id.Trim();
        if (index >= points.Count) return false;

        CharacterData data = Resources.Load<CharacterData>($"Characters/{cleanID}");
        if (data == null || data.visualPrefab == null) return false;

        Transform point = points[index];
        GameObject instance = Instantiate(data.visualPrefab, point.position, point.rotation);

        if (isAlly)
        {
            instance.name = $"Ally_{data.characterName}";
            instance.tag = "Ally";

            PlayerAlly allyComponent = instance.GetComponent<PlayerAlly>();
            if (allyComponent == null) allyComponent = instance.AddComponent<PlayerAlly>();

            allyComponent.allyName = data.characterName;
            allyComponent.maxHealth = data.maxHealth;

            if (allyHealthSliderPrefab != null)
            {
                UnityEngine.UI.Slider slider = Instantiate(allyHealthSliderPrefab, instance.transform.position + Vector3.up * 1.6f, Quaternion.identity, instance.transform);
                allyComponent.healthSlider = slider;
            }
        }
        else
        {
            instance.name = $"Enemy_{data.characterName}";
            instance.tag = "Enemy";

            EnemyHealth enemyComponent = instance.GetComponent<EnemyHealth>();
            if (enemyComponent == null) enemyComponent = instance.AddComponent<EnemyHealth>();

            // СВЯЗЫВАЕМ ДАННЫЕ С МОНСТРОМ
            enemyComponent.enemyData = data;
            enemyComponent.maxHealth = data.maxHealth;

            if (enemyHealthSliderPrefab != null)
            {
                UnityEngine.UI.Slider slider = Instantiate(enemyHealthSliderPrefab, instance.transform.position + Vector3.up * 1.6f, Quaternion.identity, instance.transform);
                enemyComponent.healthSlider = slider;
            }
        }

        return true;
    }
}

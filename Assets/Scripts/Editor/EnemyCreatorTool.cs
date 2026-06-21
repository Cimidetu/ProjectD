using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class EnemyCreatorTool : EditorWindow
{
    private string enemyID = "enemy_goblin";
    private string enemyName = "Goblin Warrior";
    private int maxHealth = 80;
    private int baseAttackDamage = 12;
    private GameObject visualPrefab;
    private Sprite icon;

    // Список абилок, настраиваемый прямо в окне тула
    private List<EnemyAbility> abilities = new List<EnemyAbility>();

    private string savePath = "Assets/Resources/Characters/";

    [MenuItem("Chaos Tools/Enemy Creator Window")]
    public static void ShowWindow()
    {
        GetWindow<EnemyCreatorTool>("Enemy Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Chaos Zero Nightmare — Enemy Monster Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        enemyID = EditorGUILayout.TextField("Enemy ID (Unique)", enemyID);
        enemyName = EditorGUILayout.TextField("Enemy In-Game Name", enemyName);
        maxHealth = EditorGUILayout.IntField("Max Health (HP)", maxHealth);
        baseAttackDamage = EditorGUILayout.IntField("Base Attack Damage", baseAttackDamage);
        EditorGUILayout.Space();

        visualPrefab = (GameObject)EditorGUILayout.ObjectField("Enemy 3D Prefab", visualPrefab, typeof(GameObject), false);
        icon = (Sprite)EditorGUILayout.ObjectField("Enemy Icon (UI)", icon, typeof(Sprite), false);
        EditorGUILayout.Space();

        // БЛОК НАСТРОЙКИ АБИЛОК
        GUILayout.Label("Enemy Special Abilities (AI):", EditorStyles.boldLabel);
        if (GUILayout.Button("Add New Special Ability (+)", GUILayout.Width(180)))
        {
            abilities.Add(new EnemyAbility());
        }

        EditorGUILayout.Space();
        for (int i = 0; i < abilities.Count; i++)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Ability #{i + 1}", EditorStyles.miniBoldLabel);
            
            abilities[i].abilityName = EditorGUILayout.TextField("Name", abilities[i].abilityName);
            abilities[i].abilityType = (EnemyAbilityType)EditorGUILayout.EnumPopup("Type", abilities[i].abilityType);
            abilities[i].baseCooldown = EditorGUILayout.IntField("Cooldown (Turns)", abilities[i].baseCooldown);
            abilities[i].multiplier = EditorGUILayout.FloatField("Effect Multiplier (x)", abilities[i].multiplier);
            abilities[i].priority = EditorGUILayout.IntField("AI Priority", abilities[i].priority);

            if (GUILayout.Button("Remove This Ability (X)", GUILayout.Width(150)))
            {
                abilities.RemoveAt(i);
                break;
            }
            GUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        savePath = EditorGUILayout.TextField("Save Folder Path", savePath);
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate and Save Enemy Monster Asset", GUILayout.Height(40)))
        {
            CreateEnemyAsset();
        }
    }

    private void CreateEnemyAsset()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        CharacterData newEnemy = ScriptableObject.CreateInstance<CharacterData>();
        newEnemy.characterName = enemyName;
        newEnemy.maxHealth = maxHealth;
        newEnemy.baseAttackDamage = baseAttackDamage;
        newEnemy.visualPrefab = visualPrefab;
        newEnemy.icon = icon;
        
        // Переносим созданные в окне абилки в ассет
        newEnemy.enemyAbilities = new List<EnemyAbility>(abilities);

        string fullPath = $"{savePath}{enemyID.Trim()}.asset";

        AssetDatabase.CreateAsset(newEnemy, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newEnemy;

        Debug.Log($"<color=red>[ИНСТРУМЕНТ]</color> Враг успешно сгенерирован и сохранен: {fullPath}");
    }
}

using UnityEngine;
using UnityEditor; 
using System.IO;
using System.Collections.Generic;

public class CardCreatorTool : EditorWindow
{
    private string cardID = "card_01";
    private string cardName = "New Card";
    private string description = "Card description here...";
    private int apCost = 1;
    private CardType cardType = CardType.Attack;
    private string characterName = "Hero";
    private Sprite cardArt;
    private int baseDamage = 10;
    private int baseShield = 0;

    private string savePath = "Assets/Resources/Cards/";

    [MenuItem("Chaos Tools/Card Creator Window")]
    public static void ShowWindow()
    {
        GetWindow<CardCreatorTool>("Card Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Chaos Zero Nightmare — Card Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        cardID = EditorGUILayout.TextField("Card ID (Unique)", cardID);
        cardName = EditorGUILayout.TextField("Card Name", cardName);
        
        GUILayout.Label("Description:");
        description = EditorGUILayout.TextArea(description, GUILayout.Height(60));
        EditorGUILayout.Space();

        apCost = EditorGUILayout.IntField("AP Cost", apCost);
        cardType = (CardType)EditorGUILayout.EnumPopup("Card Type", cardType);
        characterName = EditorGUILayout.TextField("Character Owner", characterName);
        EditorGUILayout.Space();

        cardArt = (Sprite)EditorGUILayout.ObjectField("Card Artwork", cardArt, typeof(Sprite), false);
        baseDamage = EditorGUILayout.IntField("Base Damage", baseDamage);
        baseShield = EditorGUILayout.IntField("Base Shield", baseShield);
        EditorGUILayout.Space();

        savePath = EditorGUILayout.TextField("Save Folder Path", savePath);
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate and Save Card Asset", GUILayout.Height(35)))
        {
            CreateCardAsset();
        }

        EditorGUILayout.Space();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2)); 
        EditorGUILayout.Space();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Fix and Re-Save All Cards in Folder", GUILayout.Height(40)))
        {
            FixAndResaveAllCards();
        }
        GUI.backgroundColor = Color.white;
    }

    private void CreateCardAsset()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        CardData newCard = ScriptableObject.CreateInstance<CardData>();
        FillCardWithCurrentData(newCard);

        // ИСПРАВЛЕНО: Файл на диске теперь называется СТРОГО по ID (например, "strike.asset")
        // Это полностью убирает рассинхронизацию с текстовым файлом колоды
        string fullPath = $"{savePath}{cardID.Trim()}.asset";

        AssetDatabase.CreateAsset(newCard, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newCard;

        Debug.Log($"<color=green>[ИНСТРУМЕНТ]</color> Карта успешно создана: {fullPath}");
    }

    private void FixAndResaveAllCards()
    {
        if (!Directory.Exists(savePath)) return;

        string[] fileEntries = Directory.GetFiles(savePath, "*.asset");
        
        if (fileEntries.Length == 0)
        {
            Debug.LogWarning("[ИНСТРУМЕНТ] В папке нет файлов с расширением .asset!");
            return;
        }

        List<string> assetPaths = new List<string>();
        foreach (string filePath in fileEntries)
        {
            string normalizedPath = filePath.Replace("\\", "/");
            assetPaths.Add(normalizedPath);
        }

        AssetDatabase.ForceReserializeAssets(assetPaths);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan>[ИНСТРУМЕНТ]</color> Ультимативная реанимация завершена! Принудительно обновлено файлов в папке: {assetPaths.Count}.");
    }

    private void FillCardWithCurrentData(CardData card)
    {
        card.cardID = cardID;
        card.cardName = cardName;
        card.description = description;
        card.apCost = apCost;
        card.cardType = cardType;
        card.characterName = characterName;
        card.cardArt = cardArt;
        card.baseDamage = baseDamage;
        card.baseShield = baseShield;
    }
}

using UnityEngine;
using UnityEditor;
using System.IO;

public class CharacterCreatorTool : EditorWindow
{
    // Переменные для хранения вводимых данных персонажа
    private string characterID = "hero_warrior";
    private string characterName = "Warrior";
    private GameObject visualPrefab;
    private Sprite icon;

    // Путь для автоматического сохранения персонажей в проекте
    private string savePath = "Assets/Resources/Characters/";

    // Добавляем пункт в верхнее меню Unity
    [MenuItem("Chaos Tools/Character Creator Window")]
    public static void ShowWindow()
    {
        // Открываем окно редактора
        GetWindow<CharacterCreatorTool>("Character Creator");
    }

    // Отрисовка интерфейса окна инструмента
    private void OnGUI()
    {
        GUILayout.Label("Chaos Zero Nightmare — Character Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Поля ввода базовой информации
        characterID = EditorGUILayout.TextField("Character ID (Unique)", characterID);
        characterName = EditorGUILayout.TextField("Character Name", characterName);
        EditorGUILayout.Space();

        // Поля для привязки префаба 3D-модели и иконки
        visualPrefab = (GameObject)EditorGUILayout.ObjectField("Visual 3D Prefab", visualPrefab, typeof(GameObject), false);
        icon = (Sprite)EditorGUILayout.ObjectField("Character Icon (UI)", icon, typeof(Sprite), false);
        EditorGUILayout.Space();

        // Настройка папки сохранения
        savePath = EditorGUILayout.TextField("Save Folder Path", savePath);
        EditorGUILayout.Space();

        // Кнопка генерации ассета персонажа
        if (GUILayout.Button("Generate and Save Character Asset", GUILayout.Height(40)))
        {
            CreateCharacterAsset();
        }
    }

    private void CreateCharacterAsset()
    {
        // Если папки для сохранения не существует в проекте — создаем её автоматически
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // Генерируем экземпляр ScriptableObject, используя класс CharacterData из BattleEffectManager
        CharacterData newCharacter = ScriptableObject.CreateInstance<CharacterData>();

        // Заполняем созданный файл данными из полей окна инструмента
        newCharacter.characterName = characterName;
        newCharacter.visualPrefab = visualPrefab;
        newCharacter.icon = icon;
        // Список стартовых карт characterCards оставляем пустым, его можно настраивать в инспекторе ассета

        // Формируем уникальное имя файла строго по его ID (без склеек с именем, чтобы Deck/Spawn менеджеры легко его читали)
        string fullPath = $"{savePath}{characterID.Trim()}.asset";

        // Сохраняем ассет в файлы проекта Unity
        AssetDatabase.CreateAsset(newCharacter, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Автоматически фокусируемся на созданном файле в окне Project
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newCharacter;

        Debug.Log($"<color=green>[ИНСТРУМЕНТ]</color> Персонаж успешно сгенерирован и сохранен по пути: {fullPath}");
    }
}

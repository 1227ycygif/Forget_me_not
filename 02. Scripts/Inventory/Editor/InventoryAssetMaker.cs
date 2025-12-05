#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// ItemUse 프리팹 + ItemEffectAsset(SO) 생성용 간단 툴킷
/// </summary>
public class ItemToolkitWindow : EditorWindow
{
    const string DEFAULT_FOLDER = "Assets/Items";

    string saveFolder = DEFAULT_FOLDER;

    // ItemUse 생성 입력
    string itemKey = "new_item";
    bool itemConsumable = true;

    // EquipEffect 입력
    GameObject equipPrefab;

    // ChargeEffect 입력
    int ChargeAmount = 1;

    // // SpawnEffect 입력
    // GameObject spawnPrefab;
    // Vector3 spawnLocalOffset = new Vector3(0, 0, 1);

    [MenuItem("Tools/Inventory/Item Toolkit")]
    static void Open()
    {
        var w = GetWindow<ItemToolkitWindow>("Item Toolkit");
        w.minSize = new Vector2(420, 340);
    }

    void OnGUI()
    {
        // 저장 폴더
        GUILayout.Label("저장 폴더", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        saveFolder = EditorGUILayout.TextField(saveFolder);
        if (GUILayout.Button("폴더 선택", GUILayout.Width(90)))
        {
            string p = EditorUtility.OpenFolderPanel("폴더 선택", Application.dataPath, "");
            if (!string.IsNullOrEmpty(p))
            {
                if (p.StartsWith(Application.dataPath))
                {
                    saveFolder = "Assets" + p.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("경고", "Assets 폴더 내부만 선택 가능합니다.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        DrawItemUseSection();

        EditorGUILayout.Space();
        DrawEffectSection();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "워크플로우 예시\n" +
            "1) ItemUse 프리팹 생성(키/소모여부 입력)\n" +
            "2) 필요한 효과(Equip/Heal/Spawn) SO 생성\n" +
            "3) ItemUse 컴포넌트의 effects 배열에 SO들 드래그\n" +
            "4) ItemUseRegistry.handlers에 해당 ItemUse 컴포넌트 등록",
            MessageType.Info);
    }

    void DrawItemUseSection()
    {
        GUILayout.Label("ItemUse 프리팹 생성", EditorStyles.boldLabel);
        itemKey = EditorGUILayout.TextField("Key", itemKey);
        itemConsumable = EditorGUILayout.Toggle("Consumable", itemConsumable);

        if (GUILayout.Button("ItemUse 프리팹 만들기", GUILayout.Height(26)))
        {
            if (string.IsNullOrEmpty(itemKey))
            {
                EditorUtility.DisplayDialog("오류", "Key를 입력하세요.", "OK");
                return;
            }

            EnsureFolder(saveFolder);

            // 빈 GameObject 생성 후 ItemUse 부착
            var go = new GameObject($"item_{itemKey}");
            var iu = go.AddComponent<ItemUse>();

            // 리플렉션 없이 안전하게 설정
            Undo.RegisterCreatedObjectUndo(go, "Create ItemUse Prefab");
            // 직렬화된 객체로 세팅
            var so = new SerializedObject(iu);
            so.FindProperty("key").stringValue = itemKey;
            so.FindProperty("consumable").boolValue = itemConsumable;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Prefab 저장
            string unique = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(saveFolder, $"{go.name}.prefab"));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, unique);
            DestroyImmediate(go);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
    }

    void DrawEffectSection()
    {
        GUILayout.Label("Effect SO 생성", EditorStyles.boldLabel);

        // Equip
        EditorGUILayout.LabelField("EquipEffect", EditorStyles.miniBoldLabel);
        equipPrefab = (GameObject)EditorGUILayout.ObjectField("Equip Prefab", equipPrefab, typeof(GameObject), false);
        if (GUILayout.Button("EquipEffect SO 만들기"))
        {
            var asset = CreateEffectAsset<EquipEffectAsset>("EquipEffect");
            if (asset != null)
            {
                var so = new SerializedObject(asset);
                so.FindProperty("equipPrefab").objectReferenceValue = equipPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                Selection.activeObject = asset;
            }
        }
        EditorGUILayout.Space(8);

        // Charge
        EditorGUILayout.LabelField("ChargeEffect", EditorStyles.miniBoldLabel);
        ChargeAmount = EditorGUILayout.IntField("Amount", ChargeAmount);
        if (GUILayout.Button("ChargeEffect SO 만들기"))
        {
            var asset = CreateEffectAsset<ChargeEffectAsset>("ChargeEffect");
            if (asset != null)
            {
                var so = new SerializedObject(asset);
                so.FindProperty("amount").intValue = Mathf.Max(1, ChargeAmount);
                so.ApplyModifiedPropertiesWithoutUndo();
                Selection.activeObject = asset;
            }
        }

        // 추가
    }

    T CreateEffectAsset<T>(string baseName) where T : ScriptableObject
    {
        EnsureFolder(saveFolder);
        var asset = ScriptableObject.CreateInstance<T>();
        string unique = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(saveFolder, $"{baseName}.asset"));
        AssetDatabase.CreateAsset(asset, unique);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(asset);
        return asset;
    }

    // 유틸
    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string[] parts = folder.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif

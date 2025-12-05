using UnityEngine;
using UnityEngine.Video;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// 각 컷씬의 정보를 담는 Asset을 생성하는 SO
/// </summary>

public enum CutsceneType
{
    Video,
    Dialogue
}

// 씬 순서를 여기서 잘 설정해주자, 씬 에셋에서 설정해주는 것도 잊지 말고
public enum CutsceneName
{
    Test,
    Intro,
    Dialog1,
    Dialog2,
    Ending,
    Elma,
    Amy
}

[CreateAssetMenu(
    fileName = "CutsceneConfig",
    menuName = "Cutscenes/CutsceneConfig",
    order = 0)]
public class CutsceneConfig : ScriptableObject
{
    [Header("식별자 (enum 또는 int ID)")]
    public CutsceneName sceneName;                     // 컷신 번호 or enum의 int 값

    [Header("컷신 유형 선택")]
    public CutsceneType cutsceneType;

    [Header("다음으로 이동할 씬 이름")]
    public string nextScene;           // 컷신 종료 후 이동할 씬

    public CutsceneName nextCutScene;

    [Header("스킵 가능 여부")]
    public bool skippable = true;      // 스킵 가능 여부


    [Header("비디오 컷신을 위한 옵션들")]
    [Header("영상 클립")]
    public VideoClip videoClip;        // VideoPlayer용 클립

    [Header("대화 컷신을 위한 옵션들")]
    [Header("배경음악")]
    public int bgmIdx;                  // BGM용 클립 인덱스 (없으면 무음)

    [Header("기본 배경 이미지")]
    public Sprite background;

    [Header("기본 캐릭터 초상화 (여자)")]
    public Sprite girlCharacterSprite;
    [Header("기본 캐릭터 초상화 (남자)")]
    public Sprite boyCharacterSprite;

    [Header("대사 라인들")]
    public DialogueLine[] lines;

    /// <summary>
    /// 컷신 정보를 출력용 문자열로 반환.
    /// </summary>
    public override string ToString()
    {
        return $"CutsceneConfig(sceneName={sceneName}, CutsceneType={cutsceneType}, next={nextScene}, skippable={skippable})";
    }
}

[System.Serializable]
public class DialogueLine
{
    [Header("말하는 사람 이름")]
    public string speakerName;

    [Header("독백입니까?")]
    public bool isSolo;

    [Header("텍스트")]
    [TextArea(2, 4)]
    public string text;

    [Header("이 라인에서만 쓸 초상화 (null이면 기본 사용)")]
    public Sprite overrideCharacterSprite;

    [Header("이 라인에서만 쓸 배경 (null이면 기본 사용)")]
    public Sprite overrideBackgroundSprite;

    [Header("보이스, 효과음 (선택)")]
    public AudioClip soundClip;

    [Header("자동 진행 시간 (0이면 입력 대기)")]
    public float autoNextDelay = 0f;
}

#if UNITY_EDITOR
[CustomEditor(typeof(CutsceneConfig))]
public class CutsceneConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var typeProp = serializedObject.FindProperty("cutsceneType");
        EditorGUILayout.PropertyField(typeProp);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneName"));

        var type = (CutsceneType)typeProp.enumValueIndex;
        if (type == CutsceneType.Video)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("videoClip"));
        }
        else  // Dialog
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bgmIdx"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("background"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("girlCharacterSprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("boyCharacterSprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lines"), true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nextScene"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nextCutScene"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("skippable"));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
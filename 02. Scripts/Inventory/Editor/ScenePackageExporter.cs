// Assets/Editor/ScenePackageExporter.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ScenePackageExporter : EditorWindow
{
    [Serializable]
    class ExportState
    {
        public string mdFilePath = "";
        public List<string> scenePaths = new List<string>();
        public List<string> assetPaths = new List<string>();
        public List<string> extraIncludeFolders = new List<string>(); // 선택 폴더 추가 포함
        public List<string> excludePatterns = new List<string> { "/Gizmos/", "/Editor Default Resources/" };
        public bool includeScripts = true;
        public bool includeEditorAssets = false;
        public bool verboseLog = false;
    }

    ExportState state = new ExportState();
    Vector2 scroll;

    [MenuItem("Tools/Scene Package Exporter")]
    public static void Open() => GetWindow<ScenePackageExporter>("Scene Package Exporter");

    void OnGUI()
    {
        EditorGUILayout.LabelField("씬 기반 자동 패키지 내보내기", EditorStyles.boldLabel);

        // MD 파일 지정
        EditorGUILayout.BeginHorizontal();
        state.mdFilePath = EditorGUILayout.TextField("SceneInventory.md", state.mdFilePath);
        if (GUILayout.Button("찾기", GUILayout.MaxWidth(80)))
        {
            var p = EditorUtility.OpenFilePanel("Select SceneInventory.md", Application.dataPath, "md");
            if (!string.IsNullOrEmpty(p))
            {
                // Assets 상대경로는 아니어도 됨(읽기만 함)
                state.mdFilePath = p;
            }
        }
        EditorGUILayout.EndHorizontal();

        // 옵션
        state.includeScripts = EditorGUILayout.ToggleLeft("스크립트(.cs) 포함", state.includeScripts);
        state.includeEditorAssets = EditorGUILayout.ToggleLeft("Editor 폴더 포함(에디터 유틸 공유 필요 시만)", state.includeEditorAssets);
        state.verboseLog = EditorGUILayout.ToggleLeft("Verbose 로그", state.verboseLog);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("추가로 포함할 폴더(선택):");
        if (GUILayout.Button("+ 폴더 추가")) AddFolder();
        for (int i = 0; i < state.extraIncludeFolders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            state.extraIncludeFolders[i] = EditorGUILayout.TextField(state.extraIncludeFolders[i]);
            if (GUILayout.Button("X", GUILayout.MaxWidth(24))) { state.extraIncludeFolders.RemoveAt(i); i--; }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("제외 패턴(경로에 포함되면 제외):");
        for (int i = 0; i < state.excludePatterns.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            state.excludePatterns[i] = EditorGUILayout.TextField(state.excludePatterns[i]);
            if (GUILayout.Button("X", GUILayout.MaxWidth(24))) { state.excludePatterns.RemoveAt(i); i--; }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ 제외 패턴 추가")) state.excludePatterns.Add("");

        EditorGUILayout.Space(10);
        // 동작 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Parse MD", GUILayout.Height(26))) ParseMdForScenes();
        using (new EditorGUI.DisabledScope(state.scenePaths.Count == 0))
        {
            if (GUILayout.Button("Build Asset List", GUILayout.Height(26))) BuildAssetList();
        }
        using (new EditorGUI.DisabledScope(state.assetPaths.Count == 0))
        {
            if (GUILayout.Button("Export .unitypackage", GUILayout.Height(26))) DoExport();
        }
        EditorGUILayout.EndHorizontal();

        // 미리보기
        EditorGUILayout.Space(10);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField($"Scenes ({state.scenePaths.Count})", EditorStyles.boldLabel);
        foreach (var s in state.scenePaths) EditorGUILayout.LabelField("• " + s);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Assets to Export ({state.assetPaths.Count})", EditorStyles.boldLabel);
        foreach (var a in state.assetPaths) EditorGUILayout.LabelField("• " + a);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "주의:\n" +
            "- ProjectSettings, Packages 폴더는 .unitypackage로 내보낼 수 없음(유니티 제한).\n" +
            "- 커스텀 태그/레이어/URP/HDRP 설정이 필요하면, README.md에 적용 방법을 함께 제공하세요.",
            MessageType.Info);
    }

    void AddFolder()
    {
        var p = EditorUtility.OpenFolderPanel("추가 포함할 Assets 폴더 선택", Application.dataPath, "");
        if (!string.IsNullOrEmpty(p) && p.StartsWith(Application.dataPath))
        {
            var rel = "Assets" + p.Substring(Application.dataPath.Length);
            if (!state.extraIncludeFolders.Contains(rel))
                state.extraIncludeFolders.Add(rel);
        }
        else if (!string.IsNullOrEmpty(p))
        {
            EditorUtility.DisplayDialog("경고", "Assets 폴더 내부만 선택하세요.", "확인");
        }
    }

    void ParseMdForScenes()
    {
        state.scenePaths.Clear();
        if (string.IsNullOrEmpty(state.mdFilePath) || !File.Exists(state.mdFilePath))
        {
            EditorUtility.DisplayDialog("오류", "유효한 .md 파일을 선택하세요.", "확인");
            return;
        }

        // "## Assets/.../X.unity" 형태를 추출
        var rx = new Regex(@"^##\s+(Assets/.+?\.unity)\s*$", RegexOptions.Multiline);
        var text = File.ReadAllText(state.mdFilePath);
        var matches = rx.Matches(text);

        foreach (Match m in matches)
        {
            var path = m.Groups[1].Value.Trim();
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
                state.scenePaths.Add(path);
            else
                Debug.LogWarning($"[ScenePackageExporter] 씬 경로가 프로젝트에 없음: {path}");
        }

        state.scenePaths = state.scenePaths.Distinct().ToList();
        if (state.scenePaths.Count == 0)
            EditorUtility.DisplayDialog("알림", "MD에서 씬 경로를 찾지 못했습니다. (## Assets/... .unity 형식)", "확인");
        else
            Debug.Log($"[ScenePackageExporter] MD 파싱 완료: {state.scenePaths.Count} scene(s).");
    }

    void BuildAssetList()
    {
        if (state.scenePaths.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "먼저 Parse MD를 실행해 씬 목록을 확보하세요.", "확인");
            return;
        }

        // 씬 자체 + 추가 폴더 포함 대상 구성
        var seedPaths = new List<string>();
        seedPaths.AddRange(state.scenePaths);

        // 추가 폴더 내 모든 자산 추가(폴더 그 자체를 GetDependencies에 넘기면 폴더는 무시되므로, 내부 에셋을 나열)
        foreach (var folder in state.extraIncludeFolders.Where(p => p.StartsWith("Assets")))
        {
            var guids = AssetDatabase.FindAssets("", new[] { folder });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (AssetDatabase.IsValidFolder(p)) continue;
                seedPaths.Add(p);
            }
        }

        // 의존성 수집
        var deps = AssetDatabase.GetDependencies(seedPaths.ToArray(), true)
                                .Where(p => p.StartsWith("Assets/"))
                                .Distinct()
                                .ToList();

        // 필터링
        deps = deps.Where(KeepPath).ToList();

        // 스크립트 제외 옵션
        if (!state.includeScripts)
            deps = deps.Where(p => !p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();

        // Editor 폴더 제외 옵션
        if (!state.includeEditorAssets)
            deps = deps.Where(p => !IsUnderEditorFolder(p)).ToList();

        state.assetPaths = deps;

        if (state.verboseLog)
        {
            Debug.Log("[ScenePackageExporter] === ASSETS ===\n" + string.Join("\n", state.assetPaths));
        }

        EditorUtility.DisplayDialog("완료", $"의존 자산 수집 완료: {state.assetPaths.Count}개", "확인");
    }

    bool KeepPath(string path)
    {
        // 제외 패턴
        foreach (var pattern in state.excludePatterns)
        {
            if (string.IsNullOrEmpty(pattern)) continue;
            if (path.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0) return false;
        }

        // ExportPackage로 내보낼 수 없는 범위 제외
        if (path.StartsWith("Packages/")) return false; // 패키지 폴더는 .unitypackage에 포함 불가
        if (path.StartsWith("ProjectSettings/")) return false;

        return true;
    }

    bool IsUnderEditorFolder(string assetPath)
    {
        // 경로 중간에 /Editor/ 포함 여부
        var segments = assetPath.Split('/');
        for (int i = 0; i < segments.Length; i++)
        {
            if (string.Equals(segments[i], "Editor", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    void DoExport()
    {
        if (state.assetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "먼저 Build Asset List를 실행하세요.", "확인");
            return;
        }

        var save = EditorUtility.SaveFilePanel("Export .unitypackage", Application.dataPath, "ScenePackage", "unitypackage");
        if (string.IsNullOrEmpty(save)) return;

        AssetDatabase.ExportPackage(state.assetPaths.ToArray(), save, ExportPackageOptions.Default);
        EditorUtility.RevealInFinder(save);
        Debug.Log($"[ScenePackageExporter] 내보내기 완료: {save}\nAssets: {state.assetPaths.Count}");
    }
}

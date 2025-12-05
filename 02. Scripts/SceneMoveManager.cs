using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneMoveManager : MonoBehaviour
{
    public static SceneMoveManager Instance { get; private set; }

    [Header("Transition")]
    [SerializeField] Animator transition;       // Panel에 붙은 Animator
    [SerializeField] float minCoverTime = 2f;   // 페이드 인 보장 시간
    [SerializeField] Image progress;
    [SerializeField] Text msgTip;

    [SerializeField] bool nowLoading = false;          // 씬 전환중 조회용
    [SerializeField] CutsceneName cutscene;           // 씬 전환 변수, 컷신 재생용으로 사용
    
    [Header("Photon 연동 (옵션)")]
    [Tooltip("Photon 방 내부에서 모든 플레이어가 준비 완료 후 동시에 씬 활성화할지 여부")]
    public bool usePhotonSync = true;

    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName) => StartCoroutine(LoadRoutine(sceneName, null));
    public void LoadScene(string sceneName, CutsceneName cutscene) => StartCoroutine(LoadRoutine(sceneName, cutscene));

    IEnumerator LoadRoutine(string sceneName, CutsceneName? cut)
    {
        // 컷신 재생 시 조회할 씬 이름 지정
        if (cut != null)
            cutscene = cut.Value;

        // 씬 전환 시작
        nowLoading = true;

        // 트랜지션 활성화
        if (transition) 
            transition.gameObject.SetActive(true);

        // 덮기
        yield return FadeIn();

        // 비동기 로드 시작
        var op = SceneManager.LoadSceneAsync(sceneName);
        //op.allowSceneActivation = false;

        // 새 씬에서 오브젝트 재할당
        yield return OnLoadNewScene();

        // 진행도 표시
        while (op.progress < 0.9f)
        {
            UpdateProgress(op.progress);
            Debug.Log("로딩중");
            yield return null;
        }

        UpdateProgress(1f);
        yield return new WaitForSeconds(1f);

        Debug.Log("로딩 완료");

        // Photon 대기 구간
        //if (PhotonNetwork.room != null)
        //    yield return WaitExternalReadyIfAny();
        //else
        //    Debug.Log("[SceneMoveManager] Photon Sync 비활성화 상태, 즉시 활성화 진행");

        // 씬 활성화
        //yield return new WaitForSeconds(1f);
        //op.allowSceneActivation = true;
        Debug.Log("[SceneMoveManager] New Scene Loaded");

        // 덮기 해제
        yield return FadeOut();

        // 씬 전환 완료
        nowLoading = false;

        yield break;
    }

    void UpdateProgress(float p)
    {
        if (progress) progress.fillAmount = p;
        //if (msgTip) msgTip.text = Mathf.RoundToInt(p * 100f) + "%";
    }

    IEnumerator OnLoadNewScene()
    {
        yield return new WaitForSeconds(2f);

        while (transition == null)
        {
            Debug.Log("못찾았어");
            yield return new WaitForSeconds(0.5f);
            transition = GameObject.FindGameObjectWithTag("Transition").GetComponent<Animator>();
        }
        Image[] imgs = transition.gameObject.GetComponentsInChildren<Image>(true);
        foreach (var img in imgs)
        {
            if (img.gameObject.name == "imgProgress")
                progress = img;
        }
        Text[] txts = transition.gameObject.GetComponentsInChildren<Text>(true);
        foreach (var txt in txts)
        {
            if (txt.gameObject.name == "txtTip")
                msgTip = txt;
        }

        if (transition == null || progress == null || msgTip == null)
            //Debug.LogWarning("[SceneMoveManager] 트랜지션 UI 일부를 찾지 못함");

        Debug.Log("전환 씬에서 UI 불러오기 완료!");
        if (msgTip) msgTip.text += "이곳에 게임 팁 입력";
        yield return new WaitForSeconds(1f);
    }

    IEnumerator FadeIn()
    {
        if (transition) transition.SetTrigger("FadeIn");
        float coverTimer = 0f;
        while (coverTimer < minCoverTime)
        {
            coverTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        yield break;
    }
    IEnumerator FadeOut()
    {
        if (transition) transition.SetTrigger("FadeOut");
        yield return new WaitForSeconds(2f);
        if (transition) transition.gameObject.SetActive(false);
        Debug.Log("FadeOut 실행완료");

    }

    public CutsceneName GetSceneName()
    {
        Debug.Log($"씬 넘긴 횟수 조회됨 : {cutscene}");
        return cutscene;
    }



    // 외부(네트워크 등) 준비가 필요 없으면 바로 리턴
    protected virtual IEnumerator WaitExternalReadyIfAny() { yield break; }
}

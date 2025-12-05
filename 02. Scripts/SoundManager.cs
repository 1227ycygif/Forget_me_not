using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

[RequireComponent(typeof(PhotonView))]

public class SoundManager : MonoBehaviour
{
    PhotonView pv;

    public static SoundManager manager { get; private set; }

    [Header("믹서 그룹 연결")]
    public AudioMixer mixer;
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Scene 전환 옵션 (체크 권장)")]
    [Tooltip("씬 전환 시 동적 생성한 BGM/SFX 객체를 모두 정리")]
    bool destroyTempsOnScene = true;

    // 동적 생성한 오디오 객체 추적용
    List<GameObject> sfxObjects = new();
    List<GameObject> bgmObjects = new();

    // 클릭 효과음은 고정 변수로 (계속 쓰니까 파괴 생성 부담됨)
    [Header("버튼 클릭음 연결")]
    public AudioSource SFXClick;

    [Header("SFXClip")]
    public AudioClip[] SFX;

    [Header("BGMClip")]
    public AudioClip[] BGM;

    private int MasterStep = 10;
    private int BGMStep = 5;
    private int SFXStep = 5;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (manager != null && manager != this)
        {
            Destroy(gameObject);
            return;
        }
        manager = this;

        DontDestroyOnLoad(gameObject);
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!destroyTempsOnScene) return;
        DestroyAllTemps(); // 씬이 바뀌면 SFX/BGM 임시 객체 싹 정리
    }

    private void Start()
    {

    }

    // SFX 클립 재생할 AudioSource 객체 동적 생성 (기본 No Loop)
    [PunRPC]
    public void PlaySFXAtPoint(int index, Vector3 pos, bool isLoop = false)
    {
        if (!sfxGroup)
        {
            Debug.LogWarning("담을 그룹 자체가 없는데?? (SFX)");
            return;
        }

        GameObject obj = new GameObject("SFX_" + index);
        obj.transform.position = pos;

        AudioSource src = obj.AddComponent<AudioSource>();
        src.clip = SFX[index];
        src.outputAudioMixerGroup = sfxGroup;   // ★ 여기서 믹서 지정
        src.loop = isLoop;
        src.Play();
        // 리스트에 추가, 일괄 처리용
        sfxObjects.Add(obj);
        // 자동 파괴 코루틴
        StartCoroutine(SfxAutoDestroy(obj, src.clip.length + 0.2f));         // 0.2초 여유
        Debug.Log(src.clip.length);
    }

    // 게임 전체에 동시에 들려야하는 경우 호출 (SFX)
    public void SFXPlay(int index, Vector3 pos, bool isLoop = false)
    {
        pv.RPC(nameof(PlaySFXAtPoint), PhotonTargets.All, index, pos, isLoop);
    }


    // BGM 클립 재생할 AudioSource 객체 동적 생성 (기본 Loop)
    [PunRPC]
    public void PlayBGMAtPoint(int index, Vector3 pos, bool isLoop = true)
    {
        if (!bgmGroup)
        {
            Debug.LogWarning("담을 그룹 자체가 없는데?? (BGM)");
            return;
        }

        GameObject obj = new GameObject("BGM_" + index);
        obj.transform.position = pos;

        AudioSource src = obj.AddComponent<AudioSource>();
        src.clip = BGM[index];
        src.outputAudioMixerGroup = bgmGroup;   // ★ 여기서 믹서 지정
        src.loop = isLoop;
        src.Play();

        // 리스트에 추가, 일괄 처리용
        bgmObjects.Add(obj);
    }

    // 게임 전체에 동시에 들려야하는 경우 호출 (BGM)
    public void BGMPlay(int index, Vector3 pos, bool isLoop = true)
    {
        if (PhotonNetwork.isMasterClient)
            pv.RPC(nameof(PlayBGMAtPoint), PhotonTargets.All, index, pos, isLoop);
    }
    // 클릭음은 Destroy 없고 
    public void Click()
    {
        if (!SFXClick) return;

        SFXClick.Play();
    }

    public void SFXAllStop()
    {
        DestroyTracked(sfxObjects);
    }

    public void BGMAllStop()
    {
        DestroyTracked(bgmObjects);
    }

    public void DestroyAllTemps()
    {
        DestroyTracked(sfxObjects);
        DestroyTracked(bgmObjects);
    }

    void DestroyTracked(List<GameObject> list)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            GameObject obj = list[i];
            if (obj) Destroy(obj);
        }
        list.Clear();
    }

    bool BGMSearch()
    {

        return false;
    }

    bool SFXSearch()
    {
        return false;
    }

    // 리스트에서 빈거 제거해주기
    void CleanupNulls(List<GameObject> list)
    {
        if (list == null) return;

        list.RemoveAll(go => go == null);
    }

    IEnumerator SfxAutoDestroy(GameObject obj, float clipLength)
    {
        // 클립 출력 대기
        yield return new WaitForSeconds(clipLength);

        // 리스트에서 제거
        if (sfxObjects.Contains(obj))
            sfxObjects.Remove(obj);

        // 오브젝트 파괴
        if (obj != null)
            Destroy(obj);

        yield break;
    }

    public void SetVolumeStep(string param, int step)
    {
        switch (param)
        {
            case "Master":
                MasterStep = step;
                break;
            case "BGM":
                BGMStep = step;
                break;
            case "SFX":
                SFXStep = step;
                break;
        }
    }

    public int GetVolumeStep(string param)
    {
        int step = 5;  // 이게 기본 세팅값이니까 5반환

        switch (param)
        {
            case "Master":
                step = MasterStep;
                break;
            case "BGM":
                step = BGMStep;
                break;
            case "SFX":
                step = SFXStep;
                break;
        }

        return step;
    }
}
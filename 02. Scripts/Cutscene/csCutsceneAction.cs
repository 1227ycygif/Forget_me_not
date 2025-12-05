using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class csCutsceneAction : MonoBehaviour
{
    [Header("컷신 정보 모음")]
    [SerializeField] private CutsceneConfig[] cutscenes;

    [Header("대화 컷씬을 위한 Canvas UI 참조")]
    [SerializeField] private GameObject backplate;  // 가림막
    [SerializeField] private Image imageBack;       // 배경
    [SerializeField] private Image imageGirl;       // 캐릭터 상반신
    [SerializeField] private Image imageBoy;        // 캐릭터 상반신
    [SerializeField] private GameObject canvasDialog;   // 문자 객체 최상위
    [SerializeField] private Text txtName;          // 이름
    [SerializeField] private Text txtDialog;        // 대사
    [SerializeField] private int bgmIdx;            // 브금
    [SerializeField] private AudioClip sfxSource; // 효과음

    [Header("영상 컷씬을 위한 Video Player 참조")]
    [SerializeField] private VideoPlayer video;
    // video.Play() 만 해주면 비디오는 즉시 재생 가능

    // 포톤 할거면 당연히 달아줘야하는
    PhotonView pv;
    // 현재 컷신 정보를 뽑아내 저장
    [Header("현재 컷씬")]
    [SerializeField]
    CutsceneConfig currentCutscene;
    CutsceneName nextScene;

    // 대화 컷신 출력 시 표정 변화 시 사용할 스프라이트
    Sprite spGirl;
    Sprite spBoy;
    Sprite spBack;
    // 대화 컷신 출력 시 표정 변화 후 복구할 시 필요한 원본
    Sprite origGirl;
    Sprite origBoy;
    Sprite origBack;

    // 상대방의 컷씬 재생 완료 여부 확인용 변수
    bool isReadyNow = false;
    // 업데이트에서 코루틴 무한선언한다!!! 막아!!!!!
    bool isLoadedOnce = false;
    // 대화를 넘기며 재생하기 위한 인덱스 변수 
    int idxDialog = 0;
    // 대화를 넘기고 대기시간 걸기
    float waitCount = 0f;
    // 설정할 대기시간
    [Range(0f, 10f)]
    public float inputDelay = 5f;

    /// <summary>
    /// 동작 순서
    /// 1. video & 컷씬 캔버스 할당받기
    /// 2. 둘 다 있는지 확인
    /// 3. SceneMoveManager.sceneLoaded 조회
    /// 4. 현재 재생할 컷씬을 선택
    /// - 비디오라면 비디오 클립을 선택
    /// - 캔버스라면 정해진 대사 플로우 선택
    /// 5. 재생
    /// 6. 재생 완료 후 재생 완료했음을 알린다, RPC 동기화  (서브는 보내기만, 마스터는 불필요)
    /// 7. 모든 플레이어가 준비 완료 상태라면 다음 씬으로 전환 (씬 전환은 무조건 마스터가)
    /// </summary>

    // 비디오 클립 전환 방식은 어떻게 할까?
    // G선생님께 문의한 결과, 

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void Start()
    {
        // 컷씬 등록 확인
        if (cutscenes.Length <= 0 || cutscenes == null)
        {
            Debug.LogError("CutsceneConfig 미지정");
            return;
        }

        StartCoroutine(SetCutsceneConfig());
    }

    IEnumerator SetCutsceneConfig()
    {
        // 현재 씬 번호와 일치하는 컷신 조회
        while (currentCutscene == null)
        {
            yield return new WaitForSeconds(0.5f);

            foreach (var cut in cutscenes)
            {
                // 디버그용
                if (cut.sceneName == CutsceneName.Test)
                {
                    currentCutscene = cut;
                    break;
                }

                if (cut.sceneName == SceneMoveManager.Instance.GetSceneName())
                {
                    currentCutscene = cut;
                    break;
                }
            }
        }

        // 원본 이미지 스프라이트 백업
        origGirl = currentCutscene.girlCharacterSprite;
        origBoy = currentCutscene.boyCharacterSprite;
        origBack = currentCutscene.background;

        // 비디오 컷씬 종료 이벤트 구독
        if (video != null)
        {
            video.loopPointReached += OnVideoEnd;
        }

        // 다음 씬 정보 받기
        nextScene = currentCutscene.nextCutScene;

        // 대사 씬 브금 정보 받아오기
        bgmIdx = currentCutscene.bgmIdx;

        // 대사 캔버스 비활성화
        canvasDialog.SetActive(false);

        // 씬 재생 시작
        switch (currentCutscene.cutsceneType)
        {
            case CutsceneType.Video:
                PlayVideo();
                break;

            case CutsceneType.Dialogue:
                PlayDialog();       // Update에서 dialog 출력 인덱스를 하나씩 증가시켜야겠지?
                break;
        }

        yield break;
    }

    void PlayVideo()
    {
        // 가림막 비활성화 (안하면 영상이 안보임)
        backplate.SetActive(false);

        // 대화 컷씬용 캔버스 비활성화
        canvasDialog.SetActive(false);

        // 클립 지정
        video.clip = currentCutscene.videoClip;

        // 비디오 음원 연결
        video.EnableAudioTrack(0, true);                 // 0번 트랙 켜기
        video.SetTargetAudioSource(0, video.gameObject.GetComponent<AudioSource>());         // 0번 트랙 → audioSrc

        // 비디오 재생
        video.Play();
    }

    void PlayDialog()
    {
        // 대화 캔버스 활성화
        canvasDialog.SetActive(true);

        // 비디오 구독 이벤트 해지
        video.loopPointReached -= OnVideoEnd;

        // 가림막 비활성화 (안하면 영상이 안보임)
        backplate.SetActive(false);

        // 브금 재생
        SoundManager.manager.BGMPlay(bgmIdx, this.transform.position);
    }

    void OnVideoEnd(VideoPlayer video)      // 입력받은 비디오는 사용은 안함 (지금은)
    {                                       // 그럼 왜 썼음?  이벤트 구독 오버로드 맞춰주려고 (지워보면 에러로 그 내용을 알 수 있다)
        Debug.Log("비디오 재생에 완료되었습니다. 다음 씬 전환 대기");

        GameManager.Instance.SetState(GameState.Normal);

        // 가림막 활성화 (안하면 뒷 배경이 보임) (근데 앞에서 해서 안해도 될껄?)
        if (backplate.activeSelf == false) backplate.SetActive(true);

        // 마스터라면 씬 전환 실행
        if (PhotonNetwork.isMasterClient)
        {
            // 마스터가 실행할 씬 전환 함수
            StartCoroutine(LoadCheck());
        }
        // 마스터가 아니라면 RPC
        else
        {
            pv.RPC(nameof(GetInReady), PhotonTargets.MasterClient, null);
        }
    }

    void DialogEnd()
    {
        Debug.Log("대화 컷씬 재생에 완료되었습니다. 다음 씬 전환 대기");

        GameManager.Instance.SetState(GameState.Normal);

        // 가림막 활성화 (안하면 뒷 배경이 보임) (근데 앞에서 해서 안해도 될껄?)
        if (backplate.activeSelf == false) backplate.SetActive(true);

        // 브금 종료하기
        SoundManager.manager.BGMAllStop();

        // 마스터라면 씬 전환 실행
        if (PhotonNetwork.isMasterClient)
        {
            // 마스터가 실행할 씬 전환 함수
            StartCoroutine(LoadCheck());
        }
        // 마스터가 아니라면 RPC
        else
        {
            pv.RPC(nameof(GetInReady), PhotonTargets.MasterClient, null);
        }
    }

    IEnumerator LoadCheck()
    {
        while (!isReadyNow)
        {
            yield return null;
        }

        Debug.Log("RPC 전송이 완료 됐으니까 탈출한거임 ㅇㅇ");

        pv.RPC(nameof(LoadNextScene), PhotonTargets.All, null);

        yield return null;
    }

    [PunRPC]
    void GetInReady()
    {
        isReadyNow = true;
    }

    [PunRPC]
    void LoadNextScene()
    {
        SceneMoveManager.Instance.LoadScene(currentCutscene.nextScene, nextScene);
    }

    // 사실상 다이얼로그 재생용으로 쓰는 상황
    private void Update()
    {
        if (currentCutscene == null) return;

        if (currentCutscene.cutsceneType == CutsceneType.Video) return;     // 영상 재생중, 업뎃 필요없음, 돌아가~

        // 엔딩을 한 번 호출했다면 다시는 접근하지 말라
        if (isLoadedOnce) return;

        // 대사를 다 출력하면 DialogEnd()
        if (idxDialog >= currentCutscene.lines.Length)
        {
            isLoadedOnce = true;
            DialogEnd();
            return;
        }

        // 텍스트 적용 파트
        txtName.text = currentCutscene.lines[idxDialog].speakerName;        // 이름
        txtDialog.text = currentCutscene.lines[idxDialog].text;             // 대사

        // 배경 이미지 스프라이트 처리용 조회
        spBack = currentCutscene.lines[idxDialog].overrideBackgroundSprite;
        if (spBack == null)
            imageBack.sprite = origBack;        // 뒷배경 미설정 시 원본 복원
        else
            imageBack.sprite = spBack;          // 뒷배경 설정 시 이미지 조정

        // 캐릭터 이미지 스프라이트 위치 조정 
        if (currentCutscene.lines[idxDialog].isSolo == false)       // Dual Dialog
        {
            // 스프라이트 위치 조정, 양옆
            float dirX = 660f;
            float fixY = imageGirl.rectTransform.anchoredPosition.y;        // boy로 해도 무관
            imageGirl.rectTransform.anchoredPosition = new Vector2(-dirX, fixY);
            imageBoy.rectTransform.anchoredPosition = new Vector2(dirX, fixY);
        }
        else        // Solo Dialog
        {
            // 스프라이트 위치 조정, 중앙
            float fixY = imageGirl.rectTransform.anchoredPosition.y;        // boy로 해도 무관
            imageGirl.rectTransform.anchoredPosition = new Vector2(0f, fixY);
            imageBoy.rectTransform.anchoredPosition = new Vector2(0f, fixY);
        }

        // 캐릭터 이미지 스프라이트 적용 파트
        if (txtName.text == "엘마") // 여주 처리
        {
            // 여자 이미지 처리
            spGirl = currentCutscene.lines[idxDialog].overrideCharacterSprite;  // 덮어씌울 이미지
            Debug.Log($"여자 대화, 여자 대체 이미지 있니?  {spGirl != null}");
            // 이미지 살리기, 솔로 컷신 하다가 비활성화됐을수도 있으니
            if (imageGirl.gameObject.activeSelf == false)
                imageGirl.gameObject.SetActive(true);
            // 대체 이미지 있으면 삽입
            if (spGirl != null)
                imageGirl.sprite = spGirl;
            else
                imageGirl.sprite = origGirl;
            // 이미지 밝게 빼기
            imageGirl.color = Color.white;

            // 남자 이미지 처리
            if (currentCutscene.lines[idxDialog].isSolo)
                imageBoy.gameObject.SetActive(false);
            else
            {
                if (imageBoy.gameObject.activeSelf == false)
                    imageBoy.gameObject.SetActive(true);
                imageBoy.color = Color.gray;
            }
        }
        else                       // 기타 처리
        {
            // 남자 이미지 처리
            spBoy = currentCutscene.lines[idxDialog].overrideCharacterSprite;  // 덮어씌울 이미지
            Debug.Log($"남자 대화, 남자 대체 이미지 있니?  {spBoy != null}");
            // 이미지 살리기, 솔로 컷신 하다가 비활성화됐을수도 있으니
            if (imageBoy.gameObject.activeSelf == false)
                imageBoy.gameObject.SetActive(true);
            // 대체 이미지 있으면 삽입
            if (spBoy != null)
                imageBoy.sprite = spBoy;
            else
                imageBoy.sprite = origBoy;
            // 이미지 밝게 빼기
            imageBoy.color = Color.white;

            // 여자 이미지 처리
            if (currentCutscene.lines[idxDialog].isSolo)
                imageGirl.gameObject.SetActive(false);
            else
            {
                if (imageGirl.gameObject.activeSelf == false)
                    imageGirl.gameObject.SetActive(true);
                imageGirl.color = Color.gray;
            }
        }

        // 입력을 통한 인덱스 갱신        // TODO: 입력 매니저로 빼서 처리할지 팀원들과 상의하기
        if (Input.GetKeyDown(KeyCode.Space) && waitCount >= inputDelay)
        {
            SoundManager.manager.Click();
            idxDialog++;
            waitCount = 0f;
            Debug.Log($"현재 인덱스 : {idxDialog} //  대사 수 총량 : {currentCutscene.lines.Length}");
        }
        // 입력 대기 카운트 세팅
        if (waitCount < 5f) waitCount += Time.deltaTime;
    }



    public void OnClickSkip()
    {
        Debug.Log("!!스킵 버튼 누름!!");

        backplate.SetActive(true);

        switch (currentCutscene.cutsceneType)
        {
            case CutsceneType.Video:
                video.Stop();
                OnVideoEnd(video);
                break;
            case CutsceneType.Dialogue:
                DialogEnd();
                break;
        }
    }
}

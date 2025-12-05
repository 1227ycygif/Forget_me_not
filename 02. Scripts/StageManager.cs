//#define CBT_MODE
#define RELEASE_MODE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    //RPC 호출을 위한 PhotonView 연결 레퍼런스
    PhotonView pv;
    // 씬 내의 캠의 스크립트 재할당을 위한 객체 연결
    GameObject cam;

    // 스테이지 준비 완료 시 트랜지션 해제.....인데 쓸까 이거...?
    public bool isReady;

    //플레어의 생성 위치 저장 레퍼런스
    [SerializeField] private GameObject[] playerPos;
    // 퍼즐 배치 위치 저장 래퍼런스
    private GameObject[] puzzlePos;
    // 각 스테이지마다 스테이지 매니저를 생성, 그때마다 필요한 퍼즐 프리팹 할당
    public GameObject[] puzzles;
    public GameObject foxPos;

    private void Awake()
    {
        // 포톤 뷰 연결
        pv = GetComponent<PhotonView>();

        // 배치 포지션 찾아주세오
        FindGenPosition();
        // 캠 찾아주세요      >> 밑에서 찾고 있어, 필요없다는것임
        //StartCoroutine(MainCamFinder());

        // 포톤 오픈  >> 이거 조절은 씬 매니저에서 해주고 있어서 문제 없을걸.,...?
        // PhotonNetwork.isMessageQueueRunning = true;
    }

    // 밑에서 따로 진행중임
    IEnumerator MainCamFinder()
    {
        while (cam != null)
        {
            // 무한 실해으로 인한 과부하 막기
            yield return new WaitForSeconds(5f);
            // 카메라 찾기 시작
            cam = GameObject.FindGameObjectWithTag("MainCamera");

            if (cam != null)
                cam.AddComponent<CameraCtrl>();
        }
    }

    // 배치 포지션 찾아주세오
    private void FindGenPosition()
    {
        // 플레이어 생성 위치 
        playerPos = GameObject.FindGameObjectsWithTag("PlayerPos");

        // 퍼즐 배치 위치
        puzzlePos = GameObject.FindGameObjectsWithTag("PuzzlePos");

        // 여우 생성 위치
        foxPos = GameObject.FindGameObjectWithTag("FoxPos");
    }

    // 씬 이동 매니저에서 호출할 오브젝트 배치 함수
    public void GenerateObjects()
    {
        if (playerPos == null || puzzlePos == null) FindGenPosition();

        // 플레이어는 모두가 만들되 자신 타인을 구분지어야한다
        StartCoroutine(this.CreatePlayer());

        // 현재 씬이 DemoScene 일때만 여우 생성
        if (SceneManager.GetActiveScene().name == "DemoScene")
            StartCoroutine(this.FoxSpawn());
        StageBGM();
#if !CBT_MODE
        if (PhotonNetwork.connected && PhotonNetwork.isMasterClient)
        {
            // 퍼즐은 방장이 만든다
            if (SceneManager.GetActiveScene().name == "scClassroom")
                StartCoroutine(this.GeneratePuzzle());

            // 현재 씬이 DemoScene 일때만 여우 생성
            // if (SceneManager.GetActiveScene().name == "DemoScene")
            //     StartCoroutine(this.FoxSpawn());
        }
#endif
    }

    // 플레이어를 씬에 배치하는 코루틴 함수
    IEnumerator CreatePlayer()
    {
        //현재 입장한 룸 정보를 받아옴(레퍼런스 연결)
        Room currRoom = PhotonNetwork.room;
        // 플레이어 생성 준비
        GameObject player;

        if (!PhotonNetwork.connectedAndReady || currRoom.PlayerCount != 2)
        {
            Debug.LogError("네트워크가 이상한데??");
#if !CBT_MODE
            yield break;
        }

        // 잠시 대기 
        yield return new WaitForSeconds(1f);

        // 마스터 클라이언트와 아닌 경우를 구분하여 생성
        // 1. 마스터(여주)
        if (PhotonNetwork.isMasterClient)
        {
            player = PhotonNetwork.Instantiate("PlayerF", playerPos[0].transform.position, playerPos[0].transform.rotation, 0, null);
        }
        // 2. 서브 (남주)
        else
        {
            player = PhotonNetwork.Instantiate("PlayerM", playerPos[1].transform.position, playerPos[1].transform.rotation, 0, null);

        }
#else
        }

        player = GameObject.Instantiate(Resources.Load<GameObject>("PlayerF"), playerPos[0].transform.position, Quaternion.identity);
        Debug.Log("플레이어 생성 구문 실행 끝");
#endif
        // 플레이어 태그 달기
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");

        // 내 플레이어일 때만 카메라 세팅
        PhotonView pv = player.GetComponent<PhotonView>();
        if (pv.isMine)
        {
            // 플레이어 캠 위치 찾기
            GameObject playerCamPos = player.transform.Find("camPos").gameObject;
            // 플레이어의 카메라 위치를 메인 캠 포지션으로
            playerCamPos.tag = "CamPosPlayer";

            yield return CamSettings(playerCamPos);

            Debug.Log("카메라 세팅 코루틴 호출 완료");
        }

        //player.GetComponent<Rigidbody>().isKinematic = false;
        //Debug.Log("키네마틱 비활성화");

        yield break;
    }

    IEnumerator CamSettings(GameObject playerCamPos)
    {
        while (cam == null)
        {
            //Debug.Log("못찾았어요");
            cam = GameObject.FindGameObjectWithTag("MainCamera");
            yield return new WaitForSeconds(0.8f);
        }

        CameraCtrl controller = cam.GetComponent<CameraCtrl>();
        if (controller == null)
            controller = cam.AddComponent<CameraCtrl>();
        controller.enabled = true;
        controller.SetInitPos(playerCamPos.transform);

        // 카메라 추적 위치 설정
        //controller.SetPlayerPos(playerCamPos.transform);
        // 카메라 추적 속도 설정
        //controller.SetCamSpeed(4f);

        yield break;
    }

    // 필요한 퍼즐 오브젝트를 씬에 배치하는 코루틴 함수
    IEnumerator GeneratePuzzle()
    {
        if (puzzles == null) Debug.LogError("퍼즐 없음.");

        // 동작 꼬임 방지용 대기시간
        yield return new WaitForSeconds(1.0f);

        List<string> names = new List<string>();

        for (int i = 0; i < puzzles.Length; i++)
        {
            names.Add(puzzles[i].name);
        }

        if (PhotonNetwork.room == null)
        {
            Debug.LogWarning("룸 정보가 없어 잠시 대기합니다 (1s)");
            yield return new WaitForSeconds(1.0f); ;
        }

        // 모든 퍼즐 모든 포지션에 배치
        ////////////// 이 구조, 사실상 인스펙터에 등록한 프리팹 이름만 갖져다 쓰는 꼴,  더 좋은 방법 없나...?
        for (int i = 0; i < names.Count; i++)
        {
            // 플레이어만 생성, 씬 귀속  >> 게임 내 단 하나의 퍼즐로서 작용
            PhotonNetwork.InstantiateSceneObject(names[i], puzzlePos[i].transform.position, puzzlePos[i].transform.rotation, 0, null);
            Debug.Log($"{names[i]} Loaded");
        }

        yield break;
    }

    // 여우는 방장만, 씬에 생성하기
    IEnumerator FoxSpawn()
    {
        if (PhotonNetwork.isMasterClient)
        {
            GameObject fox = PhotonNetwork.InstantiateSceneObject("Fox", foxPos.transform.position, foxPos.transform.rotation, 0, null);
            Debug.Log("여우 생성 완료");
        }
        yield break;
    }

    IEnumerator Start()
    {
        if (playerPos == null || puzzlePos == null) FindGenPosition();

        GenerateObjects();

        yield break;
    }

    void StageBGM()
    {
        
        if (SceneManager.GetActiveScene().name == "scClassroom") 
        { 
            SoundManager.manager.BGMPlay(0, this.transform.position);
        }
        if (SceneManager.GetActiveScene().name == "Stage2Scene2222")
        {
            SoundManager.manager.BGMPlay(1, this.transform.position);
        }
        if (SceneManager.GetActiveScene().name == "DemoScene")
        {
            SoundManager.manager.BGMPlay(2, this.transform.position);
        }


    }
}

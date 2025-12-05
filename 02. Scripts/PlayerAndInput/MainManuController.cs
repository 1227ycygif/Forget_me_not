using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]

public class MainManuController : MonoBehaviour
{
    [Header("버튼오브젝트들")]
    [SerializeField] Button btnSound;
    [SerializeField] Button btnInven;
    [SerializeField] Button btnSave;
    [SerializeField] Button btnQuit;

    [Header("대응하는 활성 오브젝트들")]
    [SerializeField] GameObject menuSound;
    [SerializeField] GameObject menuInven;
    [SerializeField] GameObject menuSave;       // 11.21 현재 미구현
    [SerializeField] GameObject menuQuit;
    [SerializeField] GameObject menuMainMenu;

    PhotonView pv;

    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
    }

    // 활성화될 때 오브젝트 서치, 한번만
    private void OnEnable()
    {
        // 씬에 있는 메인 캔버스 속 사운드 패널을 참조한다
        
        // 메인메뉴 씬에 있는 기능 버턴들 찾아서 할당해준다
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickQuitYes()
    {
        if (pv.isMine)
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE_WIN
            Application.Quit();
#endif
        else
            pv.RPC(nameof(QuitGamePopup), PhotonTargets.Others, null);
    }
    
    public void OnClickQuitNo()
    {
        menuQuit.SetActive(false);
        menuMainMenu.SetActive(false);
    }

    [PunRPC]
    void QuitGamePopup()
    {
        menuQuit.SetActive(true);

        for (int i = 0; i < menuQuit.transform.childCount; i++)
        {
            // 맨 마지막 제외 전부 
            menuQuit.transform.GetChild(i).gameObject.SetActive((i == menuQuit.transform.childCount-1) ? true : false);
        }
        StartCoroutine(QuitGame());
    }

    IEnumerator QuitGame()
    {
        Debug.LogWarning("게임 탈출 코루틴 시작, 3초뒤 로비로 돌아갑니다.");
        // 근데, 이거 아마 망가질거야, 씬만 옮기는게 아니라 방을 나가는 처리를 해줘야하는데, 안해줬잖아?
        yield return new WaitForSeconds(3f);

        SceneMoveManager.Instance.LoadScene("scLobby");

        yield break;
    }
}

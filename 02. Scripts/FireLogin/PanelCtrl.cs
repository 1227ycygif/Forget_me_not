using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelCtrl : MonoBehaviour
{
    // 로그인 창과 회원가입 창 전환용
    public GameObject panelLogin;
    public GameObject panelRegister;

    // Start is called before the first frame update
    void Start()
    {
        ShowLoginPanel();
    }

    public void ShowLoginPanel()
    {
        panelLogin.SetActive(true);
        panelRegister.SetActive(false);
    }
   
    public void ShowRegisterPanel()
    {
        panelLogin.SetActive(false);
        panelRegister.SetActive(true);
    }
}

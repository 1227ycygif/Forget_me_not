using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoutButton : MonoBehaviour
{
    public void OnLogout()
    {
        UserData.Instance.LogOut();
        Debug.Log("로그아웃 완료");
        SceneManager.LoadScene("scOpen");
    }
    
}

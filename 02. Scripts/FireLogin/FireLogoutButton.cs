using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class FireLogoutButton : MonoBehaviour
{
    public void OnLogout()
    {
        // Firebase 로그아웃 추가
        if (FirebaseAuth.DefaultInstance != null)
        {
            FirebaseAuth.DefaultInstance.SignOut();
            Debug.Log("Firebase 로그아웃 완료");
        }
        
        // UserData 초기화 (기존)
        if (UserData.Instance != null)
        {
            UserData.Instance.LogOut();
            Debug.Log("로그아웃 완료");
        }
        
        // Photon 연결 끊기 (추가)
        if (PhotonNetwork.connected)
        {
            PhotonNetwork.Disconnect();
            Debug.Log("Photon 연결 해제");
        }
        
        // 씬 이동 (기존)
        SceneManager.LoadScene("scLogin");
    }
}

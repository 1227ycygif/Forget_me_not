using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserData : MonoBehaviour
{
    public static UserData Instance 
    { get; private set; }

    public string userId
    {  get; private set; }

    public string username
    { get; private set; }

    public int clearedStage
    { get; private set; }

    public bool isLoggedIn
    { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetUserInfo(string id, string name, int stage)
    {
        userId = id;
        username = name;
        clearedStage = stage;
        isLoggedIn = true;

        Debug.Log($"사용자 정보 저장 : {userId} / {username} / 클리어 스테이지 : {clearedStage}");
    }

    public void UpdateClearedStage(int stage)
    {
        clearedStage = stage;
        Debug.Log($"클리어 스테이지 업데이트 : {clearedStage}");
    }

    public void LogOut()
    {
        userId = null;
        username = null;
        clearedStage = 0;
        isLoggedIn = false;

        Debug.Log("로그아웃");
    }

    public void PrintUserInfo()
    {
        if (isLoggedIn)
        {
            Debug.Log($"현재 로그인 : {userId} / {username} / 클리어 : {clearedStage}");
        }
        else
        {
            Debug.Log("로그인 안 됨");
        }
    }
    
}

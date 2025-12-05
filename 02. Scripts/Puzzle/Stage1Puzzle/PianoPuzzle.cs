using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoPuzzle : MonoBehaviour
{
    [Header("건반들 넣기")]
    [SerializeField] private Piano[] piano;

    List<int> answer = new List<int>();
    [Header("정답 입력")]
    [SerializeField] List<int> check = new List<int> {};

    public bool isSolved = false;
    void Awake()
    {
        for (int i = 0; i < piano.Length; i++)
        {
            piano = GetComponentsInChildren<Piano>();
            //Debug.Log(piano[i].name);
        }
    }

    void Start()
    {
    }

    void Update()
    {
        if (answer.Count == check.Count)
        {
            Debug.Log("정답!");
            isSolved = true;
            ClassPuzzleManager.Instance.AddSolved();
            answer.Clear();
            return;
        }
        else
        {
            for (int i = 0; i < piano.Length; i++)
            {
                if (!piano[i].isPushed) continue;

                //Debug.Log(i + " 번째 키 눌림");
                answer.Add(i);
                //Debug.Log("입력 리스트 개수: " + answer.Count);

                piano[i].isPushed = false;

                if(XX())
                {
                    Debug.Log("틀림");
                    answer.Clear();
                    return;
                }
            }
        }
    }

    bool XX()
    {
        return answer[answer.Count-1] != check[answer.Count-1];
    }
    
    // void AnswerCheck()
    // {
    //     Debug.Log("입력 리스트 : " + answer[0] + " " + answer[1] + " " + answer[2] + " " + answer[3] + " " + answer[4] + " " + answer[5] + " " + answer[6] + " " + answer[7]);
    //     Debug.Log("정답 리스트 : " + check[0] + " " + check[1] + " " + check[2] + " " + check[3] + " " + check[4] + " " + check[5] + " " + check[6] + " " + check[7]);

    //     bool answerCheck = true;

    //     for (int i = 0; i < answer.Count; i++)
    //     {
    //         if (answer[i] != check[i])
    //         {
    //             answerCheck = false;
    //             break;
    //         }
    //     }

    //     if (answerCheck)
    //     {
    //         Debug.Log("정답");
    //     }
    //     else
    //     {
    //         Debug.Log("오답");
    //     }
        
    //     answer.Clear();
    // }
}

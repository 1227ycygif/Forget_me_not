// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class StunTalisman : MonoBehaviour
// {
//     public string targetTag = "Fox";

//     Rigidbody rigid;
//     PhotonView pv;
//     bool launched = false;

//     void Awake()
//     {
//         rigid = GetComponent<Rigidbody>();
//         pv = GetComponent<PhotonView>();
//         if (rigid) rigid.isKinematic = true; // 처음엔 손에 들고 있으니까
//     }

//     // 손에 붙이는 단계 (SO에서 호출)
//     public void AttachTo(Transform parent)
//     {
//         transform.SetParent(parent, worldPositionStays: false);
//         transform.localPosition = Vector3.zero;
//         transform.localRotation = Quaternion.identity;
//     }

//     // 좌클릭에서 호출: 실제 투척 시작
//     public void Launch(Vector3 velocity)
//     {
//         if (launched) return;
//         launched = true;

//         transform.SetParent(null, true);

//         if (!rigid) rigid = GetComponent<Rigidbody>();
//         rigid.isKinematic = false;
//         rigid.velocity = Vector3.zero;
//         rigid.angularVelocity = Vector3.zero;
//         rigid.AddForce(velocity, ForceMode.VelocityChange);
//     }

//     void OnCollisionEnter(Collision collision)
//     {
//         if (!launched) return;
//         if (!pv || !pv.isMine) return;

//         if (collision.collider.CompareTag(targetTag))
//         {
//             var foxPv = collision.collider.GetComponentInParent<PhotonView>();
//             if (foxPv != null)
//             {
//                 foxPv.RPC("Stun", PhotonTargets.All);
//             }
//         }

//         PhotonNetwork.Destroy(gameObject);
//     }
// }

// using UnityEngine;

// public class StunTalisman : MonoBehaviour
// {
//     public string targetTag = "Fox";
//     Rigidbody rigid;
//     bool launched = false;

//     void Awake()
//     {
//         rigid = GetComponent<Rigidbody>();
//         if (rigid) rigid.isKinematic = true;
//     }

//     public void AttachTo(Transform parent)
//     {
//         transform.SetParent(parent, false);
//         transform.localPosition = Vector3.zero;
//         transform.localRotation = Quaternion.identity;
//     }

//     public void Launch(Vector3 velocity)
//     {
//         if (launched) return;
//         launched = true;

//         transform.SetParent(null, true);
//         if (rigid == null) rigid = GetComponent<Rigidbody>();
//         if (rigid)
//         {
//             rigid.isKinematic = false;  // 물리 활성화
//             rigid.velocity = Vector3.zero;
//             rigid.angularVelocity = Vector3.zero;
//             rigid.AddForce(velocity, ForceMode.VelocityChange);

//             Debug.Log($"부적 발사! 속도: {rigid.velocity.magnitude:F1}");
//         }

//     }

//     void OnCollisionEnter(Collision col)  // Collision 매개변수!
// {
//     Debug.Log(" 부적 물리 충돌 감지! " + col.gameObject.name);

//     if (!launched) return;

//     Debug.Log("충돌 후 스턴 처리 시도");

//     // col.collider 사용 (col.gameObject도 OK)
//     if (col.collider.CompareTag(targetTag))
//     {
//         Debug.Log("FOX 물리 충돌! 스턴 발동");
//         var foxCtrl = col.collider.GetComponent<FoxCtrl>();
//         if (foxCtrl != null)
//         {
//             Debug.Log("FoxCtrl.Stun() 호출!");
//             foxCtrl.Stun();
//         }
//         else
//         {
//             Debug.LogWarning("FoxCtrl 컴포넌트 없음!");
//         }
//     }

//     Destroy(gameObject, 0.1f); // 충돌 후 약간 딜레이 후 파괴
// }
// }

using UnityEngine;

public class StunTalisman : Photon.MonoBehaviour
{
    public string targetTag = "Fox";
    Rigidbody rigid;
    bool launched = false;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        if (rigid) rigid.isKinematic = true;
    }

    public void AttachTo(Transform parent)
    {
        transform.SetParent(parent, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Launch(Vector3 velocity)
    {
        if (launched || !photonView.isMine) return;
        launched = true;

        transform.SetParent(null, true);
        if (rigid)
        {
            rigid.isKinematic = false;
            rigid.velocity = velocity;
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (!launched || !photonView.isMine) return;

        if (col.collider.CompareTag(targetTag))
        {
            Debug.Log("FOX 물리 충돌! 스턴 발동");
            var foxCtrl = col.collider.GetComponent<FoxCtrl>();
            var foxPv = col.collider.GetComponentInParent<PhotonView>();
            if (foxCtrl != null)
            {
                Debug.Log("FoxCtrl.Stun() 호출!");
                //foxCtrl.RPC_Stun();
                foxPv.RPC("RPC_Stun", PhotonTargets.AllBuffered);
            }
            else
            {
                Debug.LogWarning("FoxCtrl 컴포넌트 없음!");
            }
        }
        PhotonNetwork.Destroy(gameObject);
    }
}
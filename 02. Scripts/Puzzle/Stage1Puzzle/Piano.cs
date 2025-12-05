using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class Piano : MonoBehaviour
{
    public AudioSource audioSource;

    public bool isPushed = false;

    [Header("피아노 건반 음원")]
    public AudioClip audioClip;

    void Start()
    {
        audioSource.clip = audioClip;
    }

    void OnMouseDown()
    {
        Debug.Log("gg");
        audioSource.Play();

        isPushed = true;
    }
}

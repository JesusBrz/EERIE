using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

public class KnockOutEvent : MonoBehaviour
{

    Animator _anim;
    FirstPersonController _player;
    public PlayerInput playerInput;
    public InteractableObject _io;

    // Start is called before the first frame update
    void Start()
    {
        _anim = GetComponentInChildren<Animator>();
        _anim.enabled = false;

        _player = FindObjectOfType<FirstPersonController>();
        playerInput = _player.GetComponent<PlayerInput>();
        _io = FindObjectOfType<InteractableObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        playerInput.SwitchCurrentActionMap("Tutorial");
        _player.GetComponentInChildren<CapsuleCollider>().enabled = false;
        _player.GetComponentInChildren<Rigidbody>().useGravity = false;
        _anim.enabled = true;
        StartCoroutine(animKnockingout());
    }

    public IEnumerator animKnockingout()
    {
        _player.transform.parent = _anim.gameObject.transform;
        _player.transform.LookAt(_anim.gameObject.transform.GetChild(0).transform);
        while (!_io._finishedEvent)
        {
            yield return new WaitForEndOfFrame();          
        }
        playerInput.SwitchCurrentActionMap("Tutorial");
    }
}

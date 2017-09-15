﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : Photon.MonoBehaviour {

    #region Variables
    private Rigidbody _rigid;
    private Transform _enemy;
    private float _originalWalkSpeed;
    private float _originalRunSpeed;

    [HideInInspector]
    public float speed;
    [HideInInspector]
    public float xMovement;
    [HideInInspector]
    public float yMovement;
    [HideInInspector]
    public bool isRunning;
    [HideInInspector]
    public bool sprintAvailable;
    [HideInInspector]
    public bool isRolling;
    
    public Transform Enemy { get { return _enemy; } }
    #endregion

    #region Initialization
    private void InitializeVariables()
    {
        _rigid = GetComponent<Rigidbody>();
        _enemy = GetEnemy();

        _originalWalkSpeed = 350f;
        _originalRunSpeed = _originalWalkSpeed * 1.8f;
        speed = _originalWalkSpeed;

        xMovement = 0f;
        yMovement = 0f;
        isRunning = false;
        sprintAvailable = true;
        isRolling = false;
    }

    private void AddEvents()
    {
        EventManager.AddEventListener("Attack", OnAttack);
        EventManager.AddEventListener("CharacterDamaged", OnCharacterDamaged);
        EventManager.AddEventListener("RollExit", OnRollExit);
    }
    #endregion

    void Start()
    {
        InitializeVariables();
        AddEvents();
    }

    #region Actions
    /// <summary>Moves the character in a given direction</summary>
    public void Move(Vector3 direction)
    {
        if (isRunning && direction.z <= 0)
        {
            speed = _originalWalkSpeed;
            isRunning = false;
        }

        if (direction != Vector3.zero)
            _rigid.velocity = new Vector3(transform.TransformDirection(direction).x * speed * Time.deltaTime, _rigid.velocity.y, transform.TransformDirection(direction).z * speed * Time.deltaTime);

        //Updates variables for the animator
        SetMovementStats(direction);
    }

    /// <summary>Sets the rotation of the character using the camera</summary>
    public void Rotate(Vector3 direction, Transform cam)
    {
        if(direction != Vector3.zero && !isRolling) transform.forward = new Vector3(cam.forward.x, 0, cam.forward.z);
    }

    /// <summary>Makes the character roll</summary>
    public void Roll(Vector3 direction)
    {
        isRolling = true;
        if (direction != Vector3.zero) transform.forward = new Vector3(transform.TransformDirection(direction).x, 0f, transform.TransformDirection(direction).z);
        EventManager.DispatchEvent("RollingAnimation", new object[] { this.gameObject.name, isRolling });
        if (!PhotonNetwork.offlineMode) photonView.RPC("SetRollingOn", PhotonTargets.All);
    }

    /// <summary>Makes the character run</summary>
    public void Run()
    {
        EventManager.DispatchEvent("ActivateRunParticle", new object[] { this.gameObject.name, true });
        isRunning = true;
        sprintAvailable = false;
        speed = _originalRunSpeed;
    }

    /// <summary>Makes the character walk</summary>
    public void StopRun()
    {
        EventManager.DispatchEvent("ActivateRunParticle", new object[] { this.gameObject.name, false });
        isRunning = false;
        sprintAvailable = true;
        speed = _originalWalkSpeed;
    }
    
    /// <summary>Sets variables used by the animator</summary>
    private void SetMovementStats(Vector3 direction)
    {
        xMovement = direction.x;
        yMovement = isRunning ? direction.z * 2 : direction.z;

        EventManager.DispatchEvent("RunningAnimations", new object[] { this.gameObject.name, xMovement, yMovement });
    }

    /// <summary>Stops animations on attack</summary>
    private void OnAttack(params object[] paramsContainer)
    {
        //Dejo esto comentado porque al parecer este evento ya no se usa, y no recuerdo para que era, asique por las dudas lo dejo aca.

        //runForward = false;
        //xMovement = 0f;
        //yMovement = 0f;
    }
    #endregion

    #region Animation Events
    /// <summary>This is what it stops the action of rolling when the animation ends.</summary>
    private void OnRollExit(params object[] paramsContainer)
    {
        if (this.gameObject.name == (string)paramsContainer[0])
            isRolling = false;
    }

    private void OnCharacterDamaged(params object[] paramsContainer)
    {
        if (this.gameObject.name == (string)paramsContainer[0])
            isRunning = false;
    }
    #endregion

    #region Enemy Reference
    public bool CheckEnemyDistance(Transform cam)
    {
        if (_enemy == null)  _enemy = GetEnemy();

        if (_enemy != null)
        {
            var dir = (_enemy.position - cam.position).normalized;
            var ang = Vector3.Angle(cam.forward, dir);

            return (Vector3.Distance(this.transform.position, _enemy.position) > 10f && ang <= 20) ? true : false;
        }

        return false;
    }

    private Transform GetEnemy()
    {
        var enems = GameObject.FindObjectsOfType<NetworkCharacter>();

        foreach (var enem in enems)
        {
            if (enem.transform != this.transform)
                return enem.transform;
        }
        
        return null;
    }
    #endregion

    #region Getters
    public Transform EnemyTransform
    {
        get { return _enemy == null ? GetEnemy() : _enemy; }
    }
    #endregion
}

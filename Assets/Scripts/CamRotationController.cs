﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CamRotationController : MonoBehaviour
{
    #region Variables
    public float minimumY = -45f;
    public float maximumY = 15f;
    public float rotateSpeed = 0.9f;
    public float smoothPercentage = 0.6f;
    public float lockOnDistance;
    public float destructibleDistance = 15f;
    public float sensivity;
    public bool showProjections;
    public bool smoothCamera = true;
    public Vector3 initialPosition;

    [System.Obsolete("Ya no se usa más")]
    public Color destructibleColor;

    int _proyectionLayer;
    int _lockOnLayer;

    private Transform _character;
    private Transform _enemy;
    private float _rotationY;
    private float _rotationX;
    private Vector3 _fixedCharPos;
    private Vector3 _correctionVector;
    private Vector3 _direction;
    private float _frontDistance;
    private float _backDistance;
    private float _angleVision = 30f;
    private float _instHorizontal;
    private float _instVertical;
    private bool _gameInCourse = true;
    private bool _lockOn = false;
    private bool _readJoystick;
    private LayerMask _mask;
    private LayerMask _enemyMask;
    private Camera _cam;
    private RaycastHit _hit;
    private DestructibleObject _currentTarget;

    [System.Obsolete("Ya no se usa más")]
    private List<MarkableObject> _allMarkables;
    [System.Obsolete("Ya no se usa más")]
    private Color _originalColor = Color.white;


    Vector2 _offset;
    Quaternion _fixedLocalRot;
    CameraShake _shake;
    bool _isInTransition;

    public DestructibleObject CurrentTarget
    {
        get { return _currentTarget; }
    }

    public Quaternion FixedRotation
    {
        get { return _fixedLocalRot; }
        set { _fixedLocalRot = value; }
    }

    public Transform Enemy
    {
        get { return _enemy; }
    }

    public Camera GetCamera
    {
        get { return _cam; }
    }

    public float AngleVision
    {
        get { return _angleVision; }
    }
    #endregion

    void Start()
    {
        GetComponents();
        AddEvents();
        gameObject.AddComponent(typeof(CameraShake));
        _shake = GetComponent<CameraShake>();
    }

    #region Initialization
    private void GetComponents()
    {
        //Ivan: para que haces esto si cada camara tiene solo un hijo de tipo camara y ninguno se llama asi??
        _cam = GetComponentInChildren<Camera>();
        _mask = ~(1 << LayerMask.NameToLayer("Player")
                | 1 << LayerMask.NameToLayer("Enemy")
                | 1 << LayerMask.NameToLayer("Floor")
                | 1 << LayerMask.NameToLayer("HitBox")
                | 1 << LayerMask.NameToLayer("PlayerCollider")
                | 1 << Utilities.IntLayers.VISIBLETOP1
                | 1 << Utilities.IntLayers.VISIBLETOP2
                | 1 << Utilities.IntLayers.VISIBLETOBOTH
                 );
        _enemyMask = PhotonNetwork.offlineMode ? 0 << LayerMask.NameToLayer("Enemy") : 0 << LayerMask.NameToLayer("Player");
        _correctionVector = new Vector3(0f, 1f, 0f);
        lockOnDistance = lockOnDistance == 0f ? 10f : lockOnDistance;

        _enemy = GetEnemy();
    }

    public void Init(Transform charac, bool readJoystick)
    {
        _character = charac;
        _readJoystick = readJoystick;
        if (_readJoystick) sensivity *= 4;
        else sensivity = 0.1f;
        transform.position = _character.position;
        transform.rotation = _character.rotation;
        if (_cam == null) _cam = GetComponentInChildren<Camera>();
        _cam.transform.localPosition = transform.InverseTransformPoint(initialPosition);

        _enemy = GetEnemy();
    }

    public void Init(Transform charac, bool readJoystick, int cullLayer)
    {
        _character = charac;
        _readJoystick = readJoystick;
        if (_readJoystick) sensivity *= 4;
        else sensivity = 0.1f;
        transform.position = _character.position;
        transform.rotation = _character.rotation;
        if (_cam == null) _cam = GetComponentInChildren<Camera>();
        _cam.transform.localPosition = transform.InverseTransformPoint(initialPosition);
        _proyectionLayer = cullLayer;
        _lockOnLayer = _proyectionLayer == 16 ? 20 : 21;

        _enemy = GetEnemy();
        showProjections = true;
        //HighlightTarget();
    }

    private void AddEvents()
    {
        EventManager.AddEventListener("ChangeStateDestuctibleProjections", ActivateProjections);
        EventManager.AddEventListener("DoConnect", UseProjections);
        EventManager.AddEventListener("DoNotConnect", UseProjections);
        EventManager.AddEventListener("DoDummyTest", UseProjections);
        EventManager.AddEventListener("DividedScreen", UseProjections);
        //EventManager.AddEventListener("BeginGame", UseProjections);
        EventManager.AddEventListener("GameFinished", OnGameFinished);
        EventManager.AddEventListener("RestartRound", OnRestartRound);
        EventManager.AddEventListener("TransitionSmoothCameraUpdate", OnTransitionSmoothUpdate);
    }

    void OnTransitionSmoothUpdate(object[] paramsContainer)
    {
        smoothCamera = (bool)paramsContainer[0];
    }

    private Transform GetEnemy()
    {
        if (GameManager.screenDivided || !PhotonNetwork.offlineMode)
        {
            var enems = GameObject.FindObjectsOfType<Player1Input>();

            foreach (var enem in enems)
            {
                if (enem.transform != _character)
                    return enem.transform;
            }
        }
        else
        {
            var enems = GameObject.FindObjectsOfType<CharacterMovement>();

            foreach (var enem in enems)
            {
                if (enem.transform != _character)
                    return enem.transform;
            }
        }

        return null;
    }
    #endregion

    #region Events
    void UseProjections(object[] paramsContainer)
    {
        showProjections = (bool)paramsContainer[0];
    }

    void ActivateProjections(object[] paramsContainer)
    {
        showProjections = (bool)paramsContainer[0];
    }

    private void OnGameFinished(params object[] paramsContainer)
    {
        _gameInCourse = false;
    }

    private void OnRestartRound(params object[] paramsContainer)
    {
        _gameInCourse = true;

        if ((bool)paramsContainer[0])
        {
            EventManager.RemoveEventListener("ChangeStateDestuctibleProjections", ActivateProjections);
            EventManager.RemoveEventListener("DoConnect", UseProjections);
            EventManager.RemoveEventListener("DoNotConnect", UseProjections);
            EventManager.RemoveEventListener("DoDummyTest", UseProjections);
            EventManager.RemoveEventListener("DividedScreen", UseProjections);
            //EventManager.RemoveEventListener("BeginGame", UseProjections);
            EventManager.RemoveEventListener("GameFinished", OnGameFinished);
            EventManager.RemoveEventListener("RestartRound", OnRestartRound);
            EventManager.RemoveEventListener("TransitionSmoothCameraUpdate", OnTransitionSmoothUpdate);
        }
    }
    #endregion

    void Update()
    {
        if (showProjections) HighlightTarget();
    }

    void FixedUpdate()
    {
        if (_gameInCourse) MoveCamera();
    }

    void LateUpdate()
    {
        if (_gameInCourse)
        {
            ClippingBehaviour();

            if (!_readJoystick && InputManager.instance.GetLockOn()) CamLock();
            else if (_readJoystick && InputManager.instance.GetJoystickLockOn()) CamLock();
        }
    }

    #region Movement
    void MoveCamera()
    {
        if (_character != null)
        {
            if (transform.position != _character.position)
            {
                if (smoothCamera)
                    transform.position = Vector3.Lerp(transform.position, _character.position, smoothPercentage);
                else
                    transform.position = _character.position;
            }

            if (!_lockOn)
            {
                if (_readJoystick)
                {
                    _instHorizontal = InputManager.instance.GetJoystickHorizontalCamera();
                    _instVertical = InputManager.instance.GetJoystickVerticalCamera();

                    if (_instHorizontal != 0 || _instVertical != 0)
                    {
                        _rotationY += _instVertical * rotateSpeed * sensivity;
                        _rotationY = Mathf.Clamp(_rotationY, minimumY, maximumY);
                        _rotationX = _instHorizontal * rotateSpeed * sensivity + transform.eulerAngles.y;

                        transform.eulerAngles = new Vector3(_rotationY, _rotationX, 0);
                    }
                }
                else
                {
                    _instHorizontal = InputManager.instance.GetHorizontalCamera();
                    _instVertical = InputManager.instance.GetVerticalCamera();

                    if (_instHorizontal != 0 || _instVertical != 0)
                    {
                        _rotationY += -_instVertical * rotateSpeed * sensivity;
                        _rotationY = Mathf.Clamp(_rotationY, minimumY, maximumY);
                        _rotationX = _instHorizontal * rotateSpeed * sensivity + transform.eulerAngles.y;

                        transform.eulerAngles = new Vector3(_rotationY, _rotationX, 0);
                    }
                }
            }
            else LockOn();
        }
    }
    #endregion

    #region Lock
    private void CamLock()
    {
        var dir = (Enemy.position - _character.position).normalized;
        var dir2 = (Enemy.position - transform.TransformPoint(_cam.transform.localPosition)).normalized;
        var dist = lockOnDistance - Vector3.Distance(transform.TransformPoint(_cam.transform.localPosition), _character.position);
        _fixedCharPos = _character.position + _correctionVector;

        var checkVision = Physics.Raycast(_fixedCharPos, dir, dist, _enemyMask) && Physics.Raycast(transform.TransformPoint(_cam.transform.localPosition), dir2, lockOnDistance, _enemyMask);

        if (!_lockOn && Vector3.Distance(_character.position, Enemy.position) <= lockOnDistance && !checkVision)
        {
            _lockOn = true;
            EventManager.DispatchEvent("LockOnActivated", new object[] { GetCamera, _character.gameObject.name, _lockOn, _lockOnLayer });
        }
        else if (_lockOn)
        {
            _lockOn = false;
            EventManager.DispatchEvent("LockOnActivated", new object[] { GetCamera, _character.gameObject.name, _lockOn, _lockOnLayer });
        }
    }

    private void CheckDistance()
    {
        if (Vector3.Distance(_character.position, Enemy.position) > lockOnDistance)
        {
            _lockOn = false;
            EventManager.DispatchEvent("LockOnActivated", new object[] { GetCamera, _character.gameObject.name, _lockOn, _lockOnLayer });
        }
    }

    private void LockOn()
    {
        _fixedCharPos = Enemy.position + _correctionVector;
        var direction = (_fixedCharPos - transform.position).normalized;

        direction = new Vector3(direction.x, 0f, direction.z);

        if (transform.forward != direction)
            transform.forward = Vector3.Lerp(transform.forward, direction, smoothPercentage);

        CheckDistance();
    }
    #endregion

    #region Clipping
    private void ClippingBehaviour()
    {
        if (_character != null)
        {
            _fixedCharPos = _character.position + _correctionVector;
            _direction = (_fixedCharPos - transform.TransformPoint(_cam.transform.localPosition)).normalized;
            _frontDistance = Vector3.Distance(transform.TransformPoint(_cam.transform.localPosition), _fixedCharPos);
            _backDistance = Vector3.Distance(transform.TransformPoint(initialPosition), _fixedCharPos);

            if (Physics.Raycast(transform.TransformPoint(_cam.transform.localPosition), _direction, out _hit, _frontDistance, _mask.value))
            {
                _cam.transform.localPosition = Vector3.Lerp(_cam.transform.localPosition, transform.InverseTransformPoint(_hit.point + _direction * 0.1f), smoothPercentage);
            }
            else if (_cam.transform.localPosition != initialPosition && Physics.Raycast(_fixedCharPos, -_direction, out _hit, _backDistance, _mask.value))
            {
                _cam.transform.localPosition = Vector3.Lerp(_cam.transform.localPosition, transform.InverseTransformPoint(_hit.point + _direction * 0.1f), smoothPercentage);
            }
            else
            {
                _cam.transform.localPosition = Vector3.Lerp(_cam.transform.localPosition, initialPosition, smoothPercentage);
            }
        }
    }

    #region Old LockOn
    /*
    private void ReLocateCamera()
    {
        if (_lockOn)
        {
            _fixedCharPos = Enemy.position + _correctionVector;
            var direction = (_fixedCharPos - transform.position).normalized;
            
            direction = new Vector3(direction.x, 0f, direction.z);

            if (transform.forward != direction)
                transform.forward = Vector3.Lerp(transform.forward, direction, smoothPercentage);

            var direction2 = (_fixedCharPos - transform.TransformPoint(_cam.transform.localPosition)).normalized;

            if (_cam.transform.forward != direction2)
                _cam.transform.forward = Vector3.Lerp(_cam.transform.forward, direction2, smoothPercentage);
            
            
            CheckDistance();
        }
        else if(_keepReadjusting)
        {
            if (transform.forward != _character.forward)
                transform.forward = Vector3.Lerp(transform.forward, _character.forward, smoothPercentage);

            if (_cam.transform.forward != _character.forward)
                _cam.transform.forward = Vector3.Lerp(_cam.transform.forward, _character.forward, smoothPercentage);

            if (transform.forward == _character.forward && _cam.transform.forward == _character.forward)
                _keepReadjusting = false;
        }
    }*/
    #endregion
    #endregion

    #region Highlight
    private void HighlightTarget()
    {
        //Agrego que estén en la zona, mi cabe zona
        var dstruc = DestructibleObject.allObjs;
        List<DestructibleObject> inRangeObj = dstruc.Where(x => x.isAlive
                                                             && x != null
                                                             && x.zone == TransitionManager.instance.currentZone
                                                             && Vector3.Distance(x.transform.position, transform.position) <= destructibleDistance
                                                             && x.destructibleType != DestructibleType.TRANSITION
                                                             && x.GetComponentInChildren<Renderer>().isVisible)
                                                    .ToList<DestructibleObject>();
        DestructibleObject closest;

        if (inRangeObj.Any())
        {
            closest = inRangeObj[0];
            float angle = Vector3.Angle(transform.forward, (closest.transform.position - transform.position).normalized);
            float tempAngle;

            foreach (var dest in inRangeObj)
            {
                tempAngle = Vector3.Angle(transform.forward, (dest.transform.position - transform.position).normalized);

                if (tempAngle < angle)
                {
                    closest = dest;
                    angle = tempAngle;
                }
            }

            if (_currentTarget == null)
            {
                _currentTarget = closest;
                MakeVisible(_currentTarget, true);
            }
            else if (closest != _currentTarget)
            {
                MakeVisible(_currentTarget, false);
                _currentTarget = closest;
                MakeVisible(_currentTarget, true);
            }
        }
        else
        {
            if (_currentTarget != null)
            {
                MakeVisible(_currentTarget, false);
            }
            _currentTarget = null;
        }

    }

    void MakeVisible(DestructibleObject obj, bool visible)
    {
        var wf = obj.GetComponentsInChildren<DestructibleImpactArea>().Where(x => x.gameObject.layer == _proyectionLayer).FirstOrDefault();
        if (wf != default(DestructibleImpactArea))
        {
            wf.SetVisible(visible);
        }

    }

    [System.Obsolete("Ya no se usa más")]
    private void ChangeColor(DestructibleObject obj, Color col)
    {
        var renders = obj.GetComponentsInChildren<Renderer>();

        foreach (var rend in renders)
        {
            var mat = rend.material;
            mat.color = col;
            rend.material = mat;
        }
    }
    #endregion

    #region Shake
    public void ShakeCamera(float amount, float duration)
    {
        _shake.ShakeCamera(amount, duration);
    }
    #endregion

    #region Transition
    /// <summary>
    /// 0 - Is Start?
    /// </summary>
    /// <param name="paramsContainer"></param>
    void OnTransitionUpdate(object[] paramsContainer)
    {
        if (_proyectionLayer == Utilities.IntLayers.VISIBLETOP1)
        {
            _offset = new Vector2(1, 0);
        }
        else
        {
            _offset = new Vector2(-1, 0);
        }
        _isInTransition = (bool)paramsContainer[0];
    }

    /// Camera's offset in screen coordinates (animate this using your favourite method). 
    /// Zero means no effect. Axes may be swapped from what you expect. 
    /// Experiment with values between -1 and 1. public Vector2 offset;

    void OnPreRender()
    {
        if (_isInTransition)
        {
            var r = new Rect(0f, 0f, 1f, 1f);
            var alignFactor = Vector2.one;

            if (_offset.y >= 0f)
            {
                // Sliding down
                r.height = 1f - _offset.y;
                alignFactor.y = 1f;
            }
            else
            {
                // Sliding up
                r.y = -_offset.y;
                r.height = 1f + _offset.y;
                alignFactor.y = -1f;
            }

            if (_offset.x >= 0f)
            {
                // Sliding right
                r.width = 1f - _offset.x;
                alignFactor.x = 1f;
            }
            else
            {
                // Sliding left
                r.x = -_offset.x;
                r.width = 1f + _offset.x;
                alignFactor.x = -1f;
            }

            // Avoid division by zero
            if (r.width == 0f)
            {
                r.width = 0.001f;
            }
            if (r.height == 0f)
            {
                r.height = 0.001f;
            }

            // Set the camera's render rectangle to r, but use the normal projection matrix
            // This works around Unity modifying the projection matrix to correct for the aspect ratio
            // (which is normally desirable behaviour, but interferes with this effect)
            GetCamera.rect = new Rect(0, 0, 1, 1);
            GetCamera.ResetProjectionMatrix();
            var m = GetCamera.projectionMatrix;
            GetCamera.rect = r;

            // The above has caused the scene render to be squashed into the rectangle r.
            // Apply a scale factor to un-squash it.
            // The translation factor aligns the top of the scene to the top of the view
            // (without this, the view is of the middle of the scene)
            var m2 = Matrix4x4.TRS(
                new Vector3(alignFactor.x * (-1 / r.width + 1), alignFactor.y * (-1 / r.height + 1), 0),
                Quaternion.identity,
                new Vector3(1 / r.width, 1 / r.height, 1));

            GetCamera.projectionMatrix = m2 * m;
        }
    }


    #endregion

    IEnumerator LerpRectPosition(Rect objToMove, Vector2 startPos, Vector2 endPos, float maxTime)
    {
        var i = 0f;

        while (i <= 1)
        {
            i += Time.deltaTime / maxTime;
            objToMove.position = Vector2.Lerp(startPos, endPos, i);
            yield return new WaitForEndOfFrame();
        }
    }

}

[System.Obsolete("Ya no se usa más")]
public class MarkableObject
{
    public float distance;
    public float angle;
    public DestructibleObject target;

    public MarkableObject(float dist, float ang, DestructibleObject targ)
    {
        distance = dist;
        angle = ang;
        target = targ;
    }
}

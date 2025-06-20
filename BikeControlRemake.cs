using UnityEngine;
using System.Collections;
using System;
using TMPro;
using Unity.Cinemachine;
//using Cinemachine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
//using System.Numerics;

public enum GearState { Neutral, Running, CheckingChange, Changing };
public enum GearBoxType { Manual, Automatic };
public enum CameraMode { FirstPerson, DashCamera, FrontForkSuspension, ThirdPerson };
public enum HeadlightMode { OFF, LeftOnly, RightOnly, FULL }

public class BikeControlRemake : MonoBehaviour
{
    //public SceneStreamer sceneStreamer;
    public float SeatShiftAmount = 0.1f;
    [SerializeField] private GameObject SeatTargetObject;
    private Vector3 SeatTargetInitialPosition;
    [SerializeField] private float resetFadeDuration = 3f;
    [SerializeField] private Vector3 resetOffset = new Vector3(0, 1f, 0); // optional Y offset
    [SerializeField] private float resetCooldown = 3f;

    private float lastResetTime = -Mathf.Infinity;
    public bool isResetting = false;
    private bool isHIGHGForce;
    public Vector3 CurrentGForce;
    public float SteeringMultiplier = 1.0f;
    public float RiderHeadMovementVal = 0.06f;
    float OriginalY_Position;
    private Transform RiderHeadInitialTransform;
    PlayerControls controls;
    private int fixedFrameCounter = 0;
    public int fixedUpdateFrequency = 6;
    public float maxSlipFF = 0.0f;
    public float maxSlipFS = 0.0f;
    public float maxSlipBF = 0.0f;
    public float maxSlipBS = 0.0f;
    public float SpringDistanceValue;
    public float DamperValue;
    public bool validRotation = false;
    public bool CollidedBody = false;
    public bool isMidAir = false;
    public bool onlyFrontTouching = false;
    public float MaxWheelieDist = 0.8f;
    public float groundHeight;
    public GameObject RiderHead;
    public GameObject WaterInteractionObject;
    public GameObject BodyCollisionColliderObject;
    public GameObject WheelieColliderObject;
    SphereCollider wheelieSphereCollider;
    public bool BodyCollided = false;
    public bool frontWheelHit;
    public bool backWheelHit;
    //public bool EnableBouyancyScript;
    public bool TouchingWater;

    public UnityEvent TurnONHeadlights;
    public UnityEvent TurnOFFHeadlights;
    private bool TailLightON = true;
    private bool FrontLightON = false;

    public Material emissiveMaterial;
    public Renderer objectToChange;
    public CinemachineCamera ThirdPersonCinemachineCamera;
    public CinemachineOrbitalFollow orbitalFollow;
    public CinemachineBasicMultiChannelPerlin cameraShakeFPP;
    public CinemachineBasicMultiChannelPerlin cameraShakeTPP;
    float throttleVal;
    float keyboardThrottleVal;
    float ReverseVal;
    float brakeVal;
    float keyboardBrakeVal;
    float wheelieValInp;
    float wheelieVal;
    //private float currentFrontBrakeTorque;
    //private float currentRearBrakeTorque;
    public float leftStickXAxis;
    //public Vector2 rightStick;
    //public bool gearUp;
    //public bool gearDown;
    public CameraMode cameraMode;
    public float previousGearSpeed;
    public float currentGearSpeed;
    public float nextGearSpeed;
    public float susDiffDistance;
    public bool rotateBike;
    public bool MoveSuspension;
    public bool shiftCollider;
    public bool rotateWheel;
    public bool modifySteering;
    public GameObject ClusterSpeedoReadout;
    public Material FullscreenMotionBlurMaterial;
    public GameObject ThirdPersonCameraObject;
    public GameObject FirstPersonCameraObject;
    CinemachineBasicMultiChannelPerlin FPPCamNoiseObject;
    CinemachineBasicMultiChannelPerlin TPPCamNoiseObject;
    CinemachineCamera FPPCameraCinemachineCamera;

    public CinemachineCamera reverseCamera;  // Assigned in inspector
    public int normalPriority = 10;
    public int lowPriority = 0;
    public int overridePriority = 100;

    [System.Serializable]
    public class CameraSetup
    {
        public CameraMode mode;
        public CinemachineCamera vCam; // ✅ This works in Unity 6 / 2025 with Cinemachine 3.x
    }

    //public CameraMode cameraMode; // Current active mode
    public CameraSetup[] cameraSetups; // Assign in inspector
    //public float ThrottleValue;
    public float targetSteeringAngle;
    public float frontWheelForwardSlip;
    public float frontWheelSidewaysSlip;
    public float backWheelForwardSlip;
    public float backWheelSidewaysSlip;
    float frontWheelfcSFOriginalStiffness; //opt
    public float LeanRatio; //Imp
    public float horizontalInput; //Imp
    public float verticalInput; //Imp
    float verticalInputUp; //Imp
    float verticalInputDown; //Imp
    //float horizontalInputLeft;
    //float horizontalInputRight;
    public float COGShiftTarget; //Imp - Inspector
    public float wheelieShiftTarget; // Imp - Inspector
    float COGShiftValue; //Imp
    public float engineBraking = 0.8f;
    //public Rigidbody BikeRigidBody;
    public Vector3 OriginalRot;
    //float motorTorqueIncrease;
    private Vector3 originalPos;
    Vector3 originalCOG;
    public Vector3 currentCOG;
    bool isAccelerating;
    bool isBraking;
    Rigidbody rb;
    public float handleModifier = 1f;
    public bool useController;
    //public bool HandleSteeringON;
    public bool HandleCOGMovement;
    public bool useHandles;
    //public bool moveSuspension;
    //public float maxSteeringSpeed = 17.0f;

    [Header("Power/Braking")]
    [Space(5)]
    public float MaxSpeed = 90.0f;
    public float motorForce;
    public float brakeForce;
    public float brakeModifier;
    public Vector3 COG_Offset;
    public bool modifyFrictionON = false;

    [Space(20)]
    [HeaderAttribute("Info")]
    public float currentSteeringAngle;
    [Tooltip("Dynamic steering angle based on the speed of the RB, affected by sterReductorAmmount")]
    [Range(-45, 45)] public float currentLean;
    public float currentLeanAngle;
    [Space(20)]
    [HeaderAttribute("Speed")]
    public float currentSpeed;
    public float currentKMHSpeed;
    public float currentEnginePower;
    public float currentBrakePowerF;
    public float currentBrakePowerB;
    [Space(20)]
    [Header("Steering")]
    [Space(5)]

    public float maxSteeringAngle;
    public float LeanSpeed = 0.2f;

    //[Range(0f, 1f)] public float steerReductorAmmount;

    [Range(0f, 1f)]
    public float inputSteerRatio = 0.5f; // 0 = only lean, 1 = only input

    [Header("Steering Input/Lean Blend Curve")]
    public AnimationCurve inputSteerRatioCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f); // Default: only input at all speeds

    public float turnSmoothing = 5.0f;
    [Space(20)]
    [Header("Headlights")]
    public GameObject LeftHeadlight;
    public GameObject RightHeadlight;
    public HeadlightMode headlightMode = HeadlightMode.OFF;

    [Space(20)]
    [Header("Lean")]
    [Space(5)]

    public float maxLeanAngle = 45f;

    //[Range(0.001f, 1f)] public float leanSmoothing;
    float targetLeanAngle;

    [Space(20)]
    [Header("Object References")]
    public Transform handle;
    public Transform suspensionMoving;
    public Transform rearAxle;  // The center of rear wheel (axle)
    public Transform swingarm; // The swingarm object, pivoted at bike frame
    /*
    private Vector3 initialDirection; // Rest direction to axle
    private Quaternion baseRotation;   // Initial rotation of the swingarm object
    */
    public Vector3 rotationOffset = new Vector3(-90f, 0f, 0f); // Adjust in Inspector
    private Vector3 initialDir;
    private Quaternion baseRot;
    private Vector3 initialLocalDir;
    private Vector3 _initialLocalAxleDir;

    private Vector3 _initialLocalAxlePos;
    private Quaternion _initialLocalRotation;
    public float angleOffset = -90f; // Adjust this to fix the 90° misalignment

    //public Transform TripleClamp;
    //public Transform suspensionFixed;
    [Space(10)]
    public WheelCollider frontWheel;
    public WheelCollider backWheel;
    [Space(10)]
    public Transform frontWheelTransform;
    public Transform backWheelTransform;
    //public GameObject susTop;
    //public GameObject susBottom;
    public GameObject ClusterNeedle;
    //public GameObject TailLight;
    /*
    public GameObject FrontLeftLightReflect;
    public GameObject FrontRightLightReflect;
    public GameObject FrontLeftLightSpot;
    public GameObject FrontRightLightSpot;
    */
    public bool isLightON;
    public TMP_Text RPMText;
    public TMP_Text GearText;
    [Space(10)]
    [Header("Engine")]
    public AnimationCurve hptoRPMCurve;
    public float clutch;
    public float currentRPM;
    //public float variableValue;
    private float idleRPM = 1200.0f;
    public float maxRPM = 10000.0f;
    public float GearChangeLimit = 8000f;
    public float wheelRPM;
    private float ClusterstartPosition = 0.0f;
    private float ClusterendPosition = -270.0f;
    private float ClusterdesiredPosition;
    public float[] gearRatios;
    public float[] gearMaxSpeed;
    private GearState gearState;
    public GearBoxType gearBoxType;
    public bool isChangingGear;
    public float increaseGearRPM;
    public float decreaseGearRPM;
    public float ShiftUpTime = 0.4f;
    public float changeGearTime = 0.5f;
    //public float currentGearRatio;
    private float currentTorque;
    public bool ignitionON;
    public bool engineRunning = false;
    public bool startEngine;
    public bool clusterCheck = true;
    public int currentGear;
    public int currentGearSelect = 0;
    public float gearModifier;
    public float gearSpeedRatio;
    //private Vector3 previousVelocity;

    // ====== NEW PHYSICS VARIABLES ======
    [Header("Physics Lean Settings")]
    [SerializeField] private float leanTorqueMultiplier = 500f;
    [SerializeField] private float leanDamping = 15f;
    [SerializeField] private float uprightTorque = 200f;
    [SerializeField] private float minLeanSpeed = 5f;
    [SerializeField] private List<Transform> visualBikeParts = new();
    [SerializeField] private Transform handleBars;
    [SerializeField] private float maxHandlebarAngle = 35f;
    [SerializeField] private float steeringReturnSpeed = 2f;
    //private float targetLeanAngle;
    private float currentHandlebarAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        DontDestroyOnLoad(this.gameObject);
        rb.useGravity = false; // Gravity initially disabled to let closest scene load first
        //rb = GetComponent<Rigidbody>();
        controls = new PlayerControls();

        controls.Gameplay.Throttle.performed += ctx => throttleVal = ctx.ReadValue<float>();
        controls.Gameplay.Throttle.canceled += ctx => throttleVal = 0.0f;

        controls.Gameplay.KeyboardThrottle.performed += ctx => keyboardThrottleVal = ctx.ReadValue<float>();
        controls.Gameplay.KeyboardThrottle.canceled += ctx => keyboardThrottleVal = 0.0f;

        controls.Gameplay.Brake.performed += ctx => brakeVal = ctx.ReadValue<float>();
        controls.Gameplay.Brake.canceled += ctx => brakeVal = 0.0f;

        controls.Gameplay.KeyboardBrake.performed += ctx => keyboardBrakeVal = ctx.ReadValue<float>();
        controls.Gameplay.KeyboardBrake.canceled += ctx => keyboardBrakeVal = 0.0f;

        controls.Gameplay.Reverse.performed += ctx => ReverseVal = ctx.ReadValue<float>();
        controls.Gameplay.Reverse.canceled += ctx => ReverseVal = 0.0f;

        controls.Gameplay.Lean.performed += ctx => leftStickXAxis = ctx.ReadValue<float>();
        controls.Gameplay.Lean.canceled += ctx => leftStickXAxis = 0.0f;

        //controls.Gameplay.Camera.performed += ctx => rightStick = ctx.ReadValue<Vector2>();
        //controls.Gameplay.Camera.canceled += ctx => rightStick = new Vector2(0.0f, 0.0f);

        controls.Gameplay.GearUp.performed += ctx => GearShiftUp();
        controls.Gameplay.GearDown.performed += ctx => GearShiftDown();

        controls.Gameplay.WheelieInput.performed += ctx => wheelieValInp = ctx.ReadValue<float>();
        controls.Gameplay.WheelieInput.canceled += ctx => wheelieValInp = 0.0f;
    }
    void OnEnable()
    {
        controls.Gameplay.Enable();
        // Safe subscription
        if (SceneStreamer.Instance != null)
        {
            SceneStreamer.Instance.OnClosestSceneLoaded += EnableGravity;
        }
    }
    void OnDisable()
    {
        controls.Gameplay.Disable();
        // Safe unsubscription
        if (SceneStreamer.Instance != null)
        {
            SceneStreamer.Instance.OnClosestSceneLoaded -= EnableGravity;
        }
    }
    private bool _gravityEnabled = false;
    private void EnableGravity()
    {
        if (!_gravityEnabled)
        {
            rb.useGravity = true;
            _gravityEnabled = true;
            Debug.Log("Gravity enabled via event");
        }
    }
    void OnDestroy()
    {
        blurAmount = 0.0f;
        MotionBlur();
    }
    void GearShiftUp()
    {
        if (currentGearSelect < 6)
        {
            currentGearSelect += 1;
        }
    }
    void GearShiftDown()
    {
        if (currentGearSelect > 0)
        {
            currentGearSelect -= 1;
        }
        if (currentGearSelect < 1)
        {
            currentGearSelect = 0;
        }
    }
    private GForceScript gForceScriptObject;
    // Start is called before the first frame update
    void Start()
    {
        // Auto-assign cameras by tags
        GameObject fppCam = GameObject.FindGameObjectWithTag("FirstPersonCamera");
        GameObject tppCam = GameObject.FindGameObjectWithTag("ThirdPersonCamera");

        if (fppCam != null)
        {
            FirstPersonCameraObject = fppCam;
            FPPCameraCinemachineCamera = fppCam.GetComponent<CinemachineCamera>();
            cameraShakeFPP = fppCam.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
            FPPCamNoiseObject = cameraShakeFPP;
        }
        else
        {
            Debug.LogWarning("FirstPersonCamera with tag not found.");
        }

        if (tppCam != null)
        {
            ThirdPersonCameraObject = tppCam;
            ThirdPersonCinemachineCamera = tppCam.GetComponent<CinemachineCamera>();
            cameraShakeTPP = tppCam.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
            TPPCamNoiseObject = cameraShakeTPP;
            orbitalFollow = tppCam.GetComponent<CinemachineOrbitalFollow>();
        }
        else
        {
            Debug.LogWarning("ThirdPersonCamera with tag not found.");
        }

        frontWheel.ConfigureVehicleSubsteps(5, 30, 100);
        backWheel.ConfigureVehicleSubsteps(5, 30, 100);
        SphereCollider waterCheckRigidBody = WaterInteractionObject.GetComponent<SphereCollider>();
        wheelieSphereCollider = WheelieColliderObject.GetComponent<SphereCollider>();
        emissiveMaterial = objectToChange.GetComponent<Renderer>().material;
        //orbitalFollow = ThirdPersonCinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        //orbitalFollow = ThirdPersonCameraObject.GetComponent<CinemachineOrbitalFollow>();
        //FPPCamNoiseObject = cameraShakeFPP.GetComponent<CinemachineBasicMultiChannelPerlin>();
        //TPPCamNoiseObject = cameraShakeTPP.GetComponent<CinemachineBasicMultiChannelPerlin>();
        //FPPCameraCinemachineCamera = FirstPersonCameraObject.GetComponent<CinemachineCamera>();

        /*
        // Store the initial direction from swingarm pivot to axle in local space
        initialDirection = (rearAxle.position - swingarm.position).normalized;
        baseRotation = swingarm.localRotation;
        */
        baseRot = swingarm.transform.localRotation;
        initialDir = (rearAxle.position - swingarm.transform.position).normalized;
        initialLocalDir = swingarm.transform.InverseTransformDirection(rearAxle.position - swingarm.position).normalized;

        // Store initial direction from swingarm pivot to axle (in local space)
        _initialLocalAxleDir = swingarm.InverseTransformPoint(rearAxle.position);

        _initialLocalRotation = swingarm.localRotation;
        _initialLocalAxlePos = swingarm.InverseTransformPoint(rearAxle.position);

        originalPos = transform.position;
        OriginalRot = transform.rotation.eulerAngles;
        originalCOG = rb.centerOfMass;
        OriginalY_Position = RiderHead.transform.localPosition.y;
        RiderHeadInitialTransform = RiderHead.transform;
        SeatTargetInitialPosition = SeatTargetObject.transform.localPosition;
        clusterCheck = true;
        gearBoxType = GearBoxType.Manual;
        gForceScriptObject = GetComponent<GForceScript>();

        suspensionMoving.localPosition = new Vector3(0.0f, -0.53779f, 0.2713f);
        /*
        // Auto-calibrate suspension offset based on initial transform positions
        float currentY = frontWheelTransform.transform.localPosition.y;
        observedYMin = currentY;
        observedYMax = currentY + 0.001f; // tiny buffer to avoid zero range

        // Expected neutral midpoint values used in updateSuspension()
        initialLerpY = Mathf.Lerp(-0.616f, -0.516f, 0.5f);
        initialLerpZ = Mathf.Lerp(0.308f, 0.263f, 0.5f);

        // Capture current suspension transform as baseline
        initialSuspensionY = suspensionMoving.transform.localPosition.y;
        initialSuspensionZ = suspensionMoving.transform.localPosition.z;

        // Derive auto offsets
        suspensionYOffset = initialSuspensionY - initialLerpY;
        suspensionZOffset = initialSuspensionZ - initialLerpZ;

        
        // Auto-calibrate wheel collider Z offset
        initialWheelColliderZ = frontWheel.transform.localPosition.z;
        initialWheelLerpZ = Mathf.Lerp(0.335f, 0.290f, 0.5f);
        wheelColliderZOffset = initialWheelColliderZ - initialWheelLerpZ;
        */

    }

    private float timer = 0f; // Tracks elapsed time
    public float executionInterval = 0.85f; // Interval in seconds

    void Update()
    {
        if (!_gravityEnabled &&
            SceneStreamer.Instance != null &&
            SceneStreamer.Instance.IsClosestSceneLoaded)
        {
            EnableGravity();
        }

        GetInput();



        Speed_O_Meter();
        UpdateCluster();

        //isHIGHGForce = GetComponent<GForceScript>().isHIGHGForce;
        isHIGHGForce = gForceScriptObject.isHIGHGForce;
        MotionBlur();

        MoveRiderHead();

        UpdateHandles();

        UpdateWheels();

        updateSuspension();

        UpdateSwingarm();



        // Increment the timer by the time elapsed since the last frame
        timer += Time.deltaTime;

        // Check if the timer has exceeded the execution interval
        if (timer >= executionInterval)
        {
            ExecutePeriodicFunction(); // Call the function
            timer = 0f; // Reset the timer
        }
    }
    void ExecutePeriodicFunction()
    {
        UpdateHeadlights();

    }
    void LateUpdate()
    {
        //updateSuspension();
    }

    void FixedUpdate()
    {
        //Throttled Fixed Update Execution
        fixedFrameCounter++;
        if (fixedFrameCounter >= 100000) { fixedFrameCounter = 0; }
        if (fixedFrameCounter % fixedUpdateFrequency == 0) { throttledFixedUpdate(); }
        //Normal Fixed Update Execution
        CheckWheelHit();
        CheckCollisions();
        ChangeFrictionMultiplierStiffness();

        ModifyFriction();
        HandleEngineA();
        CheckSlip();
        ABSSystem();
        ApplyBrake();
        HandleEngineState();

        LeanOnTurn();

        ModifyInputRatios();

        HandleSteering();



        ModifySteeringLimit();

        //HandleSteering();

        if (HandleCOGMovement && validRotation == true)
        {
            /*
            if (isAccelerating == true) { MoveCOG(); }
            if (isAccelerating == false || isBraking == false) { MoveCOG(); }
            */
            MoveCOG();
        }
        //UpdateHandles();
        //MoveRiderHead();
        SeatShift();
    }


    void throttledFixedUpdate()
    {
        /*   
        if (sceneStreamer.IsClosestSceneLoaded && !rb.useGravity)
        {
            rb.useGravity = true; // Enable gravity when scene is loaded
            Debug.Log("Gravity Enabled");
        }
        */

        CheckRotationValidity();



        HandleCamera();

        CheckGearSpeeds();
        HandleElectronics();

    }
    void CheckGearSpeeds()
    {
        if (currentGear != 0)
        {
            previousGearSpeed = gearMaxSpeed[currentGear - 1];
        }
        if (currentGear < 5)
        {
            nextGearSpeed = gearMaxSpeed[currentGear + 1];
        }
        else
        {
            nextGearSpeed = 0.0f;
        }
        currentGearSpeed = gearMaxSpeed[currentGear];
    }
    void UpdateCluster()
    {
        UpdateNeedle();
        string RPMTextVal;
        if (currentRPM < 10000)
        {
            RPMTextVal = currentRPM.ToString("0000");
            RPMText.SetText(RPMTextVal);
        }
        if (currentRPM > 9999)
        {
            RPMTextVal = currentRPM.ToString("00000");
            RPMText.SetText(RPMTextVal);
        }
        string GearTextVal = (gearState == GearState.Neutral) ? "N" : (currentGear + 1).ToString();
        GearText.SetText(GearTextVal);
    }
    //private float resetHoldTime = 2f; // Hold for 2 seconds
    //private float holdTimer = 0f;
    //private bool isHolding = false;
    private float holdTimer = 0f;
    private bool isCoroutineRunning = false;
    [SerializeField] private float holdThreshold = 0.5f;
    private void ResetBikeWithFade()
    {
        if (Time.time - lastResetTime < resetCooldown) return;

        Transform spawnPoint = FindNearestSpawnPoint();
        if (spawnPoint == null) return;

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutIn(resetFadeDuration, () =>
            {

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                transform.position = spawnPoint.position + resetOffset;
                transform.rotation = spawnPoint.rotation;


                lastResetTime = Time.time;
                isResetting = true; // Ensure this is set before starting coroutine

                // Stop any existing coroutine and start new one
                if (isCoroutineRunning)
                {
                    StopCoroutine("CheckWheelAfterReset");
                }
                StartCoroutine(CheckWheelAfterReset());
            });
        }
        else
        {
            // Instant reset fallback
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = spawnPoint.position + resetOffset;
            transform.rotation = spawnPoint.rotation;

            lastResetTime = Time.time;
            isResetting = true;

            if (isCoroutineRunning)
            {
                StopCoroutine("CheckWheelAfterReset");
            }
            StartCoroutine(CheckWheelAfterReset());
        }

        TouchingWater = false;
        CollidedBody = false;
    }
    private Transform FindNearestSpawnPoint()
    {
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Respawn");
        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject go in spawns)
        {
            float dist = Vector3.Distance(transform.position, go.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = go.transform;
            }
        }

        return nearest;
    }
    private IEnumerator CheckWheelAfterReset()
    {
        isCoroutineRunning = true;

        // Wait for a short moment to avoid immediate checks
        yield return new WaitForSeconds(0.5f);

        // Keep checking until the bike is grounded
        while (true)
        {
            // Check if both wheels are grounded
            if (frontWheelHit && backWheelHit)
            {
                isResetting = false;
                validRotation = true;
                CollidedBody = false;
                break;
            }

            // Also check if we're no longer resetting (in case reset was aborted)
            if (!isResetting)
            {
                break;
            }

            yield return null; // Wait for next frame
        }

        isCoroutineRunning = false;
    }


    void GetInput()
    {
        //float rawHorizontalInput = Input.GetAxis("Horizontal");
        //horizontalInput = Mathf.Lerp(horizontalInput, rawHorizontalInput, 10*Time.deltaTime);

        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log("Inertia Vals");
            Debug.Log(rb.inertiaTensor);
            Debug.Log("Slip Data OUT");
            Debug.Log("MaxSlip FF " + maxSlipFF);
            Debug.Log("MaxSlip FS " + maxSlipFS);
            Debug.Log("MaxSlip BF " + maxSlipBF);
            Debug.Log("MaxSlip BS " + maxSlipBS);
            Debug.Log("------------------------------------");
        }
        //verticalInput = Input.GetAxis("Vertical");
        if (useController)
        {
            verticalInputUp = throttleVal;
            verticalInputDown = -brakeVal;
            float horizontalInputTemp = leftStickXAxis;

            float smoothCurrentVelocity = 0.0f;
            float speedHorizontalSmoothTime = Mathf.Lerp(0.03f, 0.06f, (currentSpeed / MaxSpeed));
            horizontalInput = Mathf.SmoothDamp(horizontalInput, horizontalInputTemp, ref smoothCurrentVelocity, speedHorizontalSmoothTime);

            //horizontalInput = horizontalInputTemp;
            if (currentSpeed < 5.0f)
            {
                if (ReverseVal > 0.01f)
                {
                    frontWheel.brakeTorque = 0.0f;
                    backWheel.brakeTorque = 0.0f;
                    frontWheel.motorTorque = -80.0f;
                }
                else
                {
                    frontWheel.motorTorque = 0.0f;
                }
            }
            float wheelieValVelocity = 0.0f;
            float wheelieValVelocityDown = 0.0f;
            if (wheelieValInp > 0.25f)
            {
                wheelieVal = Mathf.SmoothDamp(wheelieVal, wheelieValInp, ref wheelieValVelocity, 0.1f);
            }
            else
            {
                wheelieVal = Mathf.SmoothDamp(wheelieVal, wheelieValInp, ref wheelieValVelocityDown, 0.115f);
            }




        }
        else
        {
            verticalInputUp = keyboardThrottleVal;
            verticalInputDown = -keyboardBrakeVal;
            float horizontalInputTemp = Input.GetAxis("Horizontal");
            //float smoothCurrentVelocity = 0.0f;
            float speedHorizontalSmoothTime = Mathf.Lerp(0.045f, 0.065f, currentSpeed / MaxSpeed);
            /*
            if (horizontalInputTemp <= 0.15f || horizontalInputTemp >= 0.15f)
            {
                horizontalInput = horizontalInputTemp;
            }
            else
            {
                horizontalInput = Mathf.SmoothDamp(horizontalInput, horizontalInputTemp, ref smoothCurrentVelocity, speedHorizontalSmoothTime);
            }
            */
            horizontalInput = horizontalInputTemp;


            /*
            if (verticalInput == 0.0 && verticalInputUp != 0.0f)
            {
                verticalInputUp = 0.0f;
            }
            if (verticalInput == 0.0 && verticalInputDown != 0.0f)
            {
                verticalInputDown = 0.0f;
            }
            if (verticalInput > 0.0f)
            {
                verticalInputUp = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
                verticalInputDown = 0.0f;
            }
            if (verticalInput < 0.0f)
            {
                verticalInputDown = Mathf.Clamp(verticalInput, -1.0f, 0.0f);
                verticalInputUp = 0.0f;
            }
            if (verticalInput == 0.0f)
            {
                verticalInputUp = 0.0f;
                verticalInputDown = 0.0f;
            }
            */

            //verticalInputUp = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
            //verticalInputDown = Mathf.Clamp(verticalInput, -1.0f, 0.0f);
        }
        if (Input.GetKey("l"))
        {
            //rb.AddRelativeTorque(0.0f, 0.0f, 5.0f);
            //rb.AddTorque(Vector3.forward * 10.0f);
            rb.AddTorque(rb.transform.forward * 1000.0f);
        }
        if (Input.GetKey("j"))
        {
            //rb.AddRelativeTorque(0.0f, 0.0f, -5.0f);
            rb.AddTorque(rb.transform.forward * -1000.0f);
            //rb.AddTorque(Vector3.forward * -10.0f);
        }

        if (Input.GetKey("r"))
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdThreshold)
            {
                isResetting = true;
                ResetBikeWithFade();

                Debug.Log("Resetting Position");
                GetComponent<Bouyancy>().enabled = false;
                TouchingWater = false;
                CollidedBody = false;
                isMidAir = false;
                //validRotation = true;

                //transform.position = originalPos;
                //rb.MovePosition(originalPos);
                //transform.eulerAngles = OriginalRot;
                if (currentGear != 0)
                {
                    currentGear = 0;
                    currentGearSelect = 0;
                }
                blurAmount = 0.0f;
                isResetting = false;
                if (frontWheel.isGrounded == true && backWheel.isGrounded == true && BodyCollided == false)
                {
                    isResetting = false;
                    validRotation = true;
                }
                if (isResetting == true && groundHeight < 0.5f)
                {
                    isResetting = false;
                    validRotation = true;
                }

                holdTimer = 0f; // prevent repeat
            }

        }

        if (currentGear < 0) // For out of index error prevention
        {
            currentGear = 0;
            currentGearSelect = 0;
        }
        /*
        if (Input.GetKeyDown(KeyCode.LeftShift)) // UPSHIFT
        {

            if (currentGearSelect < 6)
            {
                currentGearSelect += 1;
            }


        }
        if (Input.GetKeyDown(KeyCode.LeftControl)) // DOWNSHIFT
        {

            if (currentGearSelect > 0)
            {
                currentGearSelect -= 1;
            }
            if (currentGearSelect < 1)
            {
                currentGearSelect = 0;
            }
        }
        */

        if (Input.GetKeyDown("q")) // TOGGLE GEARBOX MODE
        {
            if (gearBoxType == GearBoxType.Manual)
            {
                gearBoxType = GearBoxType.Automatic;
            }
            else
            {
                gearBoxType = GearBoxType.Manual;
            }
        }
        /*
        if (Input.GetKeyDown("v")) // TOGGLE CAMERA MODE
        {
            if (cameraMode == CameraMode.FirstPerson)
            {
                cameraMode = CameraMode.ThirdPerson;
            }
            else
            {
                cameraMode = CameraMode.FirstPerson;
            }
        }
        */

        if (Input.GetKeyDown(KeyCode.V))
        {
            CycleCameraMode();
        }
        HandleReverseCamera();

        if (Input.GetKeyDown("o")) //CONTROLLER TOGGLE
        {
            if (useController == false)
            {
                useController = true;
            }
            else
            {
                useController = false;
            }
        }
        if (Input.GetKeyDown("i")) //IGNITION TOGGLE
        {
            if (ignitionON == false)
            {
                ignitionON = true;
            }
            else
            {
                ignitionON = false;
            }
        }
        if (Input.GetKeyDown("h")) // ENGINE START/STOP TOGGLE
        {
            if (startEngine == false)
            {
                startEngine = true;
            }
            else
            {
                startEngine = false;
            }
        }

        if (Input.GetKeyDown("f")) //IGNITION TOGGLE
        {
            /*
            if (headlightMode != HeadlightMode.OFF)
            {
                isLightON = true;
            }
            if (headlightMode == HeadlightMode.OFF)
            {
                isLightON = false;
            }
            */
            CycleHeadlightMode();
        }


        if (gearState != GearState.Changing)
        {
            if (gearState == GearState.Neutral)
            {
                clutch = 0;
                if (Mathf.Abs(verticalInputUp) > 0) gearState = GearState.Running;
            }
            else
            {
                clutch = Input.GetKey("g") ? 0 : Mathf.Lerp(clutch, 1, Time.deltaTime);
            }
        }
        clutch = Input.GetKey("g") ? 0 : Mathf.Lerp(clutch, 1.0f, Time.deltaTime);

    }
    void CycleCameraMode()
    {
        int modeCount = System.Enum.GetValues(typeof(CameraMode)).Length;
        int nextModeIndex = ((int)cameraMode + 1) % modeCount;
        cameraMode = (CameraMode)nextModeIndex;
        UpdateCameraPriority();
    }
    void HandleReverseCamera()
    {
        if (reverseCamera == null) return;

        if (Input.GetKey(KeyCode.C))
        {
            // While holding C — override everything
            reverseCamera.Priority = overridePriority;
        }
        else
        {
            // Reset reverse cam priority when not held
            if (reverseCamera.Priority != lowPriority)
            {
                reverseCamera.Priority = lowPriority;
                UpdateCameraPriority(); // Restore normal camera
            }
        }
    }
    void UpdateCameraPriority()
    {
        foreach (var setup in cameraSetups)
        {
            if (setup.vCam != null)
            {
                setup.vCam.Priority = (setup.mode == cameraMode) ? 10 : 0;
            }
        }
    }
    private void CycleHeadlightMode()
    {
        headlightMode = (HeadlightMode)(((int)headlightMode + 1) % 4); // Cycle through the enum states
    }

    private void UpdateHeadlights()
    {
        switch (headlightMode)
        {
            case HeadlightMode.OFF:
                SetHeadlightEmission(LeftHeadlight, false);
                SetHeadlightEmission(RightHeadlight, false);
                break;
            case HeadlightMode.LeftOnly:
                SetHeadlightEmission(LeftHeadlight, true);
                SetHeadlightEmission(RightHeadlight, false);
                break;
            case HeadlightMode.RightOnly:
                SetHeadlightEmission(LeftHeadlight, false);
                SetHeadlightEmission(RightHeadlight, true);
                break;
            case HeadlightMode.FULL:
                SetHeadlightEmission(LeftHeadlight, true);
                SetHeadlightEmission(RightHeadlight, true);
                break;
        }
    }

    private void SetHeadlightEmission(GameObject headlight, bool enable)
    {
        if (headlight != null)
        {
            var material = headlight.GetComponent<Renderer>().material;
            if (enable)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
        }
    }

    void HandleElectronics()
    {
        if (verticalInputDown < 0.0f)
        {
            if (TailLightON == false) { turnOnTailLight(); }

        }
        else
        {
            if (TailLightON == true) { turnOffTailLight(); }

        }

        if (headlightMode != HeadlightMode.OFF)
        {
            isLightON = true;
            TurnONHeadlights.Invoke();
        }
        if (headlightMode == HeadlightMode.OFF)
        {
            isLightON = false;
            TurnOFFHeadlights.Invoke();
        }
        /*
        if (isLightON == true)
        {
            TurnONHeadlights.Invoke();

        }
        if (isLightON == false)
        {

            TurnOFFHeadlights.Invoke();

        }
        */


    }

    void ApplyMotor()
    {
        currentTorque = CalculateTorque();
        //frontWheel.motorTorque = 0.0001f;
        if (engineRunning == true && startEngine == true && verticalInputUp > 0.0f)
        {
            backWheel.motorTorque = currentTorque * verticalInputUp;
            //frontWheel.motorTorque = currentTorque * verticalInputUp;
        }
        else
        {
            currentTorque = 0.0f;
            backWheel.motorTorque = 0.0f;
        }
        currentEnginePower = backWheel.motorTorque;
    }
    float CalculateTorque()
    {
        float torque = 0;
        if (currentRPM < idleRPM + 200 && verticalInputUp < 0.1f && currentGear == 0)
        {
            gearState = GearState.Neutral;
        }
        if (gearBoxType == GearBoxType.Automatic)
        {
            float increaseGearRPMTemp = increaseGearRPM;
            float decreaseGearRPMTemp = decreaseGearRPM;
            if (gearState == GearState.Running && clutch > 0)
            {
                if (currentRPM > increaseGearRPMTemp)
                {
                    StartCoroutine(ChangeGear(1));
                }
                if (currentRPM < decreaseGearRPMTemp)
                {
                    StartCoroutine(ChangeGear(-1));
                }
            }
        }
        if (gearBoxType == GearBoxType.Manual)
        {
            float increaseGearRPMTemp = idleRPM;
            float decreaseGearRPMTemp = maxRPM;
            HandleGearBox(increaseGearRPMTemp, decreaseGearRPMTemp);
        }
        if (engineRunning == true)
        {
            if (clutch < 0.1f) //Clutch Engaged
            {
                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM, maxRPM * verticalInputUp) + UnityEngine.Random.Range(-50, 50), 2.0f * Time.deltaTime);
            }
            else //Clutch disengaged - Connected
            {
                wheelRPM = Mathf.Abs(backWheel.rpm) * gearRatios[currentGear];
                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM - 100, wheelRPM), Time.deltaTime * 3f) + UnityEngine.Random.Range(-10, 10);
                if (currentRPM > maxRPM) currentRPM = maxRPM; // Clamp RPM to maxRPM
                torque = (hptoRPMCurve.Evaluate(currentRPM / maxRPM) * motorForce / currentRPM) * gearRatios[currentGear] * 5252f * clutch;
            }
        }
        else
        {
            if (currentRPM > maxRPM)
            {
                currentRPM = maxRPM; // Clamp RPM to maxRPM
            }
            torque = 0.0f;
        }
        return torque;
    }
    void HandleEngineA()
    {
        if (ignitionON == true)
        {
            ClusterSpeedoReadout.SetActive(true);
            if (!startEngine && !engineRunning) // Engine off and not started
            {
                if (clusterCheck == true && currentRPM == 0.0f)
                {
                    StartCoroutine(AdjustRPM(currentRPM, maxRPM, 0.65f));
                }
                if (clusterCheck == true && currentRPM == maxRPM)
                {
                    StartCoroutine(AdjustRPM(currentRPM, 0.0f, 0.65f));
                    clusterCheck = false;
                }
                if (clusterCheck == false && currentRPM == 0.0f)
                {
                    //StopAllCoroutines();
                    StopCoroutine("AdjustRPM");
                }
                else if (clusterCheck == false && currentRPM > 0.0f)
                {
                    //IGNORE
                }
            }
            if (startEngine && !engineRunning) // Engine started into idle state
            {
                clusterCheck = false;
                engineRunning = true;
                //StopAllCoroutines();
                StopCoroutine("AdjustRPM");
                StartCoroutine(SmoothIncreaseRPM(0.0f, idleRPM, 0.6f));
                currentTorque = 0.0f;
                //startEngine = false;
            }
            if (engineRunning && startEngine == true) // Engine started into running state
            {
                //startEngine = false;
                //StopAllCoroutines();
                ApplyMotor();
            }
            if (!startEngine && engineRunning) // Turning Engine Off from Running state
            {
                clutch = 1.0f;
                //StopAllCoroutines();
                StartCoroutine(SmoothDecreaseRPM(currentRPM, 0.0f, 0.5f));
                if (currentRPM == 0.0f)
                {
                    StopAllCoroutines();
                }
                engineRunning = false;
            }
        }
        if (ignitionON == false)
        {
            if (ignitionON == false && engineRunning == true)
            {
                StartCoroutine(SmoothDecreaseRPM(currentRPM, 0.0f, 0.65f));
            }
            engineRunning = false;
            startEngine = false;
            clusterCheck = true;
            ClusterSpeedoReadout.SetActive(false);
            //StopCoroutine("EngineCheck");
            //StopAllCoroutines();
            StartCoroutine(SmoothDecreaseRPM(currentRPM, 0.0f, 0.65f));
            if (currentRPM == 0.0f)
            {
                StopAllCoroutines();
            }
            backWheel.motorTorque = 0.0f;
            //StopAllCoroutines();
            //currentRPM = 0.0f;
        }

    }

    public IEnumerator SmoothIncreaseRPM(float startRPM, float endRPM, float duration)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            currentRPM = Mathf.Lerp(startRPM, endRPM, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentRPM = endRPM;
    }

    public IEnumerator SmoothDecreaseRPM(float startRPM, float endRPM, float duration)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            currentRPM = Mathf.Lerp(startRPM, endRPM, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentRPM = endRPM;
    }
    public IEnumerator EngineCheck(float endRPM, float duration)
    {
        //Zero to Max case
        float elapsed = 0.0f;
        if (currentRPM < maxRPM)
        {
            while (elapsed < duration)
            {
                currentRPM = Mathf.Lerp(0.0f, endRPM, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        if (currentRPM >= maxRPM - 100.0f)
        {
            //Max to Zero case
            elapsed = 0.0f;

            while (elapsed < duration)
            {
                currentRPM = Mathf.Lerp(endRPM, 0.0f, elapsed / duration);
                elapsed += Time.deltaTime;
                if (currentRPM == 0.0f) { clusterCheck = false; }
                yield return null;
            }

        }

    }
    IEnumerator ChangeGear(int gearChange)
    {
        if (isChangingGear)
        {
            yield break; // Exit if the coroutine is already running
        }

        isChangingGear = true;
        gearState = GearState.CheckingChange;
        if (currentGear + gearChange >= 0)
        {
            if (gearBoxType == GearBoxType.Automatic)
            {
                if (gearChange > 0)
                {
                    yield return new WaitForSeconds(ShiftUpTime);
                    //increase gear
                    if (currentRPM < increaseGearRPM || currentGear >= gearRatios.Length - 1)
                    {
                        gearState = GearState.Running;
                        isChangingGear = false;
                        yield break;
                    }
                }
                if (gearChange < 0)
                {
                    //decrease gear
                    yield return new WaitForSeconds(changeGearTime);


                    if (currentRPM > decreaseGearRPM || currentGear <= 0)
                    {
                        gearState = GearState.Running;
                        isChangingGear = false;
                        yield break;
                    }

                }
                gearState = GearState.Changing;
                yield return new WaitForSeconds(changeGearTime);
                currentGear += gearChange;
            }
            if (gearBoxType == GearBoxType.Manual)
            {
                if (gearChange > 0)
                {
                    yield return new WaitForSeconds(0.1f);
                    //increase gear
                    if (currentGear >= gearRatios.Length - 1)
                    {
                        gearState = GearState.Running;
                        isChangingGear = false;
                        yield break;
                    }
                }
                if (gearChange < 0)
                {
                    //decrease gear
                    yield return new WaitForSeconds(0.1f);

                    if (currentGear <= 0)
                    {
                        gearState = GearState.Running;
                        isChangingGear = false;
                        yield break;
                    }
                }
                gearState = GearState.Changing;
                yield return new WaitForSeconds(0.1f);
                currentGear += gearChange;
            }
        }

        if (gearState != GearState.Neutral) { gearState = GearState.Running; }
        isChangingGear = false;
    }
    IEnumerator AdjustRPM(float startValue, float endValue, float time)
    {
        float elapsed = 0.0f;

        while (elapsed < time)
        {
            currentRPM = Mathf.Lerp(startValue, endValue, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentRPM = endValue; // Ensure exact final value
    }

    // --------------------------------------------------------------------------------------------------
    void HandleGearBox(float increaseGearRPMTemp, float decreaseGearRPMTemp)
    {
        if (isChangingGear == false)
        {
            if (currentGearSelect > currentGear)
            {
                StartCoroutine(ChangeGear(1));
            }
            if (currentGearSelect < currentGear)
            {
                /*
                if (currentSpeed > previousGearSpeed / 3.6) 
                {
                    currentGearSelect = currentGear;
                    Debug.Log("Cannot downshift at this speed");
                }
                */
                if (currentSpeed > previousGearSpeed / 3.6 && currentRPM > GearChangeLimit) //Higher speed and higher RPM-NOT VALID
                {
                    currentGearSelect = currentGear;
                    Debug.Log("Cannot downshift at this speed");
                }
                if (currentSpeed > previousGearSpeed / 3.6 && currentRPM <= GearChangeLimit) // Higher Speed but lower RPM-VALID 
                {
                    StartCoroutine(ChangeGear(-1));
                }
                if (currentSpeed < previousGearSpeed / 3.6) // Lower Speed - VALID
                {
                    StartCoroutine(ChangeGear(-1));
                }





            }
        }
        else
        {
            //IGNORE
        }
        float currentGearMaxSpeed = gearMaxSpeed[currentGear];
        gearSpeedRatio = currentSpeed / currentGearMaxSpeed;
        gearModifier = motorForce * gearSpeedRatio;

    }
    //---------------------------------------------------------------------------------------------------

    public void HandleEngineState()
    {
        if (verticalInputUp > 0.0f)
        {
            isBraking = false;
            isAccelerating = true;
        }
        if (verticalInputDown < 0.0f)
        {
            isBraking = true;
            isAccelerating = false;
        }
        if (verticalInputUp == 0.0f || verticalInputDown == 0.0f)
        {
            isAccelerating = false;
            isBraking = false;
        }
    }
    void ApplyBrake()
    {
        if (isBraking == true)
        {
            backWheel.motorTorque = 0.0f;
        }
        else
        {
            backWheel.motorTorque = currentTorque * verticalInputUp;
        }

        if (frontWheel.isGrounded == true)
        {
            frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * brakeModifier * Mathf.Lerp(1.0f, 1.35f, currentSpeed / MaxSpeed);
        }
        if (frontWheel.isGrounded != true)
        {
            frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * brakeModifier * Mathf.Lerp(0.2f, 0.35f, currentSpeed / MaxSpeed) * 0.15f;
        }

        //frontWheel.brakeTorque = currentFrontBrakeTorque * Mathf.Abs(verticalInputDown);
        backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * 0.2f;

        currentBrakePowerF = frontWheel.brakeTorque;
        currentBrakePowerB = backWheel.brakeTorque;
    }
    void ABSSystem()
    {
        if (verticalInputDown < -0.01f)
        {
            if (frontWheelSidewaysSlip > 0.25f && frontWheelSidewaysSlip <= 1.0f || frontWheelSidewaysSlip < -0.25f && frontWheelSidewaysSlip >= -1.0f)
            {
                if (brakeModifier > 0.4)
                {
                    brakeModifier -= 0.05f;
                }
                else { brakeModifier = 0.65f; }
            }
            if (frontWheelSidewaysSlip <= 0.25f && frontWheelSidewaysSlip > 0.0f || frontWheelSidewaysSlip >= -0.25f && frontWheelSidewaysSlip < 0.0f)
            {
                brakeModifier = 1.0f;
            }
        }
        else
        {
            brakeModifier = 1.0f;
        }
        float frontWheelRPM = frontWheel.rpm;
        float backWheelRPM = backWheel.rpm;
        /*
        if (frontWheelRPM < 20.0f)
        {
            currentFrontBrakeTorque = brakeForce * 0.1f;
        }
        else
        {
            currentFrontBrakeTorque = brakeForce;
        }
        */


    }


    public void Accelerate()
    {
        backWheel.motorTorque = Mathf.Lerp(0.0f, motorForce, verticalInputUp);
    }
    public void EngineBrake()
    {
        if (verticalInputUp < 0.2f && verticalInputDown > -0.2f && rb.linearVelocity.magnitude > 4.0f)
        {
            backWheel.motorTorque = 0.0f;
            backWheel.brakeTorque = (brakeForce * engineBraking);

        }
        if (rb.linearVelocity.magnitude < 4.0f)
        {
            backWheel.brakeTorque = 0.0f;
            frontWheel.brakeTorque = 0.0f;
        }
    }
    public void ApplyBraking()
    {
        frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce);
        backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce);

    }
    /*
    public void HandleSteering()
    {
        if (currentSpeed <= 5.0f)
        {
            float maxSteeringAngletemp = 37.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
            //targetSteeringAngle = maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 5.0f && currentSpeed < 15.0f)
        {
            float maxSteeringAngletemp = 30.0f;
            //maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 15.0f && currentSpeed < 35.0f)
        {
            float maxSteeringAngletemp;
            if (isBraking == true)
            {
                maxSteeringAngletemp = 17.0f;
            }
            else
            {
                maxSteeringAngletemp = 14.0f;
            }

            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;

        }
        if (currentSpeed >= 35.0f && currentSpeed < 45.0f)
        {
            float maxSteeringAngletemp;
            if (isBraking == true)
            {
                maxSteeringAngletemp = 14.0f;
            }
            else
            {
                maxSteeringAngletemp = 12.0f;
            }
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 45.0f && currentSpeed < 60.0f)
        {
            float maxSteeringAngletemp;
            if (isBraking == true)
            {
                maxSteeringAngletemp = 12.0f;
            }
            else
            {
                maxSteeringAngletemp = 10f;
            }
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 60.0f && currentSpeed < 80.0f)
        {
            float maxSteeringAngletemp = 6.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        // Calculate the target steering angle based on the current lean angle
        targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngle;
        // Smoothly interpolate the current steering angle towards the target steering angle
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
        frontWheel.steerAngle = currentSteeringAngle;
    }
    */

    /*
    public void HandleSteering()
    {
        if (validRotation == true)
        {
            if (currentSpeed <= 5.0f)
            {
                float t = (currentSpeed - 0.0f) / (5.0f - 0.0f);
                float maxSteeringAngletemp = Mathf.Lerp(31.0f, 15.0f, currentSpeed / 5.0f);
                //float maxSteeringAngletemp = Mathf.Lerp(27.0f, 24.0f, t);

                maxSteeringAngle = maxSteeringAngletemp;
                targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
                //targetSteeringAngle = maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                frontWheel.steerAngle = (currentSteeringAngle * SteeringMultiplier);
            }
            if (currentSpeed > 5.0f && currentSpeed <= 15.0f)
            {
                float t = (currentSpeed - 5.0f) / (15.0f - 5.0f);
                float maxSteeringAngletemp = Mathf.Lerp(15.0f, 8.0f, currentSpeed / 15.0f);
                //float maxSteeringAngletemp = Mathf.Lerp(24.0f, 20.0f, t);

                //maxSteeringAngle = maxSteeringAngletemp;
                //targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
                //targetSteeringAngle = (-currentLeanAngle / 20.0f) * maxSteeringAngletemp;
                targetSteeringAngle = (((-currentLeanAngle / 20.0f) + horizontalInput) / 2.0f) * maxSteeringAngletemp;

                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;
            }
            //-------------------------------------------------------------------------------------------------------------

            if (currentSpeed > 15.0f && currentSpeed <= 35.0f)
            {
                float maxSteeringAngletemp;

                float t = (currentSpeed - 15.0f) / (35.0f - 15.0f);
                maxSteeringAngletemp = Mathf.Lerp(8.0f, 6.0f, currentSpeed / 35.0f);
                //maxSteeringAngletemp = Mathf.Lerp(20.0f, 14f, t);



                maxSteeringAngle = maxSteeringAngletemp;
                //targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
                targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                //frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;

            }
            if (currentSpeed > 35.0f && currentSpeed <= 45.0f)
            {
                float maxSteeringAngletemp;

                float t = (currentSpeed - 35.0f) / (45.0f - 35.0f);
                maxSteeringAngletemp = Mathf.Lerp(7.0f, 4.0f, currentSpeed / 45.0f);
                //maxSteeringAngletemp = Mathf.Lerp(14f, 8.5f, t);

                //maxSteeringAngle = maxSteeringAngletemp;
                targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                //frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;
            }
            if (currentSpeed > 45.0f && currentSpeed <= 60.0f)
            {
                float maxSteeringAngletemp;
                if (isBraking == true)
                {
                    float t = (currentSpeed - 45.0f) / (60.0f - 45.0f);
                    //maxSteeringAngletemp = Mathf.Lerp(4.0f, 3.0f, currentSpeed / 60.0f);
                    maxSteeringAngletemp = Mathf.Lerp(4.0f, 3.0f, t);
                    //maxSteeringAngletemp = Mathf.Lerp(8.5f, 5.5f, t);
                }
                else
                {
                    float t = (currentSpeed - 45.0f) / (60.0f - 45.0f);
                    //maxSteeringAngletemp = Mathf.Lerp(4.0f, 3.0f, currentSpeed / 60.0f);
                    maxSteeringAngletemp = Mathf.Lerp(4.0f, 3.0f, t);
                    //maxSteeringAngletemp = Mathf.Lerp(8.5f, 5f, t);
                }
                //maxSteeringAngle = maxSteeringAngletemp;
                targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                //frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;
            }
            if (currentSpeed > 60.0f && currentSpeed <= MaxSpeed)
            {
                float maxSteeringAngletemp;
                //float maxSteeringAngletemp = 3.0f;
                float t = (currentSpeed - 60.0f) / (MaxSpeed - 60.0f);
                //maxSteeringAngletemp = Mathf.Lerp(3.0f, 2.0f, currentSpeed / MaxSpeed);
                maxSteeringAngletemp = Mathf.Lerp(3.0f, 2.0f, t);
                //maxSteeringAngletemp = Mathf.Lerp(5f, 2.5f, t);

                targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                //frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;
            }
            if (currentSpeed > MaxSpeed)
            {
                float maxSteeringAngletemp;
                //float maxSteeringAngletemp = 3.0f;

                maxSteeringAngletemp = 2.0f;
                //maxSteeringAngletemp = Mathf.Lerp(5f, 2.5f, t);

                targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                //frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;
            }
        }
        else
        {
            float maxSteeringAngletemp = 3.0f;
            targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
        }
        frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;

    }
    */
    public void ModifyInputRatios()
    {
        // X axis: normalized speed (0.0 = stopped, 1.0 = max speed)
        float speed01 = Mathf.Clamp01(currentSpeed / MaxSpeed);
        // Y axis: input steer ratio (0.0 = only lean, 1.0 = only input)
        inputSteerRatio = Mathf.Clamp01(inputSteerRatioCurve.Evaluate(speed01));
    }

    public void HandleSteering()
    {
        float leanRatio = 1.0f - inputSteerRatio;

        if (validRotation == true)
        {
            if (currentSpeed <= 5.0f)
            {
                float t = (currentSpeed - 0.0f) / (5.0f - 0.0f);
                float maxSteeringAngletemp = Mathf.Lerp(28.0f, 12.0f, currentSpeed / 5.0f);

                maxSteeringAngle = maxSteeringAngletemp;
                targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                frontWheel.steerAngle = (currentSteeringAngle * SteeringMultiplier);
            }
            if (currentSpeed > 5.0f && currentSpeed <= 15.0f)
            {
                float t = (currentSpeed - 5.0f) / (15.0f - 5.0f);
                float maxSteeringAngletemp = Mathf.Lerp(12.0f, 8.0f, currentSpeed / 15.0f);

                targetSteeringAngle = (horizontalInput * inputSteerRatio + (-currentLeanAngle / 35.0f) * leanRatio) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
                frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;
            }
            if (currentSpeed > 15.0f && currentSpeed <= 35.0f)
            {
                float maxSteeringAngletemp;
                float t = (currentSpeed - 15.0f) / (35.0f - 15.0f);
                maxSteeringAngletemp = Mathf.Lerp(8.0f, 6.0f, currentSpeed / 35.0f);

                maxSteeringAngle = maxSteeringAngletemp;
                targetSteeringAngle = (horizontalInput * inputSteerRatio + (-currentLeanAngle / maxLeanAngle) * leanRatio) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
            }
            if (currentSpeed > 35.0f && currentSpeed <= 45.0f)
            {
                float maxSteeringAngletemp;
                float t = (currentSpeed - 35.0f) / (45.0f - 35.0f);
                maxSteeringAngletemp = Mathf.Lerp(6.0f, 3.0f, currentSpeed / 45.0f);

                targetSteeringAngle = (horizontalInput * inputSteerRatio + (-currentLeanAngle / maxLeanAngle) * leanRatio) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
            }
            if (currentSpeed > 45.0f && currentSpeed <= 60.0f)
            {
                float maxSteeringAngletemp;
                float t = (currentSpeed - 45.0f) / (60.0f - 45.0f);
                maxSteeringAngletemp = Mathf.Lerp(3.0f, 2.0f, t);

                targetSteeringAngle = (horizontalInput * inputSteerRatio + (-currentLeanAngle / maxLeanAngle) * leanRatio) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
            }
            if (currentSpeed > 60.0f && currentSpeed <= MaxSpeed)
            {
                float maxSteeringAngletemp;
                float t = (currentSpeed - 60.0f) / (MaxSpeed - 60.0f);
                maxSteeringAngletemp = Mathf.Lerp(2.0f, 1.5f, t);

                targetSteeringAngle = (horizontalInput * inputSteerRatio + (-currentLeanAngle / maxLeanAngle) * leanRatio) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
            }
            if (currentSpeed > MaxSpeed)
            {
                float maxSteeringAngletemp = 1.5f;

                targetSteeringAngle = (horizontalInput * inputSteerRatio + (-currentLeanAngle / maxLeanAngle) * leanRatio) * maxSteeringAngletemp;
                currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
            }
        }
        else
        {
            float maxSteeringAngletemp = 3.0f;
            targetSteeringAngle = (horizontalInput * inputSteerRatio + (-currentLeanAngle / maxLeanAngle) * leanRatio) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * Time.deltaTime);
        }
        frontWheel.steerAngle = currentSteeringAngle * SteeringMultiplier;
    }

    public void UpdateHandles()
    {
        if (useHandles == true)
        {

            if (currentSpeed <= 5.0f)
            {
                float handleModifiertemp = currentSteeringAngle;
                //float handleModifierFinalTemp = Mathf.Lerp(handleModifiertemp, 0.0f, Mathf.Abs(LeanRatio));

                //float leanFactor = 1.0f - Mathf.Abs(LeanRatio); // Inverse relationship with LeanRatio

                //float handleModifierFinalTemp = Mathf.Lerp(handleModifiertemp, -handleModifiertemp, 1.0f - leanFactor);

                //float handleModifierFinalTemp = handleModifiertemp * leanFactor;

                float handleModifierFinalTemp = handleModifiertemp;
                //handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle * handleModifiertemp, handle.localEulerAngles.z);
                handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, handleModifierFinalTemp, handle.localEulerAngles.z);
            }
            if (currentSpeed > 5.0f && currentSpeed <= 15.0f)
            {
                float t = (currentSpeed - 5.0f) / (15.0f - 5.0f);
                //float handleModifiertemp = Mathf.Lerp(0.85f, 0.5f, currentSpeed / 15.0f);
                float handleModifiertemp = currentSteeringAngle * Mathf.Lerp(1.0f, 0.0f, t);

                //float handleModifierFinalTemp = Mathf.Lerp(handleModifiertemp, 0.0f, Mathf.Abs(LeanRatio));
                //float leanFactor = 1.0f - Mathf.Abs(LeanRatio); // Inverse relationship with LeanRatio
                //float handleModifierFinalTemp = Mathf.Lerp(handleModifiertemp, -handleModifiertemp, 1.0f - leanFactor);

                //float handleModifierFinalTemp = handleModifiertemp * leanFactor;
                float handleModifierFinalTemp = handleModifiertemp;
                //handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle * handleModifiertemp, handle.localEulerAngles.z);
                handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, handleModifierFinalTemp, handle.localEulerAngles.z);
            }

            if (currentSpeed > 15.0f)
            {
                //float handleModifiertemp = Mathf.Lerp(0.0f, 1.0f, currentSpeed / MaxSpeed);
                float handleModifiertemp;

                //float t = (currentSpeed - 15.0f) / (MaxSpeed - 15.0f);
                //handleModifiertemp = Mathf.Lerp(0.5f, 0.0f, currentSpeed / MaxSpeed);
                //handleModifiertemp = Mathf.Lerp(0.15f, 0.0f, t);

                handleModifiertemp = 0.0f;

                handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle * handleModifiertemp, handle.localEulerAngles.z);


            }

            //handle.localRotation = Quaternion.Euler(0, currentSteeringAngle, 0);
        }
    }

    // Dynamic Y min/max tracking
    private float observedYMin = float.MaxValue;
    private float observedYMax = float.MinValue;

    // Auto-calibration base references
    private float initialSuspensionY;
    private float initialSuspensionZ;
    private float initialLerpY;
    private float initialLerpZ;

    [Header("Suspension Offsets (auto-calibrated on Start)")]
    [SerializeField] private float suspensionYOffset = 0.0f;
    [SerializeField] private float suspensionZOffset = 0.0f;

    [Header("Wheel Collider Offsets (auto-calibrated)")]
    [SerializeField] private float wheelColliderZOffset = 0.0f;

    private float initialWheelColliderZ;
    private float initialWheelLerpZ;
    /*
        public void UpdateSwingarm()
        {

            // SwingArm START
            Vector3 currentDir = (rearAxle.position - swingarm.position).normalized;
            //float angle = Vector3.SignedAngle(initialDir, currentDir, swingarm.transform.right);
            float angle = Vector3.SignedAngle(initialDir, currentDir, -swingarm.transform.right);
            swingarm.transform.localRotation = baseRot * Quaternion.Euler(-angle, 0f, 0f);

            // SwingArm END

            // Get current direction in local space
            Vector3 currentLocalDir = swingarm.transform.InverseTransformDirection(rearAxle.position - swingarm.position).normalized;

            // Calculate angle in local space around the swingarm's right axis
            float angle = Vector3.SignedAngle(
                initialLocalDir,
                currentLocalDir,
                Vector3.right); // Using Vector3.right because we're in local space

            swingarm.transform.localRotation = baseRot * Quaternion.Euler(-angle, 0f, 0f);
        }
        */

    /*
    public void UpdateSwingarm()
    {
        // Get the current world space direction
        Vector3 currentDir = rearAxle.position - swingarm.position;

        // Project both directions onto the swingarm's right-plane
        Vector3 planeNormal = swingarm.transform.right;
        Vector3 initialProj = Vector3.ProjectOnPlane(initialDir, planeNormal);
        Vector3 currentProj = Vector3.ProjectOnPlane(currentDir, planeNormal);

        float angle = Vector3.SignedAngle(initialProj, currentProj, planeNormal);
        swingarm.transform.localRotation = baseRot * Quaternion.Euler(-angle, 0f, 0f);
    }
    */
    /*
    public void UpdateSwingarm()
    {
        // Calculate direction from swingarm to axle
        Vector3 direction = rearAxle.position - swingarm.position;

        // Convert to local space and calculate X-axis angle
        float angle = Mathf.Atan2(direction.z, direction.y) * Mathf.Rad2Deg;

        // Apply offset and set rotation
        swingarm.localEulerAngles = new Vector3(angle + angleOffset, 0f, 0f);
    }
    */
    /*
    public void UpdateSwingarm() // WORKING - flipped movement.
    {
        // 1. Temporarily store the swingarm's current UP direction (for stability)
        Vector3 savedUp = swingarm.up;

        // 2. Make the swingarm point at the axle (Z-axis faces target)
        swingarm.LookAt(rearAxle.position, -savedUp);

        // 3. Apply manual rotation offsets (fix Blender/Unity axis issues)
        swingarm.Rotate(rotationOffset);

        // 4. Lock unwanted rotations (optional, adjust based on your needs)
        swingarm.localEulerAngles = new Vector3(
            swingarm.localEulerAngles.x,  // Allow X-axis rotation (suspension)
            0f,                          // Lock Y-axis
            0f                           // Lock Z-axis
        );
    }
    */
    public void UpdateSwingarm()
    {
        // 1. Calculate direction from swingarm to axle (world space)
        Vector3 directionToAxle = rearAxle.position - swingarm.position;

        // 2. FLIP THE DIRECTION (reverses suspension movement)
        Vector3 flippedDirection = -directionToAxle;

        // 3. Make swingarm point in the OPPOSITE direction (flipped suspension)
        swingarm.LookAt(swingarm.position + flippedDirection, swingarm.up);

        // 4. Apply rotation offset (tune in Inspector)
        swingarm.Rotate(rotationOffset);

        // 5. Lock unwanted rotations (X-axis only)
        swingarm.localEulerAngles = new Vector3(
            swingarm.localEulerAngles.x,  // X-axis (suspension)
            0f,                           // Lock Y-axis
            0f                            // Lock Z-axis
        );
    }
    public void updateSuspension()
    {
        /*
        // SwingArm START
        Vector3 currentDir = (rearAxle.position - swingarm.transform.position).normalized;
        //float angle = Vector3.SignedAngle(initialDir, currentDir, swingarm.transform.right);
        float angle = Vector3.SignedAngle(initialDir, currentDir, -swingarm.transform.right);
        swingarm.transform.localRotation = baseRot * Quaternion.Euler(-angle, 0f, 0f);

        // SwingArm END
        */
        // Front Suspension START
        float Y = frontWheelTransform.transform.localPosition.y;

        // Update the min and max observed Y values
        if (Y < observedYMin) observedYMin = Y;
        if (Y > observedYMax) observedYMax = Y;

        // Prevent divide-by-zero
        float safeRange = Mathf.Max(0.001f, observedYMax - observedYMin);
        float Y_normalized = (Y - observedYMin) / safeRange;

        susDiffDistance = Y_normalized;

        // Interpolate and apply offsets
        float suspensionMovingYDiff = Mathf.Lerp(-0.616f, -0.516f, susDiffDistance) + suspensionYOffset;
        float suspensionMovingZDiff = Mathf.Lerp(0.308f, 0.263f, susDiffDistance) + suspensionZOffset;
        //float wheelColliderZDiff = Mathf.Lerp(0.335f, 0.290f, susDiffDistance);
        float wheelColliderZDiff = Mathf.Lerp(0.335f, 0.290f, susDiffDistance) + wheelColliderZOffset;

        if (MoveSuspension)
        {
            suspensionMoving.transform.localPosition = new Vector3(0.0f, suspensionMovingYDiff, suspensionMovingZDiff);
        }

        if (shiftCollider)
        {
            //frontWheel.transform.localPosition = new Vector3(0.0f, -0.682f, wheelColliderZDiff);
            frontWheel.transform.localPosition = new Vector3(0.0f, -0.682f, wheelColliderZDiff);
        }

        if (rotateWheel)
        {
            frontWheelTransform.localEulerAngles = new Vector3(
                handle.localEulerAngles.x,
                handle.localEulerAngles.y,
                frontWheel.transform.rotation.z
            );

            frontWheel.transform.localEulerAngles = new Vector3(
                frontWheel.transform.rotation.x,
                0.0f,
                frontWheel.transform.rotation.z * 0.5f
            );
        }
        // Front Suspension END
    }

    public void RecalibrateSuspension()
    {
        float currentY = frontWheelTransform.transform.localPosition.y;
        observedYMin = currentY;
        observedYMax = currentY + 0.001f;

        initialLerpY = Mathf.Lerp(-0.616f, -0.516f, 0.5f);
        initialLerpZ = Mathf.Lerp(0.308f, 0.263f, 0.5f);

        initialSuspensionY = suspensionMoving.transform.localPosition.y;
        initialSuspensionZ = suspensionMoving.transform.localPosition.z;

        suspensionYOffset = initialSuspensionY - initialLerpY;
        suspensionZOffset = initialSuspensionZ - initialLerpZ;

        initialWheelColliderZ = frontWheel.transform.localPosition.z;
        initialWheelLerpZ = Mathf.Lerp(0.335f, 0.290f, 0.5f);
        wheelColliderZOffset = initialWheelColliderZ - initialWheelLerpZ;

        Debug.Log("<color=cyan>Suspension and wheel collider recalibrated at runtime.</color>");
    }

    /*
    public void updateSuspension()
    {
        float Y = frontWheelTransform.transform.localPosition;
        float Y_min = -0.731f;
        float Y_max = -0.633f;
        // Normalize Y to range [0.0, 1.0]
        float Y_normalized = (Y - Y_min) / (Y_max - Y_min);

        susDiffDistance = Y_normalized;
        float suspensionMovingYDiff = Mathf.Lerp(-0.616f, -0.516f, susDiffDistance);
        //suspensionMoving.transform.localPosition = new Vector3(0.0f,suspensionMovingYDiff, -0.027f );
        float suspensionMovingZDiff = Mathf.Lerp(0.308f, 0.263f, susDiffDistance);
        float wheelColliderZDiff = Mathf.Lerp(0.335f, 0.290f, susDiffDistance);
        if (MoveSuspension == true)
        {
            suspensionMoving.transform.localPosition = new Vector3(0.0f, suspensionMovingYDiff, suspensionMovingZDiff); // Move Suspension Mesh on Y and Z axis
        }
        if (shiftCollider == true)
        {
            frontWheel.transform.localPosition = new Vector3(0.0f, -0.682f, wheelColliderZDiff); // Move Wheel Collider on Z axis    
            //frontWheelTransform.position = new Vector3(handle.position.x, frontWheel.transform.position.y, wheelColliderZDiff);
            //frontWheelTransform.localPosition = new Vector3(0.0f, frontWheel.transform.position.y, wheelColliderZDiff);
            //frontWheelTransform.position = new Vector3(frontWheel.transform.position.x, frontWheel.transform.position.y, frontWheelTransform.position.z); // Move Wheel Collider on Z axis

            //frontWheelTransform.localPosition = new Vector3(0.0f, -0.682f, wheelColliderZDiff); // Match the Y value
        }

        float wheelMeshRotVal = currentSteeringAngle * handleModifier;
        if (rotateWheel == true)
        {
            frontWheelTransform.localEulerAngles = new Vector3(handle.localEulerAngles.x, handle.localEulerAngles.y, frontWheel.transform.rotation.z);
            frontWheel.transform.localEulerAngles = new Vector3(frontWheel.transform.rotation.x, 0.0f, frontWheel.transform.rotation.z * 0.5f);
        }



    }
    */
    void LeanOnTurn()
    {
        Vector3 currentRot = transform.rotation.eulerAngles;
        if (currentSpeed < 0.5f)
        {
            //float LeanSpeedtemp = 0.05f;
            float LeanSpeedtemp = LeanSpeed;
            float maxLeanAngleTemp = 3.0f;
            targetLeanAngle = maxLeanAngleTemp * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeedtemp * Time.deltaTime);
        }
        if (currentSpeed >= 0.5f && currentSpeed < 5.0f)
        {
            //float LeanSpeedtemp = 0.155f;
            float LeanSpeedtemp = LeanSpeed;
            float maxLeanAngleTemp = 30.0f;
            targetLeanAngle = maxLeanAngleTemp * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeedtemp * Time.deltaTime);
        }
        if (currentSpeed >= 5.0f && currentSpeed < 15.0f)
        {
            float t = (currentSpeed - 5.0f) / (15.0f - 5.0f);
            //float LeanSpeedtemp = Mathf.Lerp(0.155f, LeanSpeed, currentSpeed / 15.0f);
            float LeanSpeedtemp = LeanSpeed;
            float maxLeanAngleTemp = Mathf.Lerp(30.0f, maxLeanAngle, currentSpeed / 15.0f);

            targetLeanAngle = maxLeanAngleTemp * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeedtemp * Time.deltaTime);
        }

        if (currentSpeed >= 15.0f)
        {
            targetLeanAngle = maxLeanAngle * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeed * Time.deltaTime);
        }
        if (rotateBike == true)
        {
            if (TouchingWater == false)
            {
                if (validRotation == true)
                {
                    transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
                }

            }



            //transform.Rotate(0.0f, 0.0f, currentLeanAngle);

            /*
            // leanRatio can be -1.0f to 1.0f
            // amount to rotate = current rotation to targetLean(currentLeanAngle) according to how much you have already rotated/ maxAmount to rotate to
            float targetRotation = Mathf.Lerp(currentLeanAngle, -currentLeanAngle, targetLeanAngle);
            //float targetRotation = Mathf.Lerp(currentLeanAngle, -currentLeanAngle, transform.eulerAngles.z / maxLeanAngle);
            //transform.Rotate(0.0f, 0.0f, targetRotation * currentLeanAngle / maxLeanAngle);

            //Vector3 rotationChange = new Vector3(currentRot.x, currentRot.y, currentRot.z + targetRotation);
            //transform.eulerAngles = rotationChange;

            transform.Rotate(0.0f, 0.0f, targetRotation);

            */


        }
        currentLean = Mathf.Abs(currentLeanAngle);
        //LeanRatio = -currentLeanAngle / maxLeanAngle;
    }
    void MoveCOG()
    {
        JointSpring backWheelSpringObject = backWheel.suspensionSpring;
        if (HandleCOGMovement == true && validRotation == true)
        {
            COGShiftValue = Mathf.Lerp(0.0f, COGShiftTarget, currentLean / maxLeanAngle);
            //float shiftValue = Mathf.Lerp(-COGShiftValue / COGShiftTarget, COGShiftValue / COGShiftTarget, (LeanRatio + 1) / 2);
            float shiftValue = Mathf.Lerp(-COGShiftValue / COGShiftTarget, COGShiftValue / COGShiftTarget, (LeanRatio + horizontalInput) / 2);
            //float wheelieValueMultiplier = Mathf.Lerp(1.0f, -0.5f, rb.centerOfMass.z / wheelieShiftTarget);


            RaycastHit WheelieHit;
            Ray downRay = new Ray(frontWheelTransform.transform.position, -Vector3.up);
            Physics.Raycast(downRay, out WheelieHit, 2.0f);
            Debug.DrawLine(frontWheelTransform.transform.position, WheelieHit.point, UnityEngine.Color.red);
            float wheelieHeightDiff = Mathf.Abs(WheelieHit.distance - 0.3f);
            groundHeight = wheelieHeightDiff;


            if (wheelieVal >= 0.05f)
            {
                wheelieSphereCollider.radius = 0.275f;
            }
            else
            {
                wheelieSphereCollider.radius = 0.265f;
            }

            //float COG_Offset_Y_Muliplier = Mathf.Lerp(1.0f, 0.0f, 1.0f - Mathf.Abs(LeanRatio));
            if (currentSpeed <= 45.0f)
            {

                if ((wheelieVal >= 0.15f && horizontalInput < 0.275f) && Mathf.Abs(LeanRatio) <= 0.2f)
                {
                    /*
                    backWheel.suspensionDistance = Mathf.Lerp(0.1f, 0.15f, wheelieVal);
                    backWheelSpringObject.damper = Mathf.Lerp(2500f, 5000f, wheelieVal);
                    //backWheel.suspensionDistance = Mathf.Lerp(0.1f, 0.2f, wheelieVal);
                    backWheel.suspensionSpring = backWheelSpringObject;

                    //Changed to Inspector Value Override NOT
                    wheelieShiftTarget = Mathf.Lerp(0.85f, 0.65f, wheelieVal);

                    //float wheelieValueMultiplier = Mathf.Lerp(1.0f, -0.5f, rb.centerOfMass.z / wheelieShiftTarget);
                    //float wheelieValueMultiplier = Mathf.Lerp(1.0f, 0.0f, rb.centerOfMass.z / wheelieShiftTarget);
                    float wheelieValueMultiplier = Mathf.Lerp(0.8f, 0.15f, wheelieHeightDiff / MaxWheelieDist);
                    //float wheelieValueMultiplier = Mathf.Lerp(0.85f, -0.5f, -rb.centerOfMass.z / wheelieShiftTarget);

                    //wheelieShiftTarget = Mathf.Lerp(1.1f, 0.8f, wheelieVal);
                    //float wheelieShiftValue = Mathf.Lerp(0.0f, wheelieShiftTarget * wheelieValueMultiplier, wheelieVal);
                    float wheelieShiftValue = Mathf.Lerp(0.0f, wheelieShiftTarget, 1.0f);

                    rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y + (0.05f * wheelieShiftValue) * wheelieValueMultiplier, COG_Offset.z - wheelieShiftValue);
                    //rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y, COG_Offset.z - wheelieShiftValue);
                    */
                    // Estimate current front wheel height
                    float currentFrontHeight = frontWheelTransform.position.y - WheelieHit.point.y;

                    // Compute normalized height ratio
                    float wheelieHeightRatio = Mathf.Clamp01(currentFrontHeight / MaxWheelieDist);

                    // Optional: Debug draw
                    Debug.DrawRay(frontWheelTransform.position, Vector3.down * currentFrontHeight, Color.cyan);

                    // Scale Y and Z shift based on how close you are to the max height
                    float wheelieShiftScale = 1.0f - wheelieHeightRatio; // Reduces as height increases

                    // Apply scaled center of mass shift
                    float finalYShift = Mathf.Lerp(0f, 0.05f * wheelieShiftTarget, wheelieShiftScale);
                    float finalZShift = Mathf.Lerp(0f, wheelieShiftTarget, wheelieShiftScale);

                    rb.centerOfMass = new Vector3(
                        shiftValue * COGShiftTarget,
                        COG_Offset.y + finalYShift,
                        COG_Offset.z - finalZShift
                    );
                }
                else
                {
                    if (groundHeight < 0.15f)
                    {
                        backWheel.suspensionDistance = 0.1f;
                        backWheelSpringObject.damper = 2500f;
                        backWheel.suspensionSpring = backWheelSpringObject;
                    }
                    if (groundHeight >= 0.15f)
                    {
                        backWheel.suspensionDistance = 0.15f;
                        backWheelSpringObject.damper = 4000f;
                        backWheel.suspensionSpring = backWheelSpringObject;
                    }
                    rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y, COG_Offset.z);
                }
            }
            else
            {
                backWheel.suspensionDistance = 0.1f;
                backWheelSpringObject.damper = 2500f;
                backWheel.suspensionSpring = backWheelSpringObject;
                rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y, COG_Offset.z);
            }
        }
        else
        {
            if (LeanRatio >= 0.1f && wheelieVal <= 0.1f)
            {
                WheelieColliderObject.SetActive(false);
            }
            else
            {
                if (validRotation == true)
                {
                    WheelieColliderObject.SetActive(true);
                }
            }
            rb.centerOfMass = new Vector3(COG_Offset.x, COG_Offset.y, COG_Offset.z);
        }



    }
    public void UpdateWheels()
    {
        //UpdateSingleWheel(frontWheel, frontWheelTransform);
        //Vector3 position;
        //Quaternion rotation;
        //frontWheel.GetWorldPose(out position, out rotation);
        //handle.localEulerAngles = new Vector3(handle.localEulerAngles.x * handleModifiertemp * handleModifier, currentSteeringAngle * handleModifier, handle.localEulerAngles.z * handleModifier);
        /*
        float wheelMeshRotVal = currentSteeringAngle * handleModifier;
        frontWheelTransform.localEulerAngles = new Vector3(frontWheel.transform.rotation.x, wheelMeshRotVal, frontWheel.transform.rotation.z);
        frontWheelTransform.localPosition = frontWheel.transform.position;
        */



        UpdateSingleWheel(backWheel, backWheelTransform);
    }
    /*
	private void EmitTrail() 
	{
		if (braking)
		{
			frontTrail.emitting = frontContact.GetCOntact();
			rearTrail.emitting = rearContact.GetCOntact();
		}
		else
		{			
			frontTrail.emitting = false;
			rearTrail.emitting = false;
		}		
	}
	*/
    void DebugInfo()
    {
        frontWheel.GetGroundHit(out WheelHit frontInfo);
        backWheel.GetGroundHit(out WheelHit backInfo);

        float backCoefficient = (backInfo.sidewaysSlip / backWheel.sidewaysFriction.extremumSlip);
        float frontCoefficient = (frontInfo.sidewaysSlip / frontWheel.sidewaysFriction.extremumSlip);

        //Debug.Log(" Back Coefficient = " + backCoefficient );
        //Debug.Log(" Front Coefficient = " + frontCoefficient);	
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);
        wheelTransform.rotation = rotation;
        wheelTransform.position = position;
    }

    void Speed_O_Meter()
    {
        currentSpeed = rb.linearVelocity.magnitude;
        currentKMHSpeed = currentSpeed * 3.6f;
        LeanRatio = currentLean / maxLeanAngle;
        currentCOG = rb.centerOfMass;

    }
    float FrontWheelFrictionMultiplierLean = 1.0f;
    float frontStiffnessMultiplier = 1.0f;
    float frictionCounter;
    void ChangeFrictionMultiplierStiffness()
    {

        if (susDiffDistance <= 0.45f && Mathf.Abs(horizontalInput) <= 0.35f)
        {
            FrontWheelFrictionMultiplierLean = 0.5f;
            frictionCounter = 0.5f;
        }
        if (susDiffDistance > 0.45f || Mathf.Abs(horizontalInput) > 0.35f)
        {

            FrontWheelFrictionMultiplierLean = Mathf.Lerp(0.65f, 1.0f, frictionCounter);
            if (FrontWheelFrictionMultiplierLean < 0.99f && frictionCounter < 0.99f)
            {
                frictionCounter += 0.01f;
            }
            else
            {
                frictionCounter = 1.0f;
            }

        }
        else
        {
            if (frictionCounter != 1.0f)
            {
                FrontWheelFrictionMultiplierLean = Mathf.Lerp(0.65f, 1.0f, frictionCounter);
                if (FrontWheelFrictionMultiplierLean < 0.99f && frictionCounter < 0.995f)
                {
                    frictionCounter += 0.025f;
                }
                else
                {
                    frictionCounter = 1.0f;
                }
            }
            else
            {
                FrontWheelFrictionMultiplierLean = 1.0f;
            }
        }
    }
    float frontStiffnessOverride = 1.0f;
    void ModifyFriction()
    {
        WheelFrictionCurve frontWheelfcSFOriginal;
        WheelFrictionCurve frontWheelfcSFChange;
        frontWheelfcSFOriginal = frontWheel.sidewaysFriction;
        frontWheelfcSFChange = frontWheel.sidewaysFriction;

        if (onlyFrontTouching == true)
        {
            frontStiffnessOverride = 0.25f;
        }
        if (onlyFrontTouching == false)
        {
            frontStiffnessOverride = 1.0f;
        }
        frontWheelfcSFOriginalStiffness = frontWheelfcSFOriginal.stiffness;
        if (modifyFrictionON)
        {
            WheelFrictionCurve frontWheelfcFF;
            WheelFrictionCurve frontWheelfcSF;

            WheelFrictionCurve backWheelfcFF;
            WheelFrictionCurve backWheelfcSF;
            frontWheelfcFF = frontWheel.forwardFriction;
            frontWheelfcSF = frontWheel.sidewaysFriction;

            backWheelfcFF = backWheel.forwardFriction;
            backWheelfcSF = backWheel.sidewaysFriction;


            /*
            if (Mathf.Abs(LeanRatio) >= 0.2f && Mathf.Abs(horizontalInput) <= 0.35f)
            {
                FrontWheelFrictionMultiplierLean = 0.45f;
            }
            if (Mathf.Abs(LeanRatio) >= 0.2f && Mathf.Abs(horizontalInput) > 0.35f)
            {
                FrontWheelFrictionMultiplierLean = 0.75f;
            }
            else
            {
                FrontWheelFrictionMultiplierLean = 1.0f;
            }
            */
            // Friction Changes
            /*
            if (isAccelerating == true) // is Accelerating
            {
                if (currentSpeed > 0 && currentSpeed <= 10) // 0-10 FwF Modifier 
                {
                    frontWheelfcFF.stiffness = 2.5f;
                    frontWheelfcSF.stiffness = 3.35f;
                    backWheelfcFF.stiffness = 2.5f;
                    backWheelfcSF.stiffness = 15.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 10 && currentSpeed < 30) // 10-30 FwF modifier
                {
                    frontWheelfcFF.stiffness = 2.5f;
                    frontWheelfcSF.stiffness = 3.65f;
                    backWheelfcFF.stiffness = 2.9f;
                    backWheelfcSF.stiffness = 25.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 30 && currentSpeed < 50) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 2.65f;
                    frontWheelfcSF.stiffness = 4.8f;
                    backWheelfcFF.stiffness = 4.0f;
                    backWheelfcSF.stiffness = 38.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 50 && currentSpeed < 70) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 3.2f;
                    frontWheelfcSF.stiffness = 5.5f;
                    backWheelfcFF.stiffness = 4.7f;
                    backWheelfcSF.stiffness = 45.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 70 && currentSpeed < 90) // 70-90 FwF modifier
                {
                    frontWheelfcFF.stiffness = 4.5f;
                    frontWheelfcSF.stiffness = 6.5f;
                    backWheelfcFF.stiffness = 5.5f;
                    backWheelfcSF.stiffness = 60.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else
                {
                    frontWheelfcFF.stiffness = 3.1f;
                    frontWheelfcSF.stiffness = 4.5f;
                    backWheelfcFF.stiffness = 4.0f;
                    backWheelfcSF.stiffness = 35.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
            }
            else
            */


            {


                //frontWheelfcFF.extremumSlip = Mathf.Lerp(1.75f, 1.95f, Mathf.Abs(LeanRatio));
                //frontWheelfcSF.extremumSlip = Mathf.Lerp(1.65f, 1.85f, Mathf.Abs(LeanRatio));
                //backWheelfcSF.extremumSlip = Mathf.Lerp(1.35f, 1.15f, Mathf.Abs(LeanRatio));
                //backWheelfcFF.extremumSlip = Mathf.Lerp(1.2f, 1.35f, Mathf.Abs(LeanRatio));
                if (currentSpeed >= 0 && currentSpeed <= 10) // 0-10 FwF Modifier 
                {
                    frontWheelfcFF.stiffness = 4.5f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    frontWheelfcSF.stiffness = 6.5f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    backWheelfcFF.stiffness = 4.7f;
                    backWheelfcSF.stiffness = 15.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 10 && currentSpeed <= 30) // 10-30 FwF modifier
                {

                    frontWheelfcFF.stiffness = 6.2f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    //frontWheelfcSF.stiffness = 10.5f;
                    frontWheelfcSF.stiffness = 9.5f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    //backWheelfcFF.stiffness = 3.3f;
                    backWheelfcFF.stiffness = 8.3f;
                    backWheelfcSF.stiffness = 55.0f;


                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 30 && currentSpeed <= 50) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 10.8f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    //frontWheelfcSF.stiffness = 13.0f;
                    frontWheelfcSF.stiffness = 12.5f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    //backWheelfcFF.stiffness = 3.4f;
                    backWheelfcFF.stiffness = 13.5f;
                    //backWheelfcSF.stiffness = 36.0f;
                    backWheelfcSF.stiffness = 85.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 50 && currentSpeed <= 70) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 10.4f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    //frontWheelfcSF.stiffness = 14.2f;
                    frontWheelfcSF.stiffness = 14.5f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    //backWheelfcFF.stiffness = 4.2f;
                    backWheelfcFF.stiffness = 15.5f;
                    //backWheelfcSF.stiffness = 47.0f;
                    //backWheelfcSF.stiffness = 58.0f;
                    backWheelfcSF.stiffness = 90.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 70 && currentSpeed <= 90) // 70-90 FwF modifier
                {
                    frontWheelfcFF.stiffness = 12.1f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    frontWheelfcSF.stiffness = 22.0f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    backWheelfcFF.stiffness = 16.5f;
                    backWheelfcSF.stiffness = 98.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else
                {
                    frontWheelfcFF.stiffness = 10.5f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    frontWheelfcSF.stiffness = 13.2f * FrontWheelFrictionMultiplierLean * frontStiffnessMultiplier;
                    backWheelfcFF.stiffness = 10.5f;
                    backWheelfcSF.stiffness = 45.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }

            }


        }

    }
    void UpdateNeedle()
    {
        ClusterdesiredPosition = ClusterstartPosition - ClusterendPosition;
        float temp = currentRPM / maxRPM;
        ClusterNeedle.transform.localEulerAngles = new Vector3(ClusterNeedle.transform.localEulerAngles.x, ClusterNeedle.transform.localEulerAngles.y, ClusterstartPosition - temp * ClusterdesiredPosition);
    }
    void CheckSlip()
    {
        frontWheel.GetGroundHit(out WheelHit frontInfo);
        backWheel.GetGroundHit(out WheelHit backInfo);

        float frontWheelForwardSlipTemp = frontInfo.forwardSlip;
        frontWheelForwardSlip = frontWheelForwardSlipTemp;
        float frontWheelSidewaysSlipTemp = frontInfo.sidewaysSlip;
        frontWheelSidewaysSlip = frontWheelSidewaysSlipTemp;
        float backWheelForwardSlipTemp = backInfo.forwardSlip;
        backWheelForwardSlip = backWheelForwardSlipTemp;
        float backWheelSidewaysSlipTemp = backInfo.sidewaysSlip;
        backWheelSidewaysSlip = backWheelSidewaysSlipTemp;
        //frontWheelForwardSlip = frontWheelForwardSlipTemp;
        //backCoefficient = (backInfo.sidewaysSlip / backWheel.sidewaysFriction.extremumSlip);
        //frontCoefficient = (frontInfo.sidewaysSlip / frontWheel.sidewaysFriction.extremumSlip);

        float lastSlipFF = frontWheelForwardSlip;
        float lastSlipFS = frontWheelSidewaysSlip;
        float lastSlipBF = backWheelForwardSlip;
        float lastSlipBS = backWheelForwardSlip;
        /*
        if (frontWheelForwardSlip > lastSlipFF)
        {
            maxSlipFF = frontWheelForwardSlip;
        }
        if (frontWheelSidewaysSlip > lastSlipFS)
        {
            maxSlipFS = frontWheelSidewaysSlip;
        }
        if (backWheelForwardSlip > lastSlipBF)
        {
            maxSlipBF = backWheelForwardSlip;
        }
        if (backWheelSidewaysSlip > lastSlipBS)
        {
            maxSlipBS = backWheelSidewaysSlip;
        }
        */
        ReadSlipMax(lastSlipFF, lastSlipFS, lastSlipBF, lastSlipBS);
    }
    void ReadSlipMax(float lastSlipFF, float lastSlipFS, float lastSlipBF, float lastSlipBS)
    {
        if (frontWheelForwardSlip > maxSlipFF)
        {
            maxSlipFF = frontWheelForwardSlip;
        }
        if (frontWheelSidewaysSlip > maxSlipFS)
        {
            maxSlipFS = frontWheelSidewaysSlip;
        }
        if (backWheelForwardSlip > maxSlipBF)
        {
            maxSlipBF = backWheelForwardSlip;
        }
        if (backWheelSidewaysSlip > maxSlipBS)
        {
            maxSlipBS = backWheelSidewaysSlip;
        }
    }


    public void turnOffTailLight()
    {
        emissiveMaterial.DisableKeyword("_EMISSION");
        TailLightON = false;
    }
    public void turnOnTailLight()
    {
        emissiveMaterial.EnableKeyword("_EMISSION");
        TailLightON = true;
    }



    private void OnTriggerEnter(Collider waterCheckRigidBody)
    {
        if (waterCheckRigidBody.CompareTag("Water Surface"))
        {
            Debug.Log("Entered water surface");
            TouchingWater = true;
            GetComponent<Bouyancy>().enabled = true;
        }
    }

    private void OnTriggerExit(Collider waterCheckRigidBody)
    {
        if (waterCheckRigidBody.CompareTag("Water Surface"))
        {
            Debug.Log("Exited water surface");
            TouchingWater = false;
            GetComponent<Bouyancy>().enabled = false;
        }
    }

    void CheckWheelHit()
    {
        frontWheel.GetGroundHit(out WheelHit frontInfo);
        backWheel.GetGroundHit(out WheelHit backInfo);
        if (frontWheel.isGrounded == true)
        {
            frontWheelHit = true;
        }
        else
        {
            frontWheelHit = false;
        }
        if (backWheel.isGrounded == true)
        {
            backWheelHit = true;
        }
        else
        {
            backWheelHit = false;
        }
        if (frontWheelHit == true && backWheelHit == true)
        {
            isMidAir = false;
        }
        if (frontWheelHit == false && backWheelHit == false)
        {
            isMidAir = true;
        }
        if (frontWheelHit == true && backWheelHit == false)
        {
            isMidAir = false;
            onlyFrontTouching = true;
            //rb.centerOfMass = new Vector3(COG_Offset.x, COG_Offset.y - 0.1f, COG_Offset.z - 0.25f);
        }
        else
        {
            if (frontWheelHit == false && backWheelHit == true)
            {
                isMidAir = false;
            }

            onlyFrontTouching = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 10f)
        {
            rb.angularDamping = 3f; // Temporarily dampen spin
            StartCoroutine(ResetAngularDrag());
        }
    }

    IEnumerator ResetAngularDrag()
    {
        yield return new WaitForSeconds(0.5f);
        rb.angularDamping = 0.05f; // Restore original value
    }


    void CheckCollisions() // Dependant on CheckWheelHit() ^
    {
        if (isResetting == true && groundHeight < 0.5f)
        {
            isResetting = false;
        }

        BodyCollided = BodyCollisionColliderObject.GetComponent<CapsuleCollisionDetection>().isColliding;
        if (TouchingWater == false && isResetting == false)
        {
            if ((BodyCollided == true && isHIGHGForce) && frontWheelHit == true && backWheelHit == true) // Grounded and collided
            {
                validRotation = false;
                CollidedBody = true;

                rb.linearDamping = 0.25f;
                rb.angularDamping = 0.5f;
            }
            if (BodyCollided == false && frontWheelHit == true && backWheelHit == true) // [NORMAL] grounded and not collided
            {

                if (CollidedBody == false && groundHeight < 0.5f)
                {
                    validRotation = true;
                }
                else
                {
                    validRotation = false;
                }


                rb.linearDamping = 0.005f;
                rb.angularDamping = 0.1f;
            }
            if (BodyCollided == false && frontWheelHit == false && backWheelHit == true) // Wheelie
            {
                //validRotation = true;
                if (CollidedBody == false)
                {
                    validRotation = true;
                }
                else
                {
                    validRotation = false;
                }


                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.4f;
            }
            if (BodyCollided == true && frontWheelHit == false && backWheelHit == true) // FrontWheel not grounded and collided
            {
                validRotation = false;
                CollidedBody = true;
                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.1f;
            }
            if (BodyCollided == true && frontWheelHit == false && backWheelHit == false) // mid air not grounded and collided
            {
                validRotation = false;
                CollidedBody = true;
                rb.linearDamping = 0.05f;
                //rb.angularDamping = 1.5f;
                rb.angularDamping = 0.5f;
            }
            if (BodyCollided == false && frontWheelHit == false && backWheelHit == false) // mid air not grounded and not collided
            {
                if (CollidedBody == false && groundHeight < 0.5f)
                {
                    validRotation = true;
                }
                else if (CollidedBody == false && groundHeight > 0.5f)
                {
                    if (groundHeight < 1.5f)
                    {
                        validRotation = true;
                    }
                    else
                    {
                        validRotation = false;
                    }

                }
                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.45f;
            }
        }
    }

    float blurAmount = 0.0f;
    float BlurCounter = 0.0f;
    void MotionBlur()
    {
        float currentblurAmount = FullscreenMotionBlurMaterial.GetFloat("_GodRaysStrength");

        if (isHIGHGForce == true && BlurCounter <= 0.005f)
        {
            //FullscreenMotionBlurMaterial.SetFloat("_GodRaysStrength", 0.65f);
            //currentblurAmount = 0.8f;
            //blurAmount = Mathf.Lerp(currentblurAmount, 0.8f, Time.deltaTime * 8.0f);
            blurAmount = 0.8f;
            BlurCounter = 0.8f;
            //FullscreenMotionBlurMaterial.SetFloat("_GodRaysStrength", blurAmount);
        }
        if (isHIGHGForce == false && BlurCounter > 0.0f)
        {

            blurAmount = Mathf.Lerp(0.0f, 0.8f, BlurCounter);
            if (blurAmount > 0.0f) BlurCounter = BlurCounter - 0.001f;

        }
        else
        {
            blurAmount = 0.0f;
        }


        //float blurAmount = Mathf.Lerp(0.0f, 0.8f, currentSpeed / MaxSpeed);
        FullscreenMotionBlurMaterial.SetFloat("_GodRaysStrength", blurAmount);
    }

    /*
    float currentBlurAmount = 0.0f;
    void MotionBlur()
    {
        float targetBlurAmount = isHIGHGForce ? 0.8f : 0.0f;
        currentBlurAmount = Mathf.Lerp(currentBlurAmount, targetBlurAmount, Time.deltaTime * 2.0f); // Adjust the speed with the multiplier
        FullscreenMotionBlurMaterial.SetFloat("_GodRaysStrength", currentBlurAmount);
    }
    */
    void CheckRotationValidity()
    {
        if (validRotation == true)
        {

            rb.constraints = RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.None;
            //rb.centerOfMass = new Vector3(0.0f, 0.0f, 0.0f);
        }

        if (BodyCollided == true)
        {
            rb.constraints = RigidbodyConstraints.None;
        }
        if (isMidAir == true)
        {
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    void MoveRiderHead()
    {

        if (RiderHead != null && cameraMode == CameraMode.FirstPerson)
        {

            Vector3 currentPosition = RiderHead.transform.localPosition;

            //float targetX = Mathf.Clamp(-LeanRatio * RiderHeadMovementVal, -RiderHeadMovementVal, RiderHeadMovementVal);
            float targetX = RiderHeadMovementVal * (-currentLeanAngle / (maxLeanAngle * 0.75f));

            float targetY = OriginalY_Position - Mathf.Lerp(0.0f, 0.07f, Mathf.Abs(currentLeanAngle / maxLeanAngle));




            float smoothedX = Mathf.Lerp(currentPosition.x, targetX, Time.deltaTime * 4.5f);
            float smoothedY = Mathf.Lerp(currentPosition.y, targetY, Time.deltaTime * 3.5f);

            // Update the RiderHead's position
            RiderHead.transform.localPosition = new Vector3(smoothedX, smoothedY, currentPosition.z);
            //RiderHead.transform.localPosition = new Vector3(targetX * LeanRatio, currentPosition.y, currentPosition.z);

            //RiderHead.transform.localPosition = new Vector3(RiderHeadMovementVal * (-currentLeanAngle / (maxLeanAngle * 0.75f)), currentPosition.y - targetY, currentPosition.z);
        }
        if (RiderHead != null && cameraMode == CameraMode.ThirdPerson)
        {

            Vector3 currentPosition = RiderHead.transform.localPosition;

            //float targetX = Mathf.Clamp(-LeanRatio * RiderHeadMovementVal, -RiderHeadMovementVal, RiderHeadMovementVal);
            float targetX = 0.0f;

            float targetY = OriginalY_Position - Mathf.Lerp(0.0f, 0.07f, Mathf.Abs(currentLeanAngle / maxLeanAngle));




            float smoothedX = Mathf.Lerp(currentPosition.x, targetX, Time.deltaTime * 4.5f);
            float smoothedY = Mathf.Lerp(currentPosition.y, targetY, Time.deltaTime * 3.5f);

            // Update the RiderHead's position
            RiderHead.transform.localPosition = new Vector3(smoothedX, smoothedY, currentPosition.z);
            //RiderHead.transform.localPosition = new Vector3(targetX * LeanRatio, currentPosition.y, currentPosition.z);

            //RiderHead.transform.localPosition = new Vector3(RiderHeadMovementVal * (-currentLeanAngle / (maxLeanAngle * 0.75f)), currentPosition.y - targetY, currentPosition.z);
        }
    }
    void ModifySteeringLimit()
    {
        if (modifySteering == true)
        {
            if (horizontalInput >= 0.8f || horizontalInput <= -0.8f)
            {
                if (currentSpeed > 15.0f)
                {
                    SteeringMultiplier = Mathf.Lerp(1.0f, 0.8f, Mathf.Abs(horizontalInput));
                }
                else
                {
                    SteeringMultiplier = 1.0f;
                }

                //SteeringMultiplier = Mathf.InverseLerp(1.0f, 0.65f, Mathf.Abs(horizontalInput));

            }
            else
            {
                if (currentSpeed > 15.0f)
                {
                    if (Mathf.Abs(horizontalInput) <= 0.3f)
                    {
                        SteeringMultiplier = 0.75f;
                    }
                    else
                    {
                        SteeringMultiplier = 1.0f;
                    }
                }
                else
                {
                    SteeringMultiplier = 1.0f;
                }
            }
        }
    }

    float GForceMagnitudeMultiplierAmp;
    float GForceMagnitudeMultiplierFreq;
    void GForceValues()
    {
        CurrentGForce = GetComponent<GForceScript>().currentGForce;
        GForceMagnitudeMultiplierAmp = Mathf.Clamp(CurrentGForce.y / 2.0f, 0.9f, 1.35f);
        GForceMagnitudeMultiplierFreq = Mathf.Clamp(CurrentGForce.magnitude / 0.5f, 1.2f, 4.0f);
    }
    void HandleCamera()
    {
        GForceValues();
        if (cameraMode == CameraMode.FirstPerson)
        {
            //ThirdPersonCameraObject.SetActive(false);
            //FirstPersonCameraObject.SetActive(true);
            //float FPPCamRotate = FirstPersonCameraObject.transform.eulerAngles.y;
            //orbitalFollow.HorizontalAxis.Center = FPPCamRotate;

            if (currentSpeed >= 15.0f)
            {
                FPPCamNoiseObject.AmplitudeGain = GForceMagnitudeMultiplierAmp * Mathf.Lerp(0.006f, 0.1f, currentSpeed / MaxSpeed);
                FPPCamNoiseObject.FrequencyGain = GForceMagnitudeMultiplierFreq * Mathf.Lerp(0.75f, 1.65f, 0.65f + Mathf.Max(currentSpeed / (MaxSpeed - 15.0f), Mathf.Abs(currentLean / maxLeanAngle)));
                //FPPCameraCinemachineCamera.Lens.FieldOfView = Mathf.Lerp(64.0f, 73.0f, (currentSpeed - 15.0f) / (MaxSpeed - 15.0f));
            }
            else
            {
                //FPPCameraCinemachineCamera.Lens.FieldOfView = 64.0f;
                FPPCamNoiseObject.AmplitudeGain = 0.0f;
                FPPCamNoiseObject.FrequencyGain = 0.0f;
            }
        }
        if (cameraMode == CameraMode.ThirdPerson)
        {
            ///ThirdPersonCameraObject.SetActive(true);
            //FirstPersonCameraObject.SetActive(false);
            //orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
            //orbitalFollow = ThirdPersonCameraObject.GetComponent<CinemachineOrbitalFollow>();

            if (orbitalFollow != null)
            {
                if (currentSpeed > 12.0f)
                {
                    orbitalFollow.HorizontalAxis.Recentering.Wait = 0.15f;
                    orbitalFollow.HorizontalAxis.Recentering.Time = 0.15f;
                }
                if (currentSpeed <= 12.0f)
                {
                    orbitalFollow.HorizontalAxis.Recentering.Wait = 4.5f;
                    orbitalFollow.HorizontalAxis.Recentering.Time = 2.5f;
                }
            }
            if (currentSpeed >= 15.0f)
            {
                TPPCamNoiseObject.AmplitudeGain = GForceMagnitudeMultiplierAmp * Mathf.Lerp(0.05f, 0.15f, currentSpeed / MaxSpeed);
                TPPCamNoiseObject.FrequencyGain = GForceMagnitudeMultiplierFreq * Mathf.Lerp(4.0f, 8.0f, 4.0f + Mathf.Max(currentSpeed / (MaxSpeed - 15.0f), Mathf.Abs(currentLean / maxLeanAngle)));

            }
            else
            {

                TPPCamNoiseObject.AmplitudeGain = 0.0f;
                TPPCamNoiseObject.FrequencyGain = 0.0f;
            }
        }

    }
    void SeatShift()
    {
        //float SeatShiftValueY = SeatTargetInitialPosition.y + 0.2f + UnityEngine.Random.Range(-0.2f, 0.2f);
        float SeatShiftValueY = SeatTargetInitialPosition.y;
        float targetX = SeatShiftAmount * (-currentLeanAngle / (maxLeanAngle));
        float smoothedX = Mathf.Lerp(SeatTargetObject.transform.localPosition.x, targetX, Time.deltaTime * 4.5f);

        //SeatTargetObject.transform.localPosition = new Vector3(0.0f, Mathf.Lerp(SeatTargetObject.transform.localPosition.y, SeatShiftValue, Time.deltaTime * 5.0f), SeatTargetObject.transform.localPosition.z);
        SeatTargetObject.transform.localPosition = new Vector3(smoothedX, Mathf.Lerp(SeatTargetObject.transform.localPosition.y, SeatShiftValueY, Time.deltaTime * 5.0f), SeatTargetObject.transform.localPosition.z);
    }


}
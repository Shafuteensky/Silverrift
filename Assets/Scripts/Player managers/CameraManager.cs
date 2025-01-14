using Unity.VisualScripting;
using UnityEngine;

public class CameraManager : InitializableMonoBehaviour
{
    InputManager inputManager;

    [Header ("Base")]
    public Transform targetTransform; // Object camera will follow
    public Transform cameraPivot; // Object camera uses to pivot
    public Vector3 defaultPivotLocPos = new Vector3(0.45f, 1.45f, 0);
    public Transform cameraTransform; // Transform of the actual camera object in scene
    public LayerMask collisionLayers; // Objects camera collide with
    private float defaultPosition; // Position camera backs to

    [Header("Collision")]
    public float cameraCollisionOffset = 0.2f; // How much camera will jump off of colliding objects
    public float minCollisionOffset = 0.2f;
    public float cameraCollisionRadius = 0.1f;

    [Header("Speeds")]
    private Vector3 cameraFollowVelocity = Vector3.zero;
    private Vector3 cameraVectorPosition;
    public float cameraFollowSpeed = 0.2f;
    public float cameraLookSpeed = 0.15f;
    public float cameraPivotSpeed = 0.075f;
    public float camLookSmoothTime = 25;

    [Header("Angles")]
    public float lookAngle; // Camera looking up and down
    public float pivotAngle; // Camera look left and right
    public float minPivotAngle = -70;
    public float maxPivotAngle = 70;

    private Quaternion lastRotation;

    private GameObject player;
    private bool isRotationAllowed = true;

    private void Awake()
    {
        ScriptTools.RegisterScript(this);

        cameraTransform = Camera.main.transform;
        defaultPosition = cameraTransform.localPosition.z;
        cameraPivot = GameObject.Find("Camera Pivot").transform;
        collisionLayers = LayerMask.GetMask("Player", "Default");

        player = GameObject.Find("Player");

        lastRotation = this.transform.rotation;
    }

    private ServiceLocator serviceLocator;

    private void Start()
    {
        serviceLocator = ServiceLocator.Instance;

        inputManager = serviceLocator.inputManager;

        if (AreDependenciesInitialized(inputManager))
            Initialize();

        targetTransform = GameObject.Find("Player").transform;
    }

    private void LateUpdate()
    {
        if (!IsInitialized()) return;

        HandleAllCameraMovement();
    }

    // =========================================

    public void HandleAllCameraMovement()
    {
        FollowTarget();
        //
        RotateCamera();
        HandleCameraCollisions();
    }    

    private void FollowTarget()
    {
        Vector3 targetPosition = Vector3.SmoothDamp
            (transform.position, targetTransform.position, ref cameraFollowVelocity, cameraFollowSpeed);
        transform.position = targetPosition;    
    }

    private void RotateCamera()
    {
        if (!isRotationAllowed)
            return; 

        Vector3 rotation;
        Quaternion targetRotation;

        lookAngle = lookAngle + (inputManager.cameraInputX * cameraLookSpeed);
        pivotAngle = pivotAngle - (inputManager.cameraInputY * cameraPivotSpeed);
        //lookAngle = Mathf.Lerp(lookAngle, lookAngle + (inputManager.cameraInputX * cameraLookSpeed), camLookSmoothTime * Time.deltaTime);
        //pivotAngle = Mathf.Lerp(pivotAngle, pivotAngle - (inputManager.cameraInputY * cameraPivotSpeed), camLookSmoothTime * Time.deltaTime);
        pivotAngle = Mathf.Clamp(pivotAngle, minPivotAngle, maxPivotAngle);

        rotation = Vector3.zero;
        rotation.y = lookAngle;
        targetRotation = Quaternion.Euler(rotation);
        transform.rotation = targetRotation;

        rotation = Vector3.zero;
        rotation.x = pivotAngle;
        targetRotation = Quaternion.Euler(rotation);
        cameraPivot.localRotation = targetRotation;
    }

    public void HandleCameraCollisions()
    {
        float targetPosition = defaultPosition;
        RaycastHit hit;
        Vector3 direction = cameraTransform.position - cameraPivot.position;
        direction.Normalize();

        if (Physics.SphereCast
            (cameraPivot.transform.position, cameraCollisionRadius, direction, out hit, Mathf.Abs(targetPosition), collisionLayers))
        {
            float distance = Vector3.Distance(cameraPivot.position, hit.point);
            targetPosition =- (distance - cameraCollisionOffset);
        }

        if (Mathf.Abs(targetPosition) < minCollisionOffset)
        {
            targetPosition =- minCollisionOffset;
        }

        cameraVectorPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, 0.2f);
        cameraTransform.localPosition = cameraVectorPosition; 
    }

    public void FacePlayer()
    {
        lastRotation = this.transform.rotation;
        isRotationAllowed = false;
        Quaternion playerRotation = player.transform.rotation;
        Vector3 vec = player.transform.eulerAngles + new Vector3(0, 180, 0);
        vec = new Vector3(0, vec.y, 0);
        this.transform.eulerAngles = vec; //Vector3.Lerp(this.transform.eulerAngles, vec, Time.deltaTime * 0.1f);
    } 

    public void RestorePosition()
    {
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, lastRotation, Time.deltaTime * 5f);
        isRotationAllowed = true;
    }

    public void MovePivotByFactor(float horizontal, float vertical)
    {
        if (horizontal == 0)
            horizontal = defaultPivotLocPos.x;
        if (vertical >= 0)
            vertical = defaultPivotLocPos.z;
        Vector3 pivotPosition = cameraPivot.transform.localPosition;
        cameraPivot.transform.localPosition = Vector3.Lerp(pivotPosition, 
            new Vector3(horizontal, defaultPivotLocPos.y, vertical), 0.07f);
    }
}

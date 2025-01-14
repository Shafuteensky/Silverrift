using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static Cinemachine.DocumentationSortingAttribute;

public class PlayerLocomotion : InitializableMonoBehaviour
{
    [Header("Movement flags")]
    public bool isGrounded = true;
    public bool isInSprinting = false;
    public bool isInWalking = false;
    public bool isJumping = false;
    public bool isInteracting = false; // ������ ��������������� � ������ � ������������ �������� �����������
    public bool isStopping = false; // ������ ���������
    public bool isAlreadyStopped = true;
    public bool wasMovingFast = false;

    [Header("Movement speeds")]
    [SerializeField] private float climbingSmoothness = 0.3f;

    [Header("Falling")]
    [SerializeField] private float inAirTime = 0; // ����� � �������
    private float rayCastHeightOffset = 0.5f; // ����� ������������ ����� ����
    private float timeBeforeFallAnim = 0.2f;
    private LayerMask groundLayer;
    private float maxRayDistance; // ����. ��������� ���� ������ ����� ��� ������ ��� ����������� � ��� ����������� ������������ �� �������

    [Header("Jumping")]
    [SerializeField] private float lastJumpTime = 0; // ����� ����� ���������� ������
    [SerializeField] private float gravityIntensity = -15;
    [SerializeField] private float sphereRadius = 0.1f;
    private float airJumpTimeWindow = 0.5f; // ����� ����� ������ �������, � ������� �������� ��� ����� ��������
    private float minJumpInterval = 0.95f; // �������� ����� ������������ ��������

    //[SerializeField] private float force = 0.1f;
    [SerializeField] private float springForceConstant;
    [SerializeField] private float springDumpingConstant;

    private InputManager inputManager;
    private PlayerManager playerManager;
    private AnimatorManager animatorManager;

    GameObject player;
    Rigidbody playerRigidbody;
    Vector3 moveDirection;
    Transform cameraObject;

    // ============================================

    private void Awake()
    {
        ScriptTools.RegisterScript(this);

        //player = GameObject.Find("Player");
        //playerRigidbody = player.GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        cameraObject = Camera.main.transform;
    }
    
    private ServiceLocator serviceLocator;
    private void Start()
    {
        groundLayer = LayerMask.GetMask("Default");
        serviceLocator = ServiceLocator.Instance;

        playerManager = serviceLocator.playerManager;
        animatorManager = serviceLocator.animatorManager;

        StartCoroutine(GetWaitManagers());
    }

    private void FixedUpdate()
    {
        HandleAllMovement();
    }

    private void LateUpdate()
    {
        isInteracting = playerManager.animator.GetBool("isInteracting");
        isJumping = playerManager.animator.GetBool("isJumping");

        playerManager.animator.SetBool("isGrounded", isGrounded);
    }
    // =============== ������� ===============

    private IEnumerator GetWaitManagers()
    {
        while (serviceLocator.inputManager == null || serviceLocator.player == null)
            yield return null;

        inputManager = serviceLocator.inputManager;

        player = serviceLocator.player;
        playerRigidbody = player.GetComponent<Rigidbody>();

        if (AreDependenciesInitialized(playerManager, animatorManager, inputManager)
            && AreDependenciesInitialized(player))
            Initialize();
    }

    // =============== ����������� ������������ ===============

    public void HandleAllMovement()
    {
        if (!IsInitialized(true)) return;

        HandleFallingAndLanding();
        if (isStopping) return;
        if (isInteracting) return;
        if (isJumping) return;
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        if (isJumping || !isGrounded) return;
        moveDirection = cameraObject.forward * inputManager.verticalInput;
        moveDirection = moveDirection + cameraObject.right * inputManager.horizontalInput;
        moveDirection.Normalize();
        moveDirection.y = 0;

        if (isInSprinting)
        {
            wasMovingFast = true;
            moveDirection = moveDirection.normalized * playerManager.playerStats.sprintingSpeed;
        }
        else
        {
            wasMovingFast = false;
            if (!isInWalking & inputManager.moveAmount >= 0.5f)
            {
                moveDirection = moveDirection.normalized * playerManager.playerStats.runningSpeed;
            }
            else
            {
                moveDirection = moveDirection.normalized * playerManager.playerStats.walkingSpeed;
            }
        }

        animatorManager.animator.SetBool("wasMovingFast", wasMovingFast);
        if (inputManager.moveAmount > 0.1f)
            animatorManager.animator.SetBool("isMoving", true);
        else
            animatorManager.animator.SetBool("isMoving", false);

        //if (inputManager.moveAmount > 0.1f)
        //{
        //    isAlreadyStopped = false;
        //}
        //if (!isAlreadyStopped && inputManager.moveAmount < 0.1f)
        //{
        //    Debug.Log("!");
        //    if (wasMovingFast)
        //        animatorManager.PlayTargetAnimation("Run to stop", false);
        //    else
        //        animatorManager.PlayTargetAnimation("Walk to stop", false);
        //    isAlreadyStopped = true;
        //}
        //animatorManager.animator.SetBool("isAlreadyStopped", isAlreadyStopped);

        Vector3 movementVelocity = new Vector3(moveDirection.x, playerRigidbody.linearVelocity.y, moveDirection.z);
        playerRigidbody.linearVelocity = movementVelocity;
    }

    // ������� ��������� ������ ����� ���
    private void HandleRotation()
    {
        Vector3 targetDirection = Vector3.zero;

        targetDirection = cameraObject.forward * inputManager.verticalInput;
        targetDirection = targetDirection + cameraObject.right * inputManager.horizontalInput;
        targetDirection.Normalize();
        targetDirection.y = 0;

        if (targetDirection == Vector3.zero)
            targetDirection = player.transform.forward;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(player.transform.rotation, targetRotation, 
            playerManager.playerStats.rotationSpeed * Time.deltaTime);

        player.transform.rotation = playerRotation;
    }

    // �������, ����������� � ����������� �������� ������������� ����������
    private void HandleFallingAndLanding() 
    {
        // �������� ����� ���������
        Vector3 playerPosition = playerRigidbody.position; 
        Debug.DrawLine(playerPosition - new Vector3(0.5f, 0, 0), playerPosition + new Vector3(0.5f, 0, 0), Color.white);

        // ����� ������ ����
        Vector3 rayCastOrigin = playerPosition + new Vector3(0, 
            GameObject.Find("Player").GetComponent<CapsuleCollider>().center.y, 0); 
        Debug.DrawLine(rayCastOrigin - new Vector3(0.5f, 0, 0), rayCastOrigin + new Vector3(0.5f, 0, 0), Color.green);

        // ����� ��������� ���������� ����
        maxRayDistance = Mathf.Abs(playerPosition.y - rayCastOrigin.y) + rayCastHeightOffset; 
        Debug.DrawLine(rayCastOrigin - new Vector3(0.5f, 0, 0) - new Vector3(0, maxRayDistance, 0), 
            rayCastOrigin + new Vector3(0.5f, 0, 0) - new Vector3(0, maxRayDistance, 0), Color.red);

        // ��������� X � Z ��� ����� �����������
        Vector3 targetPosition;
        targetPosition = playerPosition; 

        // �������� � ��������� �������
        if (!isGrounded && !isJumping) 
        {
            inAirTime = inAirTime + Time.deltaTime;

            if (!isInteracting && (inAirTime > timeBeforeFallAnim))
                animatorManager.PlayTargetAnimation("Falling", true);
        }

        // ���� �� ������ � ���������� �����
        if (Physics.SphereCast(rayCastOrigin, sphereRadius, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer)) 
        {
            // ������� �����������
            if (!isGrounded && isInteracting) // ���� ��� �� �����������
            {
                playerRigidbody.linearVelocity = playerRigidbody.linearVelocity/10; // ����� �������� �� ������� ��� �����
                inAirTime = 0;

                animatorManager.PlayTargetAnimation("Landing", true);
                // �������� �����������
                isGrounded = true;
            }

            playerRigidbody.useGravity = false;

            // ����� ��������� �����
            targetPosition.y = hit.point.y; 
            Debug.DrawLine(targetPosition - new Vector3(0.5f, 0, 0), targetPosition + new Vector3(0.5f, 0, 0), Color.blue);

            // ---------- ������������ ��������� ----------
            // �������� ������� ��������� �� ������ �� �����
            if (isGrounded && !isJumping) // �������� �� ����� � �� �������
            {
                // �������������
                if (isInteracting || inputManager.moveAmount > 0)
                {
                    playerRigidbody.position = Vector3.Lerp(playerRigidbody.position, targetPosition, 
                        Time.deltaTime / climbingSmoothness); // ������� ��������� ������ ����������
                }
                // ����� �� �����
                else
                {
                    playerRigidbody.position = targetPosition; // "����������������������" ��������� ������ ����������
                    playerRigidbody.linearVelocity = new Vector3(playerRigidbody.linearVelocity.x, 0, 
                        playerRigidbody.linearVelocity.z); // ������� ������ ������������� ���������� �� ����� (��-�� RigidBody)
                }
            }
        }
        // ��� ����� � ���������� �������
        else
        {
            isGrounded = false;
            playerRigidbody.useGravity = true;
        }

        // ������ ������� � ���������� ������
        if (lastJumpTime < 60) // ����� ��� ������������
            lastJumpTime = lastJumpTime + Time.deltaTime;
    }

    // ========== �������� � ���������� � InputManager ==========

    // ������
    public void HandleJump()
    {
        // ����� �������� ����:
        if ((isGrounded // ��� ������ ���� �����
            && lastJumpTime > minJumpInterval) // � � ���������� ������ ������ ���������� �������
            // ���
            || (!isGrounded // ��� ������ ��� �����
            && inAirTime < airJumpTimeWindow // �� ��������� ���� ������ � ������� ��� �������
            && lastJumpTime > minJumpInterval)) // � � ���������� ������ ������ ���������� �������
        {  
            lastJumpTime = 0;

        animatorManager.animator.SetBool("isJumping", true);
        animatorManager.PlayTargetAnimation("Jumping", false);
        playerRigidbody.useGravity = true;

        float jumpingVelocity = Mathf.Sqrt(-2 * gravityIntensity * playerManager.playerStats.jumpHeight);
        Vector3 playerVelocity = moveDirection;
        playerVelocity.y = jumpingVelocity;
        playerRigidbody.linearVelocity = playerVelocity;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FPSController : MonoBehaviour
{
    CharacterController characterController;

    #region Components
    [Header("Components")]
        
        public Camera playerCamera;

        public Transform playerHandheld;

        public PlayerInput input { get; private set; }

        public PlayerAnimationScript playerAnimationScript { get; private set; }
        public Animator animator { get; private set; }
    #endregion


    #region Movement Variables

    private Vector2 moveInput;

        private float initialJumpVelocity;
        private float maxJumpHeight = 1f;
        private float maxJumpTime = 0.5f;
        private bool readyToJump;

        private bool canMove = true;
        private bool isWalking = true;
        private bool isGrounded;

        private Vector3 velocity;

    [Header("Movement Variables")]

        public float walkSpeed = 3f;
        public float runSpeed = 5f;

        public float gravity = -13f;
        public float airControl = 1f;
    #endregion

    #region Camera Variables

        private Vector2 lookInput;
        private float lookXLimit = 70f;
        private float cameraZTilt = 0;
        private float cameraFOV;
        private float rotationX;

    [Header("Camera Variables")]

        public float lookSpeed = 2f;
        public float cameraTiltMultiplier = -2;
        public float cameraTiltSmoothTime = 25f;
        public float defaultCameraFOV = 80;
        public float cameraFovMultiplier = 5;

        #region additional Camera Variables        
        [Header("Bobbing")]

        public bool bobOffset = true;
        public bool bobSway = true;
        public float speedCurve;
        float curveSin { get => Mathf.Sin(speedCurve); }
        float curveCos { get => Mathf.Cos(speedCurve); }

        public Vector3 travelLimit = Vector3.one * 0.025f;
        public Vector3 bobLimit = Vector3.one * 0.01f;
        Vector3 bobPosition;

        public float bobExaggeration;

        [Header("Bob Rotation")]

        public Vector3 multiplier;
        Vector3 bobEulerRotation;

    [Header("Sway")]

        public bool sway = true;
        public bool swayRotation = true;
        public float step = 0.03f;
        public float maxStepDistance  = 0.16f;
        public float rotationStep = 4f;
        public float maxRotationStep = 5f;
        Vector3 swayPos;
        Vector3 swayEulerRot;

    #endregion
    #endregion

    #region Ground Check Variables
    [Header("Ground Check Variables")]
        public LayerMask groundMask;
    #endregion

    private void Awake()
    {
        cameraFOV = defaultCameraFOV;
        if (bobExaggeration == 0) bobExaggeration = walkSpeed * 2;
        multiplier = new Vector3(1, 2, 1);

        animator = GetComponentInChildren<Animator>();
    }

    // Start is called before the first frame update
    void Start() { 
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        float timeToApex = maxJumpTime / 2;
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;

        input = GetComponent<PlayerInput>();
        input.input.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.input.Movement.canceled += ctx => moveInput = ctx.ReadValue<Vector2>();

        input.input.Block.canceled += ctx => animator.SetBool("Blocking", false);
        input.input.Ready.performed += ctx => animator.SetBool("Ready", !animator.GetBool("Ready"));

        input.input.Attack.performed += ctx => animator.SetBool("Attacking", true);
        input.input.Attack.canceled += ctx => animator.SetBool("Attacking", false);

        input.input.Block.performed += ctx => animator.SetBool("Blocking", true);
        input.input.Block.canceled += ctx => animator.SetBool("Blocking", false);

        //input.input.Heal.performed += ctx => animator.SetBool("Healing", true);
        input.input.Heal.performed += ctx => animator.SetTrigger("HealTrigger");
        //input.input.Heal.canceled += ctx => animator.SetBool("Healing", false);

        input.input.Inventory.performed += ctx => animator.SetBool("AccessingInventory", !animator.GetBool("AccessingInventory"));
    }

    // Update is called once per frame
    void Update()
    {

        checkGround();

        movePlayer();

        controlCamera();

        applySway();

        if (Input.GetButtonDown("Jump") && readyToJump == true)
        {
            jump();
        }
    }

    private void checkGround()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, characterController.height * 0.5f + 0.2f, groundMask);
        if (isGrounded)
        {
            velocity.y = -2f;
            readyToJump = true;
        } else
        {
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }
    }

    private void movePlayer()
    {

        //moveInput = input.input.Movement.ReadValue<Vector2>();
        //moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        moveInput = moveInput.normalized;
        //Debug.Log("Input Input: " + input.input.Movement.ReadValue<Vector2>());
        Debug.Log("Move Input: " + moveInput);

        // Adjust camera tilt based on sprinting
        float targetCameraTiltMultiplier = isWalking ? cameraTiltMultiplier : cameraTiltMultiplier - 5;

        // Calculate target camera tilt based on input
        float targetCameraZTilt = moveInput.x * targetCameraTiltMultiplier;

        // Interpolate camera tilt smoothly
        //cameraZTilt = Mathf.Lerp(0, moveInput.x * cameraTiltMultiplier, cameraTiltSmoothTime);
        cameraZTilt = Mathf.Lerp(cameraZTilt, targetCameraZTilt, cameraTiltSmoothTime * Time.deltaTime);

        // Calculate camera field of view
        cameraFOV = Mathf.Lerp(cameraFOV, defaultCameraFOV + (moveInput.y * cameraFovMultiplier), cameraTiltSmoothTime * Time.deltaTime);

        // Calculate movement direction
        Vector3 direction = transform.forward * moveInput.y + transform.right * moveInput.x;

        // Move character controller
        characterController.Move(direction * (isWalking ? walkSpeed : runSpeed) * Time.deltaTime);
    }

    private void jump()
    {
        if (isGrounded)
        {
            readyToJump = false;
            isGrounded = false;
            velocity.y = 25f;
            Console.WriteLine("Jumped: " + initialJumpVelocity);
        }

    }

    private void controlCamera()
    {
        lookInput = 
        new Vector2(input.input.Horizontal.ReadValue<float>() * lookSpeed * Time.deltaTime, input.input.Vertical.ReadValue<float>() * lookSpeed * Time.deltaTime);

        rotationX -= lookInput.y;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, cameraZTilt);
        playerCamera.fieldOfView = cameraFOV;
        transform.Rotate(Vector3.up * lookInput.x);
    }

    private void applySway()
    {
        if (sway)
        {
            Sway();
            playerHandheld.localPosition = Vector3.Lerp(playerHandheld.localPosition, swayPos, Time.deltaTime * 2f);
        }

        if (swayRotation)
        {
            SwayRotation();
            playerHandheld.localRotation = Quaternion.Slerp(playerHandheld.localRotation,Quaternion.Euler(swayEulerRot),Time.deltaTime * 2f);
        }

        if (bobOffset)
        {
            BobOffset();
            playerHandheld.localPosition = Vector3.Lerp(playerHandheld.localPosition, swayPos + bobPosition, Time.deltaTime * 10f);
        }

        if (bobSway)
        {
            BobRotation();
            playerHandheld.localRotation = Quaternion.Slerp(playerHandheld.localRotation, Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation), Time.deltaTime * 12f);
        }
    }

    private void Sway()
    {
        if (sway == false)
        {
            swayPos = Vector3.zero; return;
        }

        Vector3 invertLook = lookInput * -step;

        invertLook.x  = Mathf.Clamp(invertLook.x,-maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = invertLook;
    }

    private void SwayRotation()
    {
        if (swayRotation == false)
        {
            swayEulerRot = Vector3.zero; return;
        }

        Vector2 invertLook = lookInput * -step;

        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);

        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    void BobOffset()
    {
        speedCurve += Time.deltaTime * (isGrounded ? (moveInput.x + moveInput.y) * bobExaggeration : 1f) + 0.01f;

        bobPosition.x = (curveCos * bobLimit.x * (isGrounded ? 1 : 0)) - (moveInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (moveInput.y * travelLimit.y);
        bobPosition.z = -(moveInput.y * travelLimit.z);
    }

    void BobRotation()
    {
        bobEulerRotation.x = (moveInput != Vector2.zero ? multiplier.x * (Mathf.Sin(2 * speedCurve)) : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));
        bobEulerRotation.y = (moveInput != Vector2.zero ? multiplier.y * curveCos : 0);
        bobEulerRotation.z = (moveInput != Vector2.zero ? multiplier.z * curveCos * moveInput.x : 0);
    }

}

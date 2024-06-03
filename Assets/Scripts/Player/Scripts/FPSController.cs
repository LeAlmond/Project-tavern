using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class FPSController : MonoBehaviour
{
    Rigidbody playerRigidbody;

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
        public float maxJumpHeight = 50f;
        private float maxJumpTime = 1f;
        private bool readyToJump;

        public float dashStrength = 50f;

        private bool isWalking = true;
        private bool isGrounded;

        private Vector3 velocity;

        private Vector3 direction;

    [Header("Movement Variables")]

        public float walkSpeed = 6f;
        public float runSpeed = 10f;

        public float airControl = 1f;
    #endregion

    #region Camera Variables

        private Vector2 lookInput;
        private float lookXLimit = 70f;
        private float cameraZTilt = 0;
        private float cameraFOV;
        private float rotationX;
        private float rotationY;

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

    #region Lockon Variables
    [Header("Lock On Variables")]
        private Boolean lockedOn = false;
        private Transform lockOnTarget;
        public float lockonTime = 1f;
    #endregion

    private void Awake()
    {
        cameraFOV = defaultCameraFOV;
        if (bobExaggeration == 0) bobExaggeration = walkSpeed * 2;
        multiplier = new Vector3(1, 2, 1);

        animator = playerCamera.GetComponentInChildren<Animator>();
    }

    // Start is called before the first frame update
    void Start() {

        playerRigidbody = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        float timeToApex = maxJumpTime / 2;
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;

        
        input.input.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.input.Movement.canceled += ctx => moveInput = ctx.ReadValue<Vector2>();

        input.input.Sprint.performed += ctx => isWalking = false;
        input.input.Sprint.canceled += ctx => isWalking = true;

        input.input.Jump.performed += ctx => jump();

        input.input.Dash.performed += ctx => dash();

        input.input.Block.canceled += ctx => animator.SetBool("Blocking", false);
        input.input.Ready.performed += ctx => animator.SetBool("Ready", !animator.GetBool("Ready"));

        input.input.Attack.performed += ctx => animator.SetBool("Attacking", true);
        input.input.Attack.canceled += ctx => animator.SetBool("Attacking", false);

        input.input.Block.performed += ctx => animator.SetBool("Blocking", true);
        input.input.Block.canceled += ctx => animator.SetBool("Blocking", false);

        input.input.Ledger.performed += ctx => ledgerAction();

        input.input.Compass.performed += ctx => compassAction();

        input.input.Map.performed += ctx => mapAction();

        input.input.Settings.performed += ctx => settingsAction();

        input.input.LockOn.performed += ctx => LockOn();

        //input.input.Heal.performed += ctx => animator.SetBool("Healing", true);
        input.input.Heal.performed += ctx => animator.SetTrigger("HealTrigger");
        //input.input.Heal.canceled += ctx => animator.SetBool("Healing", false);

        input.input.Inventory.performed += ctx => animator.SetBool("AccessingInventory", !animator.GetBool("AccessingInventory"));
    }

    private void togglemapCompass()
    {
        animator.SetBool("Ready", false);
        animator.SetBool("Ledger", false);
    }

    private void toggleActions()
    {
        animator.SetBool("Ready", false);
        animator.SetBool("Ledger", false);
        animator.SetBool("Map", false);
        animator.SetBool("Compass", false);
    }

    private void settingsAction()
    {
        toggleActions();
        animator.SetBool("Settings", !animator.GetBool("Settings"));
    }

    private void mapAction()
    {
        togglemapCompass();
        animator.SetBool("Map", !animator.GetBool("Map"));
    }

    private void compassAction()
    {
        togglemapCompass();
        animator.SetBool("Compass", !animator.GetBool("Compass"));
    }

    private void ledgerAction()
    {
        animator.SetBool("Ready", false);
        animator.SetBool("Map", false);
        animator.SetBool("Compass", false);
        animator.SetBool("Ledger", !animator.GetBool("Ledger"));
    }

    // Update is called once per frame
    void Update()
    {
        movePlayer();

        controlCamera();

        applySway();

        checkGround();
    }

    private void getMouseDirection()
    {
        Vector2 attackDirection = lookInput;
        if (Mathf.Abs(attackDirection.x)> Mathf.Abs(attackDirection.y))
        {
            //Debug.Log("Horizontal Swing");
            if (attackDirection.x > 0)
            {
                animator.SetInteger("Attack Direction", 2);
            }
            else if (attackDirection.x < 0)
            {
                animator.SetInteger("Attack Direction", -1);
            }
           
        }
        else if (Mathf.Abs(attackDirection.x) < Mathf.Abs(attackDirection.y))
        {
            //Debug.Log("Vertical Swing");
            if (attackDirection.y > 0)
            {
                animator.SetInteger("Attack Direction", 1);
            }
            else if (attackDirection.y < 0)
            {
                animator.SetInteger("Attack Direction", 0);
            }
        }
        
    }

    private void movePlayer()
    {

        direction = transform.forward * moveInput.y + transform.right * moveInput.x;

        // Adjust camera tilt based on sprinting
        float targetCameraTiltMultiplier = isWalking ? cameraTiltMultiplier : cameraTiltMultiplier - 2;

        // Calculate target camera tilt based on input
        float targetCameraZTilt = moveInput.x * targetCameraTiltMultiplier;

        // Interpolate camera tilt smoothly
        //cameraZTilt = Mathf.Lerp(0, moveInput.x * cameraTiltMultiplier, cameraTiltSmoothTime);
        cameraZTilt = Mathf.Lerp(cameraZTilt, targetCameraZTilt, cameraTiltSmoothTime * Time.deltaTime);

        // Calculate camera field of view
        cameraFOV = Mathf.Lerp(cameraFOV, defaultCameraFOV + (moveInput.y * cameraFovMultiplier), cameraTiltSmoothTime * Time.deltaTime);

        // Calculate movement direction
        

        if (isGrounded)
        {
            playerRigidbody.AddForce(direction.x * (isWalking ? walkSpeed : runSpeed), playerRigidbody.velocity.y, direction.z * (isWalking ? walkSpeed : runSpeed));
        }else
        {
            playerRigidbody.AddForce(direction.x * airControl, 0, direction.z * airControl);
        }
    }

    private void checkGround()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 2 * 0.5f + 0.2f);
        GetComponent<Rigidbody>().drag = isGrounded ? 2 : 0;

        Vector3 flatVel = new Vector3(GetComponent<Rigidbody>().velocity.x, 0f, GetComponent<Rigidbody>().velocity.z);

        if (isGrounded)
        {
            if (isWalking)
            {
                if (flatVel.magnitude > walkSpeed)
                {
                    GetComponent<Rigidbody>().velocity = new Vector3(flatVel.normalized.x * walkSpeed, GetComponent<Rigidbody>().velocity.y, flatVel.normalized.z * walkSpeed);
                }
            }
            else
            {
                if (flatVel.magnitude > runSpeed)
                {
                    GetComponent<Rigidbody>().velocity = new Vector3(flatVel.normalized.x * runSpeed, GetComponent<Rigidbody>().velocity.y, flatVel.normalized.z * runSpeed);
                }
            }
        }
        
        
        
    }
    
    private void jump()
    {
        
        if (isGrounded)
        {
            GetComponent<Rigidbody>().velocity = new Vector3(GetComponent<Rigidbody>().velocity.x, 0f, GetComponent<Rigidbody>().velocity.z);
            GetComponent<Rigidbody>().AddForce(transform.up * initialJumpVelocity, ForceMode.Impulse);
        }

    }

    private void dash()
    {
        direction = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (isGrounded)
        {
            GetComponent<Rigidbody>().AddForce(direction.x * dashStrength, 100f, direction.z * dashStrength, ForceMode.Acceleration);
        }
       

    }

    private void controlCamera()
    {
        lookInput = 
        new Vector2(input.input.Horizontal.ReadValue<float>() * Time.deltaTime * lookSpeed, input.input.Vertical.ReadValue<float>() * Time.deltaTime * lookSpeed);
        
        rotationX -= lookInput.y;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        rotationY += lookInput.x;

        getMouseDirection();

        Vector3 playerCameraTilted = new Vector3(rotationX, rotationY, cameraZTilt);

        if (lockedOn)
        {
            Vector3 lockOnTargetPosition = lockOnTarget.position - transform.position;
            lockOnTargetPosition.Normalize();
            lockOnTargetPosition.y = 0;
            Quaternion lockOnTargetRotation = Quaternion.LookRotation(lockOnTargetPosition);
            transform.rotation = Quaternion.Slerp(transform.rotation, lockOnTargetRotation, lockonTime);

            

            lockOnTargetPosition = lockOnTarget.position - playerCamera.transform.position;
            lockOnTargetPosition.Normalize();
            lockOnTargetRotation = Quaternion.LookRotation(lockOnTargetPosition);
            lockOnTargetRotation *= Quaternion.Euler(0, 0, cameraZTilt);
            playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, lockOnTargetRotation, lockonTime);


        } else
        {
            //transform.Rotate(Vector3.up * lookInput.x);
            transform.rotation = Quaternion.Euler(0, rotationY, 0);
            playerCamera.transform.localRotation = Quaternion.Euler(playerCameraTilted);
        }

        
        playerCamera.fieldOfView = cameraFOV;

       
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

    private void BobOffset()
    {
        speedCurve += Time.deltaTime * (isGrounded ? (moveInput.x + moveInput.y) * (isWalking ? bobExaggeration : (bobExaggeration + 2)) : 1f) + 0.01f;

        bobPosition.x = (curveCos * bobLimit.x * (isGrounded ? 1 : 0)) - (moveInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (moveInput.y * travelLimit.y);
        bobPosition.z = -(moveInput.y * travelLimit.z);
    }

    private void BobRotation()
    {
        bobEulerRotation.x = (moveInput != Vector2.zero ? multiplier.x * (Mathf.Sin(2 * speedCurve)) : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));
        bobEulerRotation.y = (moveInput != Vector2.zero ? multiplier.y * curveCos : 0);
        bobEulerRotation.z = (moveInput != Vector2.zero ? multiplier.z * curveCos * moveInput.x : 0);
    }

    private void LockOn()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 100.0f))
        {
            if (hit.transform.Find("LockOnTarget") != null)
            {
                Transform lockOnTargetTransform = hit.transform.Find("LockOnTarget");
                try
                {
                    lockOnTarget = lockOnTargetTransform;
                }
                catch (Exception e)
                {
                    lockOnTarget = hit.transform;
                    Debug.Log("Error: " + e);
                }

                lockedOn = !lockedOn;

            }
            else
            {
                lockedOn = false;
            }
        }
       
    }
}

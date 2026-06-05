using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryMenuPlayerPreviewManager : MonoBehaviour
{
    // This is placed on the Player Preview for the Inventory menu to see the player as we change equipment
    // -
    // PlayerEquipment takes care of the equipment spawning/deleting AND calls SetIdleType() for the animator

    public static InventoryMenuPlayerPreviewManager Instance { get; private set; }
    private Animator animator;
    public Transform R_Hand;
    public Transform L_Hand;
    public Animator bowAnimator;
    public PlayerCustomizationSockets customizationSockets;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float maxRotationAngle = 60f;
    [SerializeField] private float inputDeadzone = 0.1f;

    private InputAction rightStickNavigateAction;
    private Quaternion baseLocalRotation;
    private float currentYRotationOffset;

    void Awake()
    {
        Instance = this;
        customizationSockets = GetComponent<PlayerCustomizationSockets>();
        animator = GetComponentInChildren<Animator>();
        baseLocalRotation = transform.localRotation;
        currentYRotationOffset = 20f;
    }

    void OnEnable()
    {
        SmartSetIdleType();
        SetBowActive(Player.Instance._playerAnimation.bowAnimator.gameObject.activeSelf);
        rightStickNavigateAction = Player.Instance._playerUIInputManager.RightStickNavigateAction;
        currentYRotationOffset = 20f;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, currentYRotationOffset, 0f);
    }

    void Update()
    {
        RotatePreviewWithRightStick();
    }

    public void SmartSetIdleType()
    {
        if(Player.Instance._playerEquipment.equippedMainHandWeapon != null ||
           Player.Instance._playerEquipment.equippedOffHandWeapon != null)
            SetIdleType(Player.Instance._playerCombat.currentWeapon.weaponTemplate.weaponType);
        else
            SetIdleTypeForNoWeapon();
    }


    // 0 = No weapon, 1 = Single Sword, 2 = Double Sword, 3 = THS, 4 = Spear, 5 = Sword & Shield
    public void SetIdleType(WeaponData.WeaponType weaponType) 
    {
        float idleType;
        switch(weaponType)
        {
            case WeaponData.WeaponType.SingleHanded: idleType = 1f; break;
            case WeaponData.WeaponType.SecondWeapon: idleType = 2f; break;
            case WeaponData.WeaponType.TwoHanded:    idleType = 3f; break;
            case WeaponData.WeaponType.Spear:        idleType = 4f; break;
            // case WeaponData.WeaponType.SwordAndShield: idleType = 5f; break; // Need to implement shield

            default:                                 idleType = 0f; break;
        }

        animator.SetFloat("IdleType", idleType); 
    }

    public void SetIdleTypeForNoWeapon() => animator.SetFloat("IdleType", 0f);

    public void SetBowActive(bool active)
    {
        if (bowAnimator == null) return;

        bowAnimator.gameObject.SetActive(active);
    }

    private void RotatePreviewWithRightStick()
    {
        if (rightStickNavigateAction == null) return;

        float horizontalInput = rightStickNavigateAction.ReadValue<Vector2>().x;
        if (Mathf.Abs(horizontalInput) < inputDeadzone)
            return;

        currentYRotationOffset += horizontalInput * rotationSpeed * Time.unscaledDeltaTime;
        currentYRotationOffset = Mathf.Clamp(currentYRotationOffset, -maxRotationAngle, maxRotationAngle);
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, currentYRotationOffset, 0f);
    }
}

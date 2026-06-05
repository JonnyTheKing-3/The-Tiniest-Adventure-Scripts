using System;
using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class CamerasManager : MonoBehaviour
{
    public static CamerasManager Instance { get; private set; }

    public enum CameraStates { ThirdPerson = 0, LockOn = 1, FreeRoam = 2, Aim = 3 }
    public Camera mainCamera;
    public GameObject CameraHelpers;
    [Space]

    public CameraStates CameraState;
    [Space]

    public CinemachineFreeLook C_ThirdPerson;
    [Space]

    public CinemachineVirtualCamera VC_LockOn;
    public CameraLockOn c_LockOn;
    [Space]

    public CinemachineFreeLook C_Aim;

    public CinemachineVirtualCamera VC_FreeRoam;
    public float ClosestDialogeExtraDistance = 5;
    public float ClosestDialogeVerticalOffset = 0f;

    [HideInInspector] public CinemachineBrain cinemachineBrain;

    [HideInInspector] public CinemachineVirtualCameraBase _currentCam;
    void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        c_LockOn = VC_LockOn.GetComponent<CameraLockOn>();
        cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();

        _currentCam = C_ThirdPerson;
        ActivePriority = 20;
        InactivePriority = 10;
        C_ThirdPerson.Priority = ActivePriority;
        VC_LockOn.Priority = InactivePriority;
        VC_FreeRoam.Priority = InactivePriority;
        C_Aim.Priority = InactivePriority;
    }

    public void MakeCameraStartReady() // Called by StartMenuManager to set up proper camera position for Start Menu
    {
        cinemachineBrain.enabled = false;
        mainCamera.transform.position = GameManager.Instance.StartMenuCameraPosition;
        mainCamera.transform.rotation = Quaternion.Euler(GameManager.Instance.StartMenuCameraRotation);
    }
    public void MakeCameraLoadReady()  // Called by LoadGame()
    {
        cinemachineBrain.enabled = true;
    }



    int ActivePriority = 20, InactivePriority = 10;
    void GivePriorityTo(CinemachineVirtualCameraBase next)
    {
        if (_currentCam == next) return;

        next.Priority = ActivePriority;
        _currentCam.Priority = InactivePriority;
        _currentCam = next;
    }

    public void SwitchToThirdPerson()
    {
        if (_currentCam == VC_LockOn) SwitchOffLockOn();

        // Force new camera to start from current camera's pose so the blend between them is smooth
        C_ThirdPerson.ForceCameraPosition(_currentCam.State.FinalPosition, _currentCam.State.FinalOrientation);

        GivePriorityTo(C_ThirdPerson);
        CameraState = CameraStates.ThirdPerson;
    }

    public void SwitchToAim()
    {
        if (CameraState == CameraStates.Aim) return;

        // Force new camera to start from current camera's pose so the blend between them is smooth
        C_Aim.ForceCameraPosition(_currentCam.State.FinalPosition, _currentCam.State.FinalOrientation);

        GivePriorityTo(C_Aim);
        CameraState = CameraStates.Aim;
    }

    public void SwitchOffLockOn()
    {
        // for (int i = targetGroup.m_Targets.Length - 1; i >= 1; i--)
        //     targetGroup.RemoveMember(targetGroup.m_Targets[i].target);

        // targetGroup.enabled = false;
        c_LockOn.enabled = false;
    }

    public void SwitchToLockOnCombat(Transform target, float y, float p)
    {
        c_LockOn.enabled = true;
        c_LockOn.targetB = target;
        c_LockOn.currentYaw = y;
        c_LockOn.currentPitch = p;
        c_LockOn.snapThisFrame = true;

        GivePriorityTo(VC_LockOn);
        CameraState = CameraStates.LockOn;
    }

    public void SwitchToLockOnDialogueFromCurrentCamera(Transform target)
    {
        c_LockOn.enabled = true;
        c_LockOn.targetB = target;
        c_LockOn.SetOrbitFromCameraPose(_currentCam.State.FinalPosition);
        c_LockOn.snapThisFrame = true;

        GivePriorityTo(VC_LockOn);
        CameraState = CameraStates.LockOn;
    }

    public void SwitchToFreeRoamDialogueClosest(Transform target)
    {
        GetFreeRoamDialogueClosestPose(target, out Vector3 desiredPos, out Vector3 desiredRot);

        VC_FreeRoam.transform.SetPositionAndRotation(desiredPos, Quaternion.Euler(desiredRot));

        GivePriorityTo(VC_FreeRoam);
        CameraState = CameraStates.FreeRoam;
    }

    public void GetFreeRoamDialogueClosestPose(Transform target, out Vector3 desiredPos, out Vector3 desiredRot)
    {
        Vector3 A = target.position;
        Vector3 B = Player.Instance._playerLocomotion.transform.position;
        Vector3 mid = 0.5f * (A + B);

        Vector3 levelCamPos = new Vector3(transform.position.x, mid.y, transform.position.z);
        Vector3 dir = levelCamPos - mid;

        float dist = Vector3.Distance(mid, A);
        desiredPos = mid + (dir.normalized * (dist + ClosestDialogeExtraDistance));
        desiredPos += Vector3.up * ClosestDialogeVerticalOffset;
        desiredRot = Quaternion.LookRotation(mid - desiredPos, Vector3.up).eulerAngles;
    }

    public void SwitchToFreeRoam(Vector3 pos, Vector3 rot)
    {
        if (_currentCam == VC_LockOn) SwitchOffLockOn();

        VC_FreeRoam.transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
        GivePriorityTo(VC_FreeRoam);
        CameraState = CameraStates.FreeRoam;
    }
}

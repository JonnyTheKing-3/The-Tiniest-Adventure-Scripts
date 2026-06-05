using UnityEngine;

public class PlayerCustomizationSockets : MonoBehaviour
{
    public Transform headSocket;
    public Transform bodySocket;
    public Transform backSocket;

    public Transform GetSocket(CustomizationSlot slot)
    {
        switch (slot)
        {
            case CustomizationSlot.Head: return headSocket;
            case CustomizationSlot.Body: return bodySocket;
            case CustomizationSlot.Back: return backSocket;
            default: return null;
        }
    }
}

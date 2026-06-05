using UnityEngine;

public interface IApproachable : IInteractable
{
    public abstract void FadeApproachUI(float targetAlpha);
}

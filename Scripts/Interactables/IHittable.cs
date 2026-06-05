using System;
using UnityEngine;

public interface IHittable : IInteractable
{
   public abstract void GotHit(GameObject obj, HitData attackData, float AttackerAttackStat, Vector3 hitPoint, Vector3 hitNormal);
}

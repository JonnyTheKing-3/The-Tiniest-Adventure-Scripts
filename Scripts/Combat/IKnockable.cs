using System;
using UnityEngine;

public interface IKnockbackable
{
    void StartKnockback(Vector3 targetPos, float duration, HitData attackData, Transform attacker);
}

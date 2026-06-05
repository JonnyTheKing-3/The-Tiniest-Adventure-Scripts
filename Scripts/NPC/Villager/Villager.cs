using UnityEngine;

public class Villager : MonoBehaviour
{
    public enum VillagerState { Idle = 0, Walking = 1 }
    public VillagerState villagerState = VillagerState.Idle;
    [HideInInspector] public VillagerLocomotionFSM villagerLoco;
    [HideInInspector] public VillagerAnimation villagerAnim;

    void Awake()
    {
        villagerLoco = GetComponent<VillagerLocomotionFSM>();
        villagerAnim = GetComponentInChildren<VillagerAnimation>();
    }
}

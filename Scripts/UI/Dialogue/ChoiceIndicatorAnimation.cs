using DG.Tweening;
using UnityEngine;

public class ChoiceIndicatorAnimation : MonoBehaviour
{
    [SerializeField] private float moveDistance = 200f;
    [SerializeField] private float moveDuration = 1.5f;
    private Tween t;

    private void OnEnable()
    {
        PingPongPos();
    }

    public void PingPongPos() => t = transform.DOLocalMoveX(moveDistance, moveDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetRelative();

    private void OnDisable() => t.Kill();
}

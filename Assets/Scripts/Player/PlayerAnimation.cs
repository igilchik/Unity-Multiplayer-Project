using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetAnimationSpeed(float speed)
    {
        animator.SetFloat("Speed", speed);
    }
    void Update()
{
    animator.speed = 0.1f;
}
}

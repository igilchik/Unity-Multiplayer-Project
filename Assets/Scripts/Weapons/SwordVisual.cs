using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SwordVisual : MonoBehaviour
{
    [SerializeField] private Sword sword;
    private Animator animator;

    private const string ATTACK = "Attack";

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (sword == null)
            sword = GetComponentInParent<Sword>();
    }

    private void OnEnable()
    {
        if (sword != null)
            sword.OnSwordSwing += Sword_OnSwordSwing;
    }

    private void OnDisable()
    {
        if (sword != null)
            sword.OnSwordSwing -= Sword_OnSwordSwing;
    }

    private void Sword_OnSwordSwing(object sender, System.EventArgs e)
    {
        animator.SetTrigger(ATTACK);
    }

    public void PlayAttack()
    {
        animator.SetTrigger(ATTACK);
    }
}

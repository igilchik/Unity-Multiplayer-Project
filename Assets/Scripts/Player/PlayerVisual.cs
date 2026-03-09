using Unity.Netcode;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Player player;
    private NetworkObject netObj;

    private const string DIE = "IsDie";
    private const string IS_RUNNING = "IsRunning";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        player = GetComponentInParent<Player>();
        netObj = GetComponentInParent<NetworkObject>();
    }

    private void Update()
    {
        if (player == null) return;

        animator.SetBool(IS_RUNNING, !player.IsDead() && player.IsRunning());

        if (netObj != null && netObj.IsOwner && !player.IsDead())
            AdjustPlayerFacingDirectionLocal();
    }

    private void AdjustPlayerFacingDirectionLocal()
    {
        if (GameInput.Instance == null) return;

        Vector3 mousePos = GameInput.Instance.GetMousePosition();
        Vector3 playerPos = player.transform.position;

        spriteRenderer.flipX = mousePos.x < playerPos.x;
    }

    public void PlayDeath()
    {
        PlayDeathAnimation();
    }

    public void PlayDeathAnimation()
    {
        animator.SetTrigger(DIE);
    }
}

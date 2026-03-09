using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Health health;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private PlayerCombatNGO combat;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector2 _localMoveInput;     
    private Vector2 _serverMoveInput;  

    private float _sendTimer;
    private float _faceSendTimer;
    private const float FaceSendRate = 1f / 30f; 
    private const float SendRate = 1f / 20f; 

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<Health>();
        if (playerState == null) playerState = GetComponent<PlayerState>();
        if (combat == null) combat = GetComponent<PlayerCombatNGO>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb.simulated = IsServer;

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (!IsServer)
            rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (!IsSpawned) return;

        if (IsMatchEnded())
        {
            _localMoveInput = Vector2.zero;
        }

        if (health != null && health.IsDead)
        {
            _localMoveInput = Vector2.zero;
        }
        else
        {
            if (!IsMatchEnded())
                ReadInput();
            else
                _localMoveInput = Vector2.zero;
        }

        if (!IsMatchEnded() && playerState != null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 dir = (Vector2)(mouseWorld - transform.position);

                if (dir.sqrMagnitude > 0.0001f)
                    playerState.SubmitFacingServerRpc(dir.normalized);
            }
        }

        if (!IsMatchEnded() && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (combat != null)
                combat.RequestAttack();
        }

        if (IsServer)
        {
            _serverMoveInput = _localMoveInput.sqrMagnitude > 0.0001f ? _localMoveInput.normalized : Vector2.zero;
        }
        else
        {
            _sendTimer += Time.deltaTime;
            if (_sendTimer >= SendRate)
            {
                _sendTimer = 0f;
                SubmitMoveInputServerRpc(_localMoveInput);
            }
        }
    }

    private void LateUpdate()
    {
        if (!IsSpawned) return;
        if (playerState == null) return;
        if (spriteRenderer == null) return;

        Vector2 f = playerState.Facing.Value;
        if (f.sqrMagnitude < 0.0001f) return;

        spriteRenderer.flipX = f.x < 0f;
    }


    private void FixedUpdate()
    {
        if (!IsServer) return;
        if (!IsSpawned) return;

        if (IsMatchEnded())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (health != null && health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = _serverMoveInput * moveSpeed;
    }

    private void ReadInput()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y = -1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y = 1f;

        _localMoveInput = new Vector2(x, y).normalized;
    }

    [ServerRpc]
    private void SubmitMoveInputServerRpc(Vector2 move)
    {
        _serverMoveInput = move.sqrMagnitude > 0.0001f ? move.normalized : Vector2.zero;
    }

    private bool IsMatchEnded()
    {
        if (GameFreeze.MatchEnded) return true;
        if (MatchManagerNGO.Instance != null && MatchManagerNGO.Instance.MatchEnded.Value) return true;
        return false;
    }

    public bool IsDead() => health != null && health.IsDead;
    public bool IsRunning() => _localMoveInput.sqrMagnitude > 0.001f;
}

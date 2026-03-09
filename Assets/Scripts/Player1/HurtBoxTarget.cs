using UnityEngine;

public class HurtBoxTarget : MonoBehaviour
{
    public Health Health { get; private set; }

    private void Awake()
    {
        Health = GetComponentInParent<Health>();
    }
}

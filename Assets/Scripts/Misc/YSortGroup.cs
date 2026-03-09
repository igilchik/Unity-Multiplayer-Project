using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class YSort : MonoBehaviour
{
    public int orderOffset = 0;
    public int orderPerUnit = 100;

    SortingGroup group;

    void Awake()
    {
        group = GetComponent<SortingGroup>();
    }

    void LateUpdate()
    {
        group.sortingOrder = orderOffset - Mathf.RoundToInt(transform.position.y * orderPerUnit);
    }
}


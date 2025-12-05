using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    public GameObject itemPrefab;
    public Transform dropPoint;
    public float dropForce = 3f;

    public void Drop()
    {
        if (!itemPrefab)
        {
            return;
        }

        var t = dropPoint ? dropPoint : transform;
        var go = Instantiate(itemPrefab, t.position, Quaternion.identity);
        var rb = go.GetComponent<Rigidbody>();
        
        if (rb)
        {
            rb.AddForce(Vector3.up * dropForce, ForceMode.VelocityChange);
        }
    }
}

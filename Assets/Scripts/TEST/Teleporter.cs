using UnityEngine;
using static RootMotion.FinalIK.Grounding;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private Vector3 teleportPos;
    [SerializeField] private bool toPointer = true;
    [SerializeField] private GameObject pointer;
    [SerializeField] private bool correctY = true;

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name != "Player")
            return;

        Vector3 newPos = new Vector3();
        if (toPointer)
            newPos = pointer.transform.position;
        else
            newPos = teleportPos;

        GameObjectTools.AlignObjectToSurface(collider.gameObject, newPos, collider.gameObject.transform.rotation, correctY);
    }
}

using System.Numerics;
using UnityEngine;

public class SimpleRotation : MonoBehaviour
{
    [SerializeField] private int modifier;
    private int factor;
    [SerializeField] private bool isInRange;

    [SerializeField] private float check;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        modifier = 2;    
    }

    // Update is called once per frame
    void Update()
    {
        check = (Time.deltaTime % 2);
        if (isInRange) { factor = Random.Range(0, modifier); } else { factor = modifier; }
        transform.Rotate(UnityEngine.Vector3.one * Time.deltaTime * factor);
    }
}

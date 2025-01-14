using UnityEngine;

public class Creature
{
    private string name;
    private int health;
    private float movementSpeed;
    private float rotationSpeed;

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public int Health
    { 
        get { return health; } 
        set { health = value; } 
    }

    public float MovementSpeed
    {
        get { return movementSpeed; }
        set { movementSpeed = value; }
    }

    public float RotationSpeed
    {
        get { return rotationSpeed; }
        set { rotationSpeed = value; }
    }
}

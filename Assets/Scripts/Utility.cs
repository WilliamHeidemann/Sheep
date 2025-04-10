using UnityEngine;

public static class Utility
{
    public static Vector3 AddFlat(this Vector3 vector3, Vector2 vector2)
    {
        return new Vector3(vector3.x + vector2.x, vector3.y, vector3.z + vector2.y);
    }
}
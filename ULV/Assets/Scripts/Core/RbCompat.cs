using UnityEngine;

/// <summary>
/// En Unity 6 Rigidbody2D.velocity se llama linearVelocity.
/// Estas dos funciones funcionan en cualquier versión.
/// </summary>
public static class RbCompat
{
    public static Vector2 GetVelocity(this Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVelocity(this Rigidbody2D rb, Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}

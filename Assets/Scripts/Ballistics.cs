using UnityEngine;
using System.Collections;

public class Ballistics
{
    // https://en.wikipedia.org/wiki/Range_of_a_projectile
    // Unités = mètres, secondes, degrés.

    public static Vector2 GetPositionAtTime(float t, float v, float theta)
    {
        float x, y;

        x = 1f;
        y = 1f;

        return new Vector2(x, y);
    }

    public static float GetAngleToReachDistance(float dist, float speed)
    {
        // Returns an angle in degrees as Unity uses degrees.
        return Mathf.Rad2Deg * 0.5f * Mathf.Asin(dist * Physics.gravity.y / Mathf.Pow(speed, 2f));
    }

    public static float GetForceToReachDistance(float dist, float angle)
    {
        // Takes in an angle in degrees as Unity uses degrees.
        return Mathf.Sqrt(Mathf.Abs(dist * Physics.gravity.y / Mathf.Sin(2f * Mathf.Deg2Rad * angle)));
    }
}
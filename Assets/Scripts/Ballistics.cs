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

    public static float _GetSpeedToReachDistanceWithGivenAngle(float range_, float gravity_, float angle_, float launchHeight_)
    {
        // This takes the difference of floor level into account.
        // Gravity should always be a positive value.
        // The angle is relative to the horizon and is given in degrees (this function will convert it to radians).

        //angle_ = 45f;
        angle_ *= Mathf.Deg2Rad;
        float delta = launchHeight_ * launchHeight_ + range_ * range_; // This is the discriminant.
        // Usually, we check if the discriminant is greater, equal or less than zero. But as we add two square numbers, it can only be greater or equal to zero.

        Debug.Log("delta=" + delta);

        if (delta == 0)
        {
            float x, speed;
            x = 2f * gravity_ * launchHeight_ / 2f;
            Debug.Log("x=" + x);

            speed = Mathf.Sqrt(Mathf.Pow(Mathf.Sin(angle_), 2f) / x);
            return speed;
        }
        else
        {
            float x1, x2, speed;
            x1 = (-(2f * gravity_ * launchHeight_) - Mathf.Sqrt(delta)) / 2f;
            x2 = (-(2f * gravity_ * launchHeight_) + Mathf.Sqrt(delta)) / 2f;

            Debug.Log("x1=" + x1 + " x2=" + x2);
            if (x1 >= 0)
            {
                speed = Mathf.Sqrt(Mathf.Pow(Mathf.Sin(angle_), 2f) / x1);
                return speed;
            }
            else if (x2 >= 0)
            {
                speed = Mathf.Sqrt(Mathf.Pow(Mathf.Sin(angle_), 2f) / x2);
                return speed;
            }
            else
            {
                return -2f;
            }
        }
    }

    public static float GetSpeedToReachDistanceWithGivenAngle(float range_, float gravity_, float angle_, float launchHeight_)
    {
        // http://physics.stackexchange.com/questions/27992/solving-for-initial-velocity-required-to-launch-a-projectile-to-a-given-destinat

        angle_ += 360f;
        angle_ %= 90f;
        angle_ *= Mathf.Deg2Rad;

        float v;

        v = (1f / Mathf.Cos(angle_)) * Mathf.Sqrt((0.5f * gravity_ * range_ * range_) / (range_ * Mathf.Tan(angle_) + launchHeight_));

        return v;
    }
}
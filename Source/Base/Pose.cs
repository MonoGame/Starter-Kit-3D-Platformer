// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;

/// <summary>
/// Describes a 3D pose with translation, rotation, and scale.
/// </summary>
public struct Pose
{
    public static Pose Identity => new Pose
    {
        Translation = Vector3.Zero,
        Rotation = Quaternion.Identity,
        Scale = Vector3.One
    };

    public Vector3 Translation;
    public Quaternion Rotation;
    public Vector3 Scale;

    public static Pose FromMatrix(Matrix transform)
    {
        Pose result;
        transform.Decompose(out result.Scale, out result.Rotation, out result.Translation);
        return result;
    }

    public static Pose Lerp(Pose pose1, Pose pose2, float amount)
    {
        Pose result;
        Lerp(ref pose1, ref pose2, amount, out result);
        return result;
    }

    public static void Lerp(ref Pose pose1, ref Pose pose2, float amount, out Pose result)
    {
        Vector3.Lerp(ref pose1.Scale, ref pose2.Scale, amount, out result.Scale);
        Quaternion.Lerp(ref pose1.Rotation, ref pose2.Rotation, amount, out result.Rotation);
        Vector3.Lerp(ref pose1.Translation, ref pose2.Translation, amount, out result.Translation);
    }

    public static Pose Slerp(Pose from, Pose to, float amount)
    {
        Pose result;
        Slerp(ref from, ref to, amount, out result);
        return result;
    }

    public static void Slerp(ref Pose from, ref Pose to, float amount, out Pose result)
    {
        Vector3.SmoothStep(ref from.Scale, ref to.Scale, amount, out result.Scale);
        Quaternion.Slerp(ref from.Rotation, ref to.Rotation, amount, out result.Rotation);
        Vector3.SmoothStep(ref from.Translation, ref to.Translation, amount, out result.Translation);
    }

    public static void Add(ref Pose pose1, ref Pose pose2, out Pose result)
    {
        Vector3.Add(ref pose1.Scale, ref pose2.Scale, out result.Scale);
        Quaternion.Multiply(ref pose1.Rotation, ref pose2.Rotation, out result.Rotation);
        Vector3.Add(ref pose1.Translation, ref pose2.Translation, out result.Translation);
    }

    public static void Subtract(ref Pose pose1, ref Pose pose2, out Pose result)
    {
        Vector3.Subtract(ref pose1.Scale, ref pose2.Scale, out result.Scale);
        Quaternion.Divide(ref pose1.Rotation, ref pose2.Rotation, out result.Rotation);
        Vector3.Subtract(ref pose1.Translation, ref pose2.Translation, out result.Translation);
    }

    public static Pose operator +(Pose pose1, Pose pose2)
    {
        Pose result;
        Add(ref pose1, ref pose2, out result);
        return result;
    }

    public static Pose operator -(Pose pose1, Pose pose2)
    {
        Pose result;
        Subtract(ref pose1, ref pose2, out result);
        return result;
    }

    public static Pose operator *(Pose pose1, Pose pose2)
    {
        return new Pose
        {
            Scale = pose1.Scale * pose2.Scale,
            Rotation = pose1.Rotation * pose2.Rotation,
            Translation = pose1.Translation + pose2.Translation
        };
    }

    public static bool operator !=(Pose pose1, Pose pose2)
    {
        return !pose1.Equals(pose2);
    }

    public static bool operator ==(Pose pose1, Pose pose2)
    {
        return pose1.Equals(pose2);
    }

    public Matrix ToMatrix()
    {
        Matrix result;
        ToMatrix(out result);
        return result;
    }

    public void ToMatrix(out Matrix result)
    {
        // The old method here did this:
        //
        //  Matrix.CreateScale(Scale) * 
        //  Matrix.CreateFromQuaternion(Quaternion) * 
        //  Matrix.CreateTranslation(Translation);
        //
        // A matrix multiply costs 64 float muls and 48 
        // float adds.  So ignoring the cost of doing 
        // CreateFromQuaternion this had a cost of 128
        // muls and 96 adds in matrix multiplication alone!
        //
        // The new method below directly builds the matrix
        // and is only 9 muls.  Huge win!
        //

        Matrix rotation;
        Matrix.CreateFromQuaternion(ref Rotation, out rotation);

        result = new Matrix
        {
            M11 = Scale.X * rotation.M11,
            M12 = Scale.X * rotation.M12,
            M13 = Scale.X * rotation.M13,
            M14 = 0,

            M21 = Scale.Y * rotation.M21,
            M22 = Scale.Y * rotation.M22,
            M23 = Scale.Y * rotation.M23,
            M24 = 0,

            M31 = Scale.Z * rotation.M31,
            M32 = Scale.Z * rotation.M32,
            M33 = Scale.Z * rotation.M33,
            M34 = 0,

            M41 = Translation.X,
            M42 = Translation.Y,
            M43 = Translation.Z,
            M44 = 1
        };
    }

    public override string ToString()
    {
        return string.Format("Pose S: {0} Q: {1} T: {2}", Scale, Rotation, Translation);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (obj.GetType() != typeof(Pose))
        {
            return false;
        }
        return Equals((Pose)obj);
    }

    public bool Equals(Pose other)
    {
        return other.Rotation.Equals(this.Rotation) && other.Translation.Equals(this.Translation) && other.Scale.Equals(this.Scale);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int result = this.Rotation.GetHashCode();
            result = (result * 397) ^ this.Translation.GetHashCode();
            result = (result * 397) ^ this.Scale.GetHashCode();
            return result;
        }
    }
}

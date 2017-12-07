using UnityEngine;

namespace ServerShared.Player
{
    public struct PlayerMove
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public float AnimationAngle;
        public float AnimationExtension;
        public Vector3 HandlePosition;
        public Quaternion HandleRotation;

        public Vector3 SliderPosition;
        public Quaternion SliderRotation;
    }
}

using UnityEngine;

namespace Assets.Scripts
{
    public class SimpleGameObject
    {
        public Vector2 OldPosition;
        public Vector2 NewPosition;
        public Vector2 MovementDirection;
        public float Radius;
        public float Speed;
        public SimpleGameObjectTypeEnum Type;
        public IController SimpleObjectController;
        public bool HasCollided;

        public SimpleGameObject(Vector2 oldPosition, Vector2 newPosition, Vector2 movementDirection, float radius, float speed, SimpleGameObjectTypeEnum type, IController instantiatedObjectController = null)
        {
            OldPosition = oldPosition;
            NewPosition = newPosition;
            MovementDirection = movementDirection;
            Radius = radius;
            Speed = speed;
            Type = type;
            SimpleObjectController = instantiatedObjectController;
        }
    }
}

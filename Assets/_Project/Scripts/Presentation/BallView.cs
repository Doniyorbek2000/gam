using BilliardGame.Physics;
using UnityEngine;

namespace BilliardGame.Presentation
{
    /// <summary>Visual ball: follows table-space positions and rolls by the distance it travels.</summary>
    public sealed class BallView : MonoBehaviour
    {
        // Larger jumps are teleports (respot, ball in hand) and must not spin the ball.
        private const float MaxRollStep = 0.25f;

        [SerializeField] private int ballId;
        [SerializeField] private float radius = 0.028575f;

        public int BallId => ballId;

        public void Configure(int id, float ballRadius)
        {
            ballId = id;
            radius = ballRadius;
        }

        public void SetPose(Vec2 position, bool pocketed)
        {
            if (pocketed)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            Vector3 target = TableSpace.ToWorld(position, radius);
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > 1e-6f && distance < MaxRollStep)
            {
                Vector3 axis = Vector3.Cross(Vector3.up, delta / distance);
                transform.rotation = Quaternion.AngleAxis(distance / radius * Mathf.Rad2Deg, axis) * transform.rotation;
            }

            transform.position = target;
        }
    }
}

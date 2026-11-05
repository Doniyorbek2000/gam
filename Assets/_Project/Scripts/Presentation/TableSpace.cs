using BilliardGame.Physics;
using UnityEngine;

namespace BilliardGame.Presentation
{
    /// <summary>Table space (X along the length, Y across) maps to world X/Z; the cloth surface is world y = 0.</summary>
    public static class TableSpace
    {
        public static Vector3 ToWorld(Vec2 position, float height) => new Vector3((float)position.X, height, (float)position.Y);

        public static Vec2 ToTable(Vector3 world) => new Vec2(world.x, world.z);
    }
}

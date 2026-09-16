using Platformer.Core;
using Platformer.Mechanics;
using Platformer.Model;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Gameplay
{

    /// <summary>
    /// Fired when a Player collides with an Enemy.
    /// </summary>
    /// <typeparam name="EnemyCollision"></typeparam>
    public class PlayerEnemyCollision : Simulation.Event<PlayerEnemyCollision>
    {
        public EnemyController enemy;
        public PlayerController player;

        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            float playerBottom = player.Bounds.min.y;
            float enemyTop = enemy.Bounds.max.y;

            bool playerIsFalling = player.velocity.y <= 0f;

            // Allows the player's feet to be slightly below the exact top
            // of the enemy and still count as a stomp.
            float stompTolerance = 0.25f;

            var willHurtEnemy =
                playerIsFalling &&
                playerBottom >= enemyTop - stompTolerance;

            if (willHurtEnemy)
            {
                var enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.Decrement();
                    if (!enemyHealth.IsAlive)
                    {
                        Schedule<EnemyDeath>().enemy = enemy;
                        player.Bounce(5);
                    }
                    else
                    {
                        player.Bounce(7);
                    }
                }
                else
                {
                    Schedule<EnemyDeath>().enemy = enemy;
                    player.Bounce(5);
                }
            }
            else
            {
                Schedule<PlayerDeath>();
            }
        }
    }
}
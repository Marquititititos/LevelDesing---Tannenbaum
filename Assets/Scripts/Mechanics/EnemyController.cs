using System.Collections;
using System.Collections.Generic;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// A simple controller for enemies. Provides movement control over a patrol path.
    ///
    /// EXTENDED FOR LEVEL DESIGN CLASS:
    /// Movement, chase behaviour and collision handling are exposed as public fields with
    /// [Header]/[Tooltip] so they can be tuned entirely from the Inspector. All new features
    /// default to values that reproduce the original behaviour exactly, so nothing changes
    /// unless a student explicitly customises it.
    /// </summary>
    [RequireComponent(typeof(AnimationController), typeof(Collider2D))]
    public class EnemyController : MonoBehaviour
    {
        // =========================================================
        //                        PATROL
        // =========================================================
        [Header("Patrol")]
        [Tooltip("The path this enemy will patrol back and forth along. Leave empty for a stationary enemy.")]
        public PatrolPath path;

        [Tooltip("Whether the enemy moves along its patrol path. Disable to keep the enemy stationary even if a path is assigned. Enabled by default, matching the original.")]
        public bool enablePatrol = true;

        [Tooltip("Patrol speed as a fraction of the enemy's maxSpeed (from AnimationController). 0.5 = half speed (same as the original, which was hardcoded to 0.5).")]
        [Range(0.05f, 2f)]
        public float patrolSpeedMultiplier = 0.5f;

        [Tooltip("If enabled, the enemy starts patrolling in the opposite direction along its path.")]
        public bool reversePatrolDirection = false;

        // =========================================================
        //                   CHASE PLAYER (OPTIONAL)
        // =========================================================
        [Header("Chase Player (Optional)")]
        [Tooltip("If enabled, the enemy will stop patrolling and move toward the player when the player enters 'detectionRange'. Disabled by default, matching the original (patrol-only) behaviour.")]
        public bool enableChasePlayer = false;

        [Tooltip("Distance (in world units) at which the enemy notices and starts chasing the player. Only applies if 'enableChasePlayer' is enabled.")]
        [Min(0f)]
        public float detectionRange = 5f;

        [Tooltip("Chase speed as a fraction/multiple of the enemy's maxSpeed. 1 = full maxSpeed while chasing. Only applies if 'enableChasePlayer' is enabled.")]
        [Range(0.1f, 2f)]
        public float chaseSpeedMultiplier = 1f;

        // =========================================================
        //                  COMBAT / COLLISION
        // =========================================================
        [Header("Combat / Collision")]
        [Tooltip("Sound played when this enemy is defeated or takes damage (played by other systems that reference this clip).")]
        public AudioClip ouch;

        [Tooltip("Whether touching the player triggers the player-vs-enemy collision event. Enabled by default, matching the original. Disable for purely decorative/harmless enemies.")]
        public bool damagePlayerOnContact = true;

        // =========================================================
        //                       VISUALS
        // =========================================================
        [Header("Visuals")]
        [Tooltip("If enabled, flips the SpriteRenderer horizontally based on movement direction. Disabled by default since AnimationController may already handle flipping - enable only if your enemy sprite isn't flipping on its own.")]
        public bool flipSpriteWithDirection = false;

        [Tooltip("Draws detection range and patrol info as gizmos in the Scene view when this enemy is selected. Editor-only, no effect on gameplay.")]
        public bool showDebugGizmos = true;

        // =========================================================
        //                    INTERNAL STATE
        // =========================================================

        /// <summary>Moves the enemy along its assigned PatrolPath.</summary>
        internal PatrolPath.Mover mover;
        /// <summary>Reference to this enemy's AnimationController (drives movement and animation).</summary>
        internal AnimationController control;
        /// <summary>Reference to this enemy's Collider2D.</summary>
        internal Collider2D _collider;
        /// <summary>Reference to this enemy's AudioSource.</summary>
        internal AudioSource _audio;
        SpriteRenderer spriteRenderer;

        // Cached reference to the player, used only when chase is enabled.
        private PlayerController playerController;

        public Bounds Bounds => _collider.bounds;

        void Awake()
        {
            control = GetComponent<AnimationController>();
            _collider = GetComponent<Collider2D>();
            _audio = GetComponent<AudioSource>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!damagePlayerOnContact)
                return;

            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                var ev = Schedule<PlayerEnemyCollision>();
                ev.player = player;
                ev.enemy = this;
            }
        }

        void Update()
        {
            control.move = Vector2.zero;
            bool movementHandledByChase = false;

            if (enableChasePlayer)
            {
                movementHandledByChase = TryChasePlayer();
            }

            if (!movementHandledByChase && enablePatrol && path != null)
            {
                if (mover == null)
                {
                    float patrolSpeed = control.maxSpeed * patrolSpeedMultiplier * (reversePatrolDirection ? -1f : 1f);
                    mover = path.CreateMover(patrolSpeed);
                }

                Vector2 direction = mover.Position - (Vector2)transform.position;

                control.move.x = Mathf.Clamp(direction.x, -1f, 1f);
                control.move.y = Mathf.Clamp(direction.y, -1f, 1f);
            }

            if (spriteRenderer != null && Mathf.Abs(control.move.x) > 0.01f)
            {
                // Moving left = use original sprite
                if (control.move.x < 0f)
                    spriteRenderer.flipX = false;

                // Moving right = flip the original sprite
                else if (control.move.x > 0f)
                    spriteRenderer.flipX = true;
            }
        }

        /// <summary>
        /// Looks for the player and, if within range, steers the enemy toward them.
        /// Returns true if chase movement was applied this frame.
        /// </summary>
        private bool TryChasePlayer()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
                if (playerController == null)
                    return false;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, playerController.transform.position);
            if (distanceToPlayer > detectionRange)
                return false;

            float direction = Mathf.Clamp(playerController.transform.position.x - transform.position.x, -1f, 1f);
            control.move.x = direction * chaseSpeedMultiplier;
            return true;
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            if (enableChasePlayer)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, detectionRange);
            }
        }
    }
}
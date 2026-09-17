using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// AnimationController integrates physics and animation. It is generally used for simple enemy animation.
    ///
    /// EXTENDED FOR LEVEL DESIGN CLASS:
    /// Movement and jump behaviour are exposed as public fields with [Header]/[Tooltip] so they can be
    /// tuned entirely from the Inspector. All new features default to values that reproduce the
    /// original behaviour exactly (instant movement, single jump, always-flip), so nothing changes
    /// for existing objects (like EnemyController) unless a student explicitly customises it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class AnimationController : KinematicObject
    {
        // =========================================================
        //                  HORIZONTAL MOVEMENT
        // =========================================================
        [Header("Horizontal Movement")]
        [Tooltip("Max horizontal speed. (Same as the original)")]
        public float maxSpeed = 7;

        [Tooltip("If DISABLED (default), movement is instant, matching the original behaviour. Enable it so the object accelerates/decelerates gradually instead of snapping to max speed instantly.")]
        public bool useSmoothAcceleration = false;

        [Tooltip("How fast the object accelerates up to maxSpeed (units/sec^2). Only applies if 'useSmoothAcceleration' is enabled.")]
        public float acceleration = 40f;

        [Tooltip("How fast the object decelerates when 'move.x' is zero (units/sec^2). Only applies if 'useSmoothAcceleration' is enabled.")]
        public float deceleration = 45f;

        [Tooltip("Acceleration/deceleration multiplier while the object is airborne. 1 = same control as on the ground (default, matches the original). Only applies if 'useSmoothAcceleration' is enabled.")]
        [Range(0f, 1f)]
        public float airControlMultiplier = 1f;

        // internal value used to smooth horizontal movement frame to frame
        private float smoothedMoveX;

        // =========================================================
        //                     JUMP - BASIC
        // =========================================================
        [Header("Jump - Basic")]
        [Tooltip("Max jump velocity. Determines the height of a normal jump. (Same as the original)")]
        public float jumpTakeOffSpeed = 7;

        // =========================================================
        //                  JUMP - DOUBLE JUMP
        // =========================================================
        [Header("Jump - Double Jump")]
        [Tooltip("Enables or disables an extra jump while airborne, triggered the same way as a normal jump (by setting 'jump' to true while not grounded). Disabled by default, matching the original.")]
        public bool enableDoubleJump = false;

        [Tooltip("Number of extra jumps allowed while airborne (1 = double jump, 2 = triple jump, etc). Only applies if 'enableDoubleJump' is enabled.")]
        [Min(1)]
        public int extraJumpsAllowed = 1;

        [Tooltip("Speed multiplier for extra jumps relative to the normal jump (1 = same height as the normal jump).")]
        [Range(0.1f, 2f)]
        public float extraJumpSpeedMultiplier = 1f;

        private int jumpsRemaining;

        // =========================================================
        //             CONTROL FLAGS (set externally)
        // =========================================================
        [Header("Control (usually set by other scripts, e.g. EnemyController)")]
        [Tooltip("Desired direction of travel, normally in the range -1..1 on X. Set every frame by whatever script is driving this object (e.g. EnemyController).")]
        public Vector2 move;

        [Tooltip("Set to true to initiate a jump. Consumed automatically once the jump is applied.")]
        public bool jump;

        [Tooltip("Set to true to cut the current upward jump velocity short (variable jump height).")]
        public bool stopJump;

        // =========================================================
        //                       VISUALS
        // =========================================================
        [Header("Visuals")]
        [Tooltip("If enabled (default), flips the SpriteRenderer horizontally based on 'move.x', matching the original behaviour. Disable for sprites that should never flip.")]
        public bool flipSpriteWithDirection = true;

        [Tooltip("Name of the Animator bool parameter set to whether the object is grounded. Change this only if your Animator Controller uses a different parameter name.")]
        public string groundedAnimatorParam = "grounded";

        [Tooltip("Name of the Animator float parameter set to the normalized horizontal speed (0..1). Change this only if your Animator Controller uses a different parameter name.")]
        public string velocityXAnimatorParam = "velocityX";

        SpriteRenderer spriteRenderer;
        Animator animator;
        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            jumpsRemaining = extraJumpsAllowed;
        }

        protected override void ComputeVelocity()
        {
            if (IsGrounded)
                jumpsRemaining = extraJumpsAllowed; // reset extra jumps when touching the ground

            if (jump && IsGrounded)
            {
                LeaveMovingPlatform();

                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (jump && !IsGrounded && enableDoubleJump && jumpsRemaining > 0)
            {
                // Only reachable if 'enableDoubleJump' is turned on (off by default, matching the original).
                velocity.y = jumpTakeOffSpeed * extraJumpSpeedMultiplier * model.jumpModifier;
                jumpsRemaining--;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (flipSpriteWithDirection)
            {
                if (move.x > 0.01f)
                    spriteRenderer.flipX = false;
                else if (move.x < -0.01f)
                    spriteRenderer.flipX = true;
            }

            if (HasAnimatorParameter(groundedAnimatorParam, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(groundedAnimatorParam, IsGrounded);
            }

            if (HasAnimatorParameter(velocityXAnimatorParam, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(velocityXAnimatorParam, Mathf.Abs(velocity.x) / maxSpeed);
            }

            if (!useSmoothAcceleration)
            {
                // Identical to the original script: instant velocity.
                targetVelocity = move * maxSpeed;
            }
            else
            {
                // --- Horizontal movement with acceleration/deceleration and air control ---
                float targetSpeed = move.x * maxSpeed;
                bool hasInput = Mathf.Abs(move.x) > 0.01f;
                float accelRate = hasInput ? acceleration : deceleration;
                if (!IsGrounded)
                    accelRate *= airControlMultiplier;

                smoothedMoveX = Mathf.MoveTowards(smoothedMoveX, targetSpeed, accelRate * Time.deltaTime);
                targetVelocity = new Vector2(smoothedMoveX, move.y * maxSpeed);
            }
        }

        private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName && param.type == type)
                    return true;
            }

            return false;
        }
    }
}
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace BilliardGame.Avatar
{
    /// <summary>
    /// One player character (docs/TECH_ARCHITECTURE.md §2.3.1): the shared Humanoid Animator plus an Animation Rigging
    /// Two Bone IK per arm whose targets the match logic moves (bridge hand on the cloth, grip hand on the cue, drink,
    /// clap), and Humanoid look-at for the head. Lives on the model child (the Animator); MatchCharacter moves the
    /// root. Visual only.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterRig : MonoBehaviour
    {
        public static readonly int SpeedParam = Animator.StringToHash("Speed");
        public static readonly int AddressParam = Animator.StringToHash("Address");
        public static readonly int SeatedParam = Animator.StringToHash("Seated");
        public static readonly int SadParam = Animator.StringToHash("Sad");
        public static readonly int CelebrateParam = Animator.StringToHash("Celebrate");

        [SerializeField] private Animator animator;
        [SerializeField] private RigBuilder rigBuilder;
        [SerializeField] private TwoBoneIKConstraint leftArm;
        [SerializeField] private TwoBoneIKConstraint rightArm;
        [SerializeField] private Transform leftTarget;
        [SerializeField] private Transform rightTarget;
        [SerializeField] private Transform leftHint;
        [SerializeField] private Transform rightHint;
        [SerializeField] private Transform chalk;
        [Tooltip("Root speed of the walk clip (m/s); the agent walks at this speed so the feet do not slide.")]
        [SerializeField] private float walkClipSpeed = 1.2f;
        [SerializeField] private float weightSharpness = 10f;

        private float leftWeight, rightWeight, leftGoal, rightGoal;
        private Vector3 lookPosition;
        private float lookWeight, lookGoal;

        public Animator Animator => animator;
        public float WalkClipSpeed => walkClipSpeed;
        public Transform LeftTarget => leftTarget;
        public Transform RightTarget => rightTarget;
        public Transform Head => animator.GetBoneTransform(HumanBodyBones.Head);
        public Transform RightHand => animator.GetBoneTransform(HumanBodyBones.RightHand);
        public Transform LeftHand => animator.GetBoneTransform(HumanBodyBones.LeftHand);

        public void Configure(Animator anim, RigBuilder builder, TwoBoneIKConstraint left, TwoBoneIKConstraint right, Transform chalkProp, float walkSpeed)
        {
            animator = anim;
            rigBuilder = builder;
            leftArm = left;
            rightArm = right;
            leftTarget = left.data.target;
            rightTarget = right.data.target;
            leftHint = left.data.hint;
            rightHint = right.data.hint;
            chalk = chalkProp;
            walkClipSpeed = walkSpeed;
        }

        /// <summary>Blend each arm towards IK (1) or the animation (0) over a few frames.</summary>
        public void SetArms(float left, float right)
        {
            leftGoal = Mathf.Clamp01(left);
            rightGoal = Mathf.Clamp01(right);
        }

        /// <summary>Places a hand target (world) and its elbow hint (offset from the hand, world).</summary>
        public void PlaceLeft(Vector3 position, Quaternion rotation, Vector3 elbow)
        {
            leftTarget.SetPositionAndRotation(position, rotation);
            if (leftHint != null) leftHint.position = elbow;
        }

        public void PlaceRight(Vector3 position, Quaternion rotation, Vector3 elbow)
        {
            rightTarget.SetPositionAndRotation(position, rotation);
            if (rightHint != null) rightHint.position = elbow;
        }

        public void LookAt(Vector3 position, float weight)
        {
            lookPosition = position;
            lookGoal = Mathf.Clamp01(weight);
        }

        public void ShowChalk(bool visible)
        {
            if (chalk != null && chalk.gameObject.activeSelf != visible) chalk.gameObject.SetActive(visible);
        }

        public void SetSpeed(float metresPerSecond) => animator.SetFloat(SpeedParam, metresPerSecond, 0.1f, Time.deltaTime);
        public void SetAddress(bool on) => animator.SetBool(AddressParam, on);
        public void SetSeated(bool on) => animator.SetBool(SeatedParam, on);
        public void Sad() => animator.SetTrigger(SadParam);
        public void Celebrate() => animator.SetTrigger(CelebrateParam);

        private void Update()
        {
            float k = 1f - Mathf.Exp(-weightSharpness * Time.deltaTime);
            leftWeight = Mathf.Lerp(leftWeight, leftGoal, k);
            rightWeight = Mathf.Lerp(rightWeight, rightGoal, k);
            lookWeight = Mathf.Lerp(lookWeight, lookGoal, k * 0.5f);
            if (leftArm != null) leftArm.weight = leftWeight;
            if (rightArm != null) rightArm.weight = rightWeight;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            animator.SetLookAtWeight(lookWeight, 0.15f, 0.8f, 1f, 0.6f);
            animator.SetLookAtPosition(lookPosition);
        }
    }
}

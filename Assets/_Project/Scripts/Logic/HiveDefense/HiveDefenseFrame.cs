using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What the View tells the session each tick: where it moved the core, what the player aims at, presses and holds.</summary>
    public readonly struct HiveDefenseFrame
    {
        // Not game tuning: a camera way on the ground shorter than this (squared) is none, as for a camera looking straight down.
        private const float NoWaySqr = 0.00000001f;

        /// <summary>
        /// Makes a frame with the mouse idle, so auto-aim chooses what an aimed song strikes, and Give and Call Wave untouched.
        /// <paramref name="move"/> is x and z on the ground and steers the giant.
        /// </summary>
        public HiveDefenseFrame(
            Vector3 corePosition,
            Vector3 aimPoint,
            Vector2 move,
            int pressedHoleMask,
            bool rightHand,
            bool leftHand)
            : this(corePosition, aimPoint, move, pressedHoleMask, rightHand, leftHand, false)
        {
        }

        /// <summary>
        /// Makes a frame with Give and Call Wave untouched. <paramref name="move"/> is x and z on the ground and steers the
        /// giant. While <paramref name="isMouseLive"/> is true the mouse aims: aimed songs strike at <paramref name="aimPoint"/>.
        /// </summary>
        public HiveDefenseFrame(
            Vector3 corePosition,
            Vector3 aimPoint,
            Vector2 move,
            int pressedHoleMask,
            bool rightHand,
            bool leftHand,
            bool isMouseLive)
            : this(corePosition, aimPoint, move, pressedHoleMask, rightHand, leftHand, isMouseLive, false, false)
        {
        }

        /// <summary>
        /// Makes a frame with no camera direction, so the giant steers on the world's axes. <paramref name="move"/> is x and
        /// z on the ground and steers the giant. While <paramref name="isMouseLive"/> is true the mouse aims: aimed songs
        /// strike at <paramref name="aimPoint"/>. <paramref name="give"/> is true while Give is held, and
        /// <paramref name="callWave"/> on the tick Call Wave is pressed.
        /// </summary>
        public HiveDefenseFrame(
            Vector3 corePosition,
            Vector3 aimPoint,
            Vector2 move,
            int pressedHoleMask,
            bool rightHand,
            bool leftHand,
            bool isMouseLive,
            bool give,
            bool callWave)
            : this(corePosition, aimPoint, move, pressedHoleMask, rightHand, leftHand, isMouseLive, give, callWave, Vector3.zero)
        {
        }

        /// <summary>
        /// Makes a frame. <paramref name="move"/> steers the giant relative to <paramref name="cameraForward"/>, the way the
        /// camera looks: its y is along that way on the ground and its x to the right of it. While
        /// <paramref name="isMouseLive"/> is true the mouse aims: aimed songs, and the giant's leap slam, strike at
        /// <paramref name="aimPoint"/>. <paramref name="give"/> is true while Give is held, and <paramref name="callWave"/>
        /// on the tick Call Wave is pressed.
        /// </summary>
        public HiveDefenseFrame(
            Vector3 corePosition,
            Vector3 aimPoint,
            Vector2 move,
            int pressedHoleMask,
            bool rightHand,
            bool leftHand,
            bool isMouseLive,
            bool give,
            bool callWave,
            Vector3 cameraForward)
        {
            CorePosition = corePosition;
            AimPoint = aimPoint;
            Move = move;
            PressedHoleMask = pressedHoleMask;
            RightHand = rightHand;
            LeftHand = leftHand;
            IsMouseLive = isMouseLive;
            Give = give;
            CallWave = callWave;
            CameraForward = cameraForward;
        }

        /// <summary>
        /// Where the View moved the core. The session takes it as the core's place and turns the core to face the way it
        /// moved. Ignored while the giant stands, because the core then rides inside the giant.
        /// </summary>
        public Vector3 CorePosition { get; }

        /// <summary>The point on the ground under the cursor. Only read while <see cref="IsMouseLive"/>.</summary>
        public Vector3 AimPoint { get; }

        /// <summary>True while the mouse aims (it moved a moment ago or a button is held); false while auto-aim chooses.</summary>
        public bool IsMouseLive { get; }

        /// <summary>
        /// The keys' move, relative to the camera and not yet turned by it, length 0 to 1: y forward along
        /// <see cref="CameraForward"/> (W) and x to its right (D). With no camera direction, x and y are the world's x and z.
        /// It steers the giant through <see cref="GroundStep"/>.
        /// </summary>
        public Vector2 Move { get; }

        /// <summary>
        /// The way the camera looks, on the ground and of unit length (plan section I's chase camera), or zero when the View
        /// gives none. While the giant stands it steers relative to this, so W walks away from the camera. Only its way on
        /// the ground counts; a zero, or one straight down, means the world's forward.
        /// </summary>
        public Vector3 CameraForward { get; }

        /// <summary>
        /// Bit n is set when hole n was pressed this tick. See <see cref="HoleBit"/>. While the giant stands the holes are its
        /// moves (H roar, J punch, K kick, L slam) and no note reaches the songs.
        /// </summary>
        public int PressedHoleMask { get; }

        /// <summary>
        /// True on the tick the right hand's launch was pressed (R2, the right mouse button or Space). Only the giant uses it: it
        /// launches its right fist.
        /// </summary>
        public bool RightHand { get; }

        /// <summary>
        /// True on the tick the left hand's launch was pressed (L2, the left mouse button or Left Shift). Only the giant uses it:
        /// it launches its left fist.
        /// </summary>
        public bool LeftHand { get; }

        /// <summary>True while Give is held: beside a hurt hive the swarm pays wasps into its health, and the hive hands out nothing.</summary>
        public bool Give { get; }

        /// <summary>True on the tick Call Wave is pressed: in a farm phase the wave comes at once, for bonus wasps.</summary>
        public bool CallWave { get; }

        /// <summary>The bit for a hole, counting from 0.</summary>
        public static int HoleBit(int hole) => 1 << hole;

        /// <summary>
        /// Turns a move relative to the camera into a step on the ground, once: y along the way
        /// <paramref name="cameraForward"/> looks on the ground (W) and x to its right (D). A move longer than 1 is cut to 1.
        /// A zero camera way, or one straight down, means the world's forward, so x and y are then the world's x and z. The
        /// View walks the core with it and the session steers the giant with it, so the keys mean the same to both.
        /// </summary>
        public static Vector3 GroundStep(Vector2 move, Vector3 cameraForward)
        {
            move = Vector2.ClampMagnitude(move, 1f);
            cameraForward.y = 0f;
            float sqr = cameraForward.sqrMagnitude;
            Vector3 forward = sqr > NoWaySqr ? cameraForward / Mathf.Sqrt(sqr) : Vector3.forward;
            var right = new Vector3(forward.z, 0f, -forward.x);
            return (right * move.x) + (forward * move.y);
        }
    }
}

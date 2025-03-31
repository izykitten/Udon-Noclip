using UdonSharp;
using UnityEngine;
using Varneon.VUdon.Noclip.Enums;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Varneon.VUdon.Noclip
{
    /// <summary>
    /// Simple noclip
    /// </summary>
    [SelectionBase]
    [DefaultExecutionOrder(-1000000000)]
    [HelpURL("https://github.com/Varneon/VUdon-Noclip/wiki/Settings")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Noclip : UdonSharpBehaviour
    {
        #region Serialized Fields
        /// <summary>
        /// Method for triggering noclip
        /// </summary>
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Method for triggering the noclip mode")]
        private NoclipTriggerMethod noclipTriggerMethod = NoclipTriggerMethod.DoubleJump;

        /// <summary>
        /// Time in which jump has to be double tapped in order to toggle noclip
        /// </summary>
        [SerializeField]
        [Tooltip("Time in which jump has to be double tapped in order to toggle noclip")]
        [Range(0.1f, 5f)]  // Changed from 1f to 5f for higher maximum threshold
        private float toggleThreshold = 0.25f;

        /// <summary>
        /// Maximum speed in m/s
        /// </summary>
        [SerializeField]
        [Tooltip("Maximum speed in m/s")]
        [Range(1f, 50f)]
        private float speed = 15f;

        /// <summary>
        /// Input speed multiplier curve for VR
        /// </summary>
        [Header("VR")]
        [SerializeField]
        [Tooltip("Input speed multiplier curve for VR.\n\nHorizontal (0-1): VR movement input magnitude\n\nVertical (0-1): Speed multiplier")]
        private AnimationCurve vrInputMultiplier = null;

        /// <summary>
        /// Speed multiplier when Shift is not pressed
        /// </summary>
        [Header("Desktop")]
        [SerializeField]
        [Tooltip("Speed multiplier when Shift is not pressed")]
        [Range(0.1f, 1f)]
        private float desktopSpeedFraction = 0.25f;

        /// <summary>
        /// Allow vertical movement on desktop
        /// </summary>
        [SerializeField]
        [Tooltip("Allow vertical movement on desktop")]
        private bool desktopVerticalInput = true;

        /// <summary>
        /// Key for ascending on desktop
        /// </summary>
        [SerializeField]
        [Tooltip("Key for ascending on desktop")]
        private KeyCode upKey = KeyCode.E;

        /// <summary>
        /// Key for descending on desktop
        /// </summary>
        [SerializeField]
        [Tooltip("Key for descending on desktop")]
        private KeyCode downKey = KeyCode.Q;

        /// <summary>
        /// Whether to restrict noclip to specific users
        /// </summary>
        [Header("User Restrictions")]
        [SerializeField]
        [Tooltip("If enabled, only users with usernames in the allowed list can use noclip")]
        private bool restrictToSpecificUsers = false;

        /// <summary>
        /// Array of allowed usernames
        /// </summary>
        [SerializeField]
        [Tooltip("List of usernames that are allowed to use noclip")]
        private string[] allowedUsernames = new string[0];
        #endregion // Serialized Fields

        #region Private Variables
        /// <summary>
        /// Can double jump be used to toggle noclip
        /// </summary>
        private bool toggleByDoubleJump;

        /// <summary>
        /// Can five jumps be used to toggle noclip
        /// </summary>
        private bool toggleByFiveJumps;

        /// <summary>
        /// Count of consecutive jumps for five-jump trigger
        /// </summary>
        private int consecutiveJumpCount;

        /// <summary>
        /// Is the current user allowed to use noclip
        /// </summary>
        private bool userIsAllowed;

        /// <summary>
        /// Is the noclip currently enabled
        /// </summary>
        private bool noclipEnabled;

        /// <summary>
        /// Is the switching currently primed, waiting for second jump press within the threshold
        /// </summary>
        private bool switchPrimed;

        /// <summary>
        /// Current position of the player
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// Last position of the player
        /// </summary>
        /// <remarks>
        /// Used for applying remaining velocity to the player after switching noclip off
        /// </remarks>
        private Vector3 lastPosition;

        /// <summary>
        /// Local player
        /// </summary>
        private VRCPlayerApi localPlayer;

        /// <summary>
        /// Is local player currently in VR
        /// </summary>
        private bool vrEnabled;

        /// <summary>
        /// Collider buffer for detecting local player's collider
        /// </summary>
        private Collider[] playerCollider = new Collider[1];

        /// <summary>
        /// Current horizontal move input
        /// </summary>
        private float inputMoveHorizontal;

        /// <summary>
        /// Current vertical move input
        /// </summary>
        private float inputMoveVertical;

        /// <summary>
        /// Current vertical look input
        /// </summary>
        private float inputLookVertical;
        #endregion // Private Variables

        private void OnEnable()
        {
            // Initialize animation curve if it's null
            if (vrInputMultiplier == null)
            {
                vrInputMultiplier = new AnimationCurve();
                // Use AddKey with direct float values instead of creating Keyframe objects
                vrInputMultiplier.AddKey(0f, 0f);
                vrInputMultiplier.AddKey(1f, 1f);
            }

            // Make sure the string array is initialized
            if (allowedUsernames == null)
            {
                allowedUsernames = new string[0];
            }
        }

        #region Unity Methods
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;

            vrEnabled = localPlayer.IsUserInVR();

            toggleByDoubleJump = noclipTriggerMethod == NoclipTriggerMethod.DoubleJump;
            toggleByFiveJumps = noclipTriggerMethod == NoclipTriggerMethod.FiveJumps;

            // Check if user is allowed
            userIsAllowed = !restrictToSpecificUsers || IsUserAllowed();
        }

        private void LateUpdate()
        {
            if (noclipEnabled)
            {
                Vector3 localPlayerPos = localPlayer.GetPosition();

                // Cache the last position
                lastPosition = position;

                // If the player's collider isn't enabled, the player has entered a seat so disable noclip
                // Radius is supposed to be 0f but the check sometimes fails when the player is inside a collider
                // Larger radius might return false positives if LocalPlayer layer is being used for something else in the world
                if (Physics.OverlapSphereNonAlloc(localPlayerPos, 100000f, playerCollider, 1024) < 1)
                {
                    SetNoclipEnabled(false);

                    return;
                }

                // Get the head rotation for movement direction
                Quaternion headRot = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

                float deltaTime = Time.deltaTime;

                if (vrEnabled)
                {
                    // Get the movement input vector
                    Vector3 movementInputVector = new Vector3(inputMoveHorizontal, 0f, inputMoveVertical);

                    // Get the maximum delta magnitude
                    float deltaTimeSpeed = deltaTime * speed;

                    // Create a delta vector for local X and Z axes
                    Vector3 xzDelta = deltaTimeSpeed * vrInputMultiplier.Evaluate(movementInputVector.magnitude) * movementInputVector.normalized;

                    // Create a delta vector for world Y axis
                    Vector3 yWorldDelta = new Vector3(0f, vrInputMultiplier.Evaluate(Mathf.Abs(inputLookVertical)) * Mathf.Sign(inputLookVertical) * deltaTimeSpeed, 0f);

                    // Apply the position changes
                    position += headRot * xzDelta + yWorldDelta;

                    // Get the player's playspace origin tracking data
                    VRCPlayerApi.TrackingData originData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);

                    // Get the playspace delta for applying to the final position
                    Vector3 playspaceDelta = originData.position - localPlayerPos;

                    // If the player is in VR, use the origin's rotation instead of player's rotation and align the room with spawn point
                    localPlayer.TeleportTo(position + playspaceDelta, originData.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
                }
                else
                {
                    float worldVertical = (Input.GetKey(downKey) ? -1f : 0f) + (Input.GetKey(upKey) ? 1f : 0f);

                    // Get the maximum delta magnitude
                    float deltaTimeMaxSpeed = deltaTime * (Input.GetKey(KeyCode.LeftShift) ? speed : speed * desktopSpeedFraction);

                    // Apply the position changes from vertical and horizontal inputs
                    position += headRot * (new Vector3(inputMoveHorizontal, 0f, inputMoveVertical).normalized * deltaTimeMaxSpeed);

                    // If allowed, apply vertical motion
                    if (desktopVerticalInput)
                    {
                        position += new Vector3(0f, deltaTimeMaxSpeed * worldVertical, 0f);
                    }

                    // Teleport player to the new position
                    localPlayer.TeleportTo(position, localPlayer.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.Default, true);
                }

                // Force the player's velocity to zero to prevent the falling animation from triggering
                localPlayer.SetVelocity(Vector3.zero);
            }
        }
        #endregion

        #region Public Delayed Custom Event Methods
        /// <summary>
        /// Disables the switch priming if jump hasn't been pressed again within the threshold
        /// </summary>
        public void _DisablePriming()
        {
            switchPrimed = false;
        }
        
        /// <summary>
        /// Resets the jump counter after threshold
        /// </summary>
        public void _ResetJumpCounter()
        {
            consecutiveJumpCount = 0;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets the noclip mode enabled
        /// </summary>
        /// <param name="enabled"></param>
        private void SetNoclipEnabled(bool enabled)
        {
            // Don't allow enabling if user is not on the allowed list
            // Bypass restriction check if in Unity Editor play mode
            bool inEditorPlayMode = false;
            #if UNITY_EDITOR
            inEditorPlayMode = true;
            #endif
            
            if (enabled && restrictToSpecificUsers && !userIsAllowed && !inEditorPlayMode) return;

            if (noclipEnabled == enabled) { return; }

            noclipEnabled = enabled;

            localPlayer.Immobilize(enabled);

            if (enabled)
            {
                // Get the initial position of the player
                position = localPlayer.GetPosition();
            }
            else
            {
                // Apply the remainder velocity from last lerp to the player's velocity to allow them to fly after turning off noclip
                localPlayer.SetVelocity((position - lastPosition) / Time.deltaTime);
            }
        }

        /// <summary>
        /// Checks if the current user's username is in the allowed list
        /// </summary>
        private bool IsUserAllowed()
        {
            if (allowedUsernames == null || allowedUsernames.Length == 0)
            {
                return false;
            }

            string username = localPlayer.displayName;
            
            for (int i = 0; i < allowedUsernames.Length; i++)
            {
                if (allowedUsernames[i] == username)
                {
                    return true;
                }
            }
            
            return false;
        }
        #endregion

        #region VRChat Override Methods
        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            // Only register inputs when jump was just pressed
            if (!value) return;

            // Handle double jump method
            if (toggleByDoubleJump)
            {
                if (noclipEnabled)
                {
                    // If noclip is enabled and switch primed, disable noclip
                    if (switchPrimed)
                    {
                        switchPrimed = false;
                        SetNoclipEnabled(false);
                    }
                    else // Prime the switch if it's not already
                    {
                        switchPrimed = true;
                        SendCustomEventDelayedSeconds(nameof(_DisablePriming), toggleThreshold);
                    }
                }
                else
                {
                    // If noclip is not enabled and switch is primed, enable noclip
                    if (switchPrimed)
                    {
                        switchPrimed = false;
                        SetNoclipEnabled(true);
                    }
                    else // Prime the switch if it's not already
                    {
                        switchPrimed = true;
                        SendCustomEventDelayedSeconds(nameof(_DisablePriming), toggleThreshold);
                    }
                }
            }

            // Handle five jumps method
            if (toggleByFiveJumps)
            {
                // Increment jump counter
                consecutiveJumpCount++;

                // Reset counter timer
                SendCustomEventDelayedSeconds(nameof(_ResetJumpCounter), toggleThreshold);

                // If we reached 5 jumps, toggle noclip
                if (consecutiveJumpCount >= 5)
                {
                    consecutiveJumpCount = 0;
                    SetNoclipEnabled(!noclipEnabled);
                }
            }
        }

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            inputMoveHorizontal = value;
        }

        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            inputMoveVertical = value;
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            if (noclipEnabled && vrEnabled)
            {
                inputLookVertical = value;
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                // If the local player respawns, disable noclip to prevent confusion
                SetNoclipEnabled(false);
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // If the local player just left and noclip is enabled, disable it to prevent any unnecessary errors when leaving the world
            if (noclipEnabled && !Utilities.IsValid(player))
            {
                noclipEnabled = false;
            }
        }
        #endregion

        #region Public API Methods
        /// <summary>
        /// Enables noclip
        /// </summary>
        public void _EnableNoclip()
        {
            SetNoclipEnabled(true);
        }

        /// <summary>
        /// Disables noclip
        /// </summary>
        public void _DisableNoclip()
        {
            SetNoclipEnabled(false);
        }

        /// <summary>
        /// Sets noclip enabled
        /// </summary>
        public void _SetNoclipEnabled(bool enabled)
        {
            SetNoclipEnabled(enabled);
        }

        /// <summary>
        /// Sets the noclip max speed
        /// </summary>
        public void _SetMaxSpeed(float maxSpeed)
        {
            speed = maxSpeed;
        }

        /// <summary>
        /// Add a username to the allowed list
        /// </summary>
        public void _AddAllowedUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return;
            }
            
            // Make sure array is initialized
            if (allowedUsernames == null)
            {
                allowedUsernames = new string[0];
            }
            
            // Check if already in the list
            for (int i = 0; i < allowedUsernames.Length; i++)
            {
                if (allowedUsernames[i] == username)
                {
                    return; // Username already exists
                }
            }
            
            // Create new array with one more slot
            string[] newArray = new string[allowedUsernames.Length + 1];
            
            // Copy existing elements
            for (int i = 0; i < allowedUsernames.Length; i++)
            {
                newArray[i] = allowedUsernames[i];
            }
            
            // Add new username to the end
            newArray[newArray.Length - 1] = username;
            
            // Replace old array
            allowedUsernames = newArray;
            
            // Update allowed status
            userIsAllowed = !restrictToSpecificUsers || IsUserAllowed();
        }

        /// <summary>
        /// Remove a username from the allowed list
        /// </summary>
        public void _RemoveAllowedUser(string username)
        {
            if (string.IsNullOrEmpty(username) || allowedUsernames == null || allowedUsernames.Length == 0)
            {
                return;
            }
            
            // Count how many entries to keep
            int itemsToKeep = 0;
            bool found = false;
            
            for (int i = 0; i < allowedUsernames.Length; i++)
            {
                if (allowedUsernames[i] != username)
                {
                    itemsToKeep++;
                }
                else
                {
                    found = true;
                }
            }
            
            // If username not found, nothing to do
            if (!found)
            {
                return;
            }
            
            // Create new array with one fewer slot
            string[] newArray = new string[itemsToKeep];
            
            // Copy all elements except the one to remove
            int newIndex = 0;
            for (int i = 0; i < allowedUsernames.Length; i++)
            {
                if (allowedUsernames[i] != username)
                {
                    newArray[newIndex] = allowedUsernames[i];
                    newIndex++;
                }
            }
            
            // Replace old array
            allowedUsernames = newArray;
            
            // Update allowed status
            userIsAllowed = !restrictToSpecificUsers || IsUserAllowed();
        }

        /// <summary>
        /// Add the local user to the allowed list
        /// </summary>
        public void _AddSelfToAllowedList()
        {
            if (Networking.LocalPlayer != null)
            {
                _AddAllowedUser(Networking.LocalPlayer.displayName);
            }
        }

        /// <summary>
        /// Clear the allowed users list
        /// </summary>
        public void _ClearAllowedUsers()
        {
            allowedUsernames = new string[0];
            userIsAllowed = !restrictToSpecificUsers;
        }

        /// <summary>
        /// Enable or disable user restrictions
        /// </summary>
        public void _SetUserRestrictions(bool restrict)
        {
            restrictToSpecificUsers = restrict;
            userIsAllowed = !restrictToSpecificUsers || IsUserAllowed();
        }
        #endregion
    }
}

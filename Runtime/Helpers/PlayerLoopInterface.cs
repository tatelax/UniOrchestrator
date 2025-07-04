using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.LowLevel;
#if UNITY_2020_1_OR_NEWER

#else
using UnityEngine.Experimental.LowLevel;
using UnityEngine.Experimental.PlayerLoop;
#endif

namespace UniOrchestrator
{
    /// <summary>
    /// Unity exposes the PlayerLoop to allow you to insert your own "systems" to be run in similar ways to eg. Update or FixedUpate.
    /// The interface for that is a bit hairy, and there are bugs that needs workarounds, so this is a nice interface for interacting with that system.
    ///
    /// In essence, use PlayerLoopInterface.InsertSystemBefore/After to have a callback be executed every frame, before or after some built-in system.
    /// The built-in systems live in UnityEngine.Experimental.PlayerLoop, so if you want to insert a system to run just before Update, call:
    ///
    /// PlayerLoopInterface.InsertSystemBefore(typeof(MyType), MyMethod, typeof(UnityEngine.PlayerLoop.Update);
    ///
    /// If you want to run in the fixed timestep (FixedUpdate), you have to insert the system as a subsystem of UnityEngine.PlayerLoop.FixedUpdate. For example, use
    /// UnityEngine.PlayerLoop.FixedUpdate.ScriptRunBehaviourFixedUpdate:
    ///
    /// PlayerLoopInterface.InsertSystemBefore(typeof(MyType), MyMethod, typeof(UnityEngine.PlayerLoop.FixedUpdate.ScriptRunBehaviourFixedUpdate);
    /// </summary>
    internal static class PlayerLoopInterface {

        private static List<PlayerLoopSystem> insertedSystems = new List<PlayerLoopSystem>();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize() {
            // Systems are not automatically removed from the PlayerLoop, so we need to clean up the ones that have been added in play mode, as they'd otherwise
            // keep running when outside play mode, and in the next play mode if we don't have assembly reload turned on.

            Application.quitting += ClearInsertedSystems;
        }

        private static void ClearInsertedSystems ()
        {
            foreach (var playerLoopSystem in insertedSystems)
                TryRemoveSystem(playerLoopSystem.type);

            insertedSystems.Clear();

            Application.quitting -= ClearInsertedSystems;
        }

        private enum InsertType {
            Before,
            After
        }

        /// <summary>
        /// Inserts a new player loop system in the player loop, just after another system.
        /// </summary>
        /// <param name="newSystemMarker">Type marker for the new system.</param>
        /// <param name="newSystemUpdate">Callback that will be called each frame after insertAfter.</param>
        /// <param name="insertAfter">The subsystem to insert the system after.</param>
        public static void InsertSystemAfter(Type newSystemMarker, PlayerLoopSystem.UpdateFunction newSystemUpdate, Type insertAfter) {
            var playerLoopSystem = new PlayerLoopSystem {type = newSystemMarker, updateDelegate = newSystemUpdate};
            InsertSystemAfter(playerLoopSystem, insertAfter);
        }

        /// <summary>
        /// Inserts a new player loop system in the player loop, just before another system.
        /// </summary>
        /// <param name="newSystemMarker">Type marker for the new system.</param>
        /// <param name="newSystemUpdate">Callback that will be called each frame before insertBefore.</param>
        /// <param name="insertBefore">The subsystem to insert the system before.</param>
        public static void InsertSystemBefore(Type newSystemMarker, PlayerLoopSystem.UpdateFunction newSystemUpdate, Type insertBefore) {
            var playerLoopSystem = new PlayerLoopSystem {type = newSystemMarker, updateDelegate = newSystemUpdate};
            InsertSystemBefore(playerLoopSystem, insertBefore);
        }

        /// <summary>
        /// Inserts a new player loop system in the player loop, just after another system.
        /// </summary>
        /// <param name="toInsert">System to insert. Needs to have updateDelegate and Type set.</param>
        /// <param name="insertAfter">The subsystem to insert the system after</param>
        public static void InsertSystemAfter(PlayerLoopSystem toInsert, Type insertAfter) {
            if (toInsert.type == null)
                throw new ArgumentException("The inserted player loop system must have a marker type!", nameof(toInsert.type));
            if (toInsert.updateDelegate == null)
                throw new ArgumentException("The inserted player loop system must have an update delegate!", nameof(toInsert.updateDelegate));
            if (insertAfter == null)
                throw new ArgumentNullException(nameof(insertAfter));

            var rootSystem = PlayerLoop.GetCurrentPlayerLoop();

            InsertSystem(ref rootSystem, toInsert, insertAfter, InsertType.After, out var couldInsert);
            if (!couldInsert) {
                throw new ArgumentException($"When trying to insert the type {toInsert.type.Name} into the player loop after {insertAfter.Name}, " +
                                            $"{insertAfter.Name} could not be found in the current player loop!");
            }

            insertedSystems.Add(toInsert);
            PlayerLoop.SetPlayerLoop(rootSystem);
        }

        /// <summary>
        /// Inserts a new player loop system in the player loop, just before another system.
        /// </summary>
        /// <param name="toInsert">System to insert. Needs to have updateDelegate and Type set.</param>
        /// <param name="insertBefore">The subsystem to insert the system before</param>
        public static void InsertSystemBefore(PlayerLoopSystem toInsert, Type insertBefore) {
            if (insertBefore == null)
                throw new ArgumentNullException(nameof(insertBefore));

            var rootSystem = PlayerLoop.GetCurrentPlayerLoop();
            InsertSystem(ref rootSystem, toInsert, insertBefore, InsertType.Before, out var couldInsert);
            if (!couldInsert) {
                throw new ArgumentException($"When trying to insert the type {toInsert.type.Name} into the player loop before {insertBefore.Name}, " +
                                            $"{insertBefore.Name} could not be found in the current player loop!");
            }

            insertedSystems.Add(toInsert);
            PlayerLoop.SetPlayerLoop(rootSystem);
        }

        /// <summary>
        /// Tries to remove a system from the PlayerLoop. The first system found that has the same type identifier as the supplied one will be removed.
        /// </summary>
        /// <param name="type">Type identifier of the system to remove</param>
        /// <returns></returns>
        public static bool TryRemoveSystem(Type type) {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "Trying to remove a null type!");

            var currentSystem = PlayerLoop.GetCurrentPlayerLoop();
            var couldRemove = TryRemoveTypeFrom(ref currentSystem, type);
            PlayerLoop.SetPlayerLoop(currentSystem);
            return couldRemove;
        }

        private static bool TryRemoveTypeFrom(ref PlayerLoopSystem currentSystem, Type type) {
            var subSystems = currentSystem.subSystemList;
            if (subSystems == null)
                return false;

            for (int i = 0; i < subSystems.Length; i++) {
                if (subSystems[i].type == type) {
                    var newSubSystems = new PlayerLoopSystem[subSystems.Length - 1];

                    Array.Copy(subSystems, newSubSystems, i);
                    Array.Copy(subSystems, i + 1, newSubSystems, i, subSystems.Length - i - 1);

                    currentSystem.subSystemList = newSubSystems;

                    return true;
                }

                if (TryRemoveTypeFrom(ref subSystems[i], type))
                    return true;
            }

            return false;
        }

        public static PlayerLoopSystem CopySystem(PlayerLoopSystem system) {
            // PlayerLoopSystem is a struct.
            var copy = system;

            // but the sub system list is an array.
            if (system.subSystemList != null) {
                copy.subSystemList = new PlayerLoopSystem[system.subSystemList.Length];
                for (int i = 0; i < copy.subSystemList.Length; i++) {
                    copy.subSystemList[i] = CopySystem(system.subSystemList[i]);
                }
            }

            return copy;
        }

        private static void InsertSystem(ref PlayerLoopSystem currentLoopRecursive, PlayerLoopSystem toInsert, Type insertTarget, InsertType insertType,
            out bool couldInsert) {
            var currentSubSystems = currentLoopRecursive.subSystemList;
            if (currentSubSystems == null) {
                couldInsert = false;
                return;
            }

            int indexOfTarget = -1;
            for (int i = 0; i < currentSubSystems.Length; i++)
            {
                if (currentSubSystems[i].type != insertTarget) continue;
                indexOfTarget = i;
                break;
            }

            if (indexOfTarget != -1) {
                var newSubSystems = new PlayerLoopSystem[currentSubSystems.Length + 1];

                var insertIndex = insertType == InsertType.Before ? indexOfTarget : indexOfTarget + 1;

                for (int i = 0; i < newSubSystems.Length; i++) {
                    if (i < insertIndex)
                        newSubSystems[i] = currentSubSystems[i];
                    else if (i == insertIndex) {
                        newSubSystems[i] = toInsert;
                    }
                    else {
                        newSubSystems[i] = currentSubSystems[i - 1];
                    }
                }

                couldInsert = true;
                currentLoopRecursive.subSystemList = newSubSystems;
            }
            else {
                for (var i = 0; i < currentSubSystems.Length; i++) {
                    var subSystem = currentSubSystems[i];
                    InsertSystem(ref subSystem, toInsert, insertTarget, insertType, out var couldInsertInInner);
                    if (!couldInsertInInner) continue;
                    currentSubSystems[i] = subSystem;
                    couldInsert = true;
                    return;
                }

                couldInsert = false;
            }
        }

        public static void ShowPlayerLoop(PlayerLoopSystem playerLoopSystem, StringBuilder text, int inline)
        {
            if (playerLoopSystem.type != null)
            {
                for (var i = 0; i < inline; i++)
                {
                    text.Append("\t");
                }
                text.AppendLine(playerLoopSystem.type.Name);
            }

            if (playerLoopSystem.subSystemList == null) return;
        
            inline++;
            foreach (var s in playerLoopSystem.subSystemList)
            {
                ShowPlayerLoop(s, text, inline);
            }
        }
    }
}
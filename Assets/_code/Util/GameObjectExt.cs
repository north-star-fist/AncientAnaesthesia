using UnityEngine;

namespace Sergei.Safonov.Unity.Util {

    /// <summary>
    /// Extensions for <see cref="GameObject"/> class.
    /// </summary>
    public static class GameObjectExt {

        /// <summary>
        /// Gets a particular component of a <see cref="GameObject"/>. Adds the component if there is no such one attached
        /// to the game object.
        /// </summary>
        /// <typeparam name="T"> type of the needed component </typeparam>
        /// <param name="obj"> game object </param>
        /// <returns> gotten or added component </returns>
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component {
            if (!obj.TryGetComponent<T>(out var comp)) {
                comp = obj.AddComponent<T>();
            }
            return comp;
        }

        /// <summary>
        /// Destroys all the child game objects of the specified game object.
        /// </summary>
        /// <param name="trans"> parent game object transform </param>
        public static void DestroyAllChildObjects(this Transform trans) {
            while (trans.childCount > 0) {
                GameObject.DestroyImmediate(trans.GetChild(0).gameObject);
            }
        }
    }
}
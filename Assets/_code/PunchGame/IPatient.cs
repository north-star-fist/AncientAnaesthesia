using System;
using UnityEngine;

namespace AncientAnaesthesia {

    public interface IPatient {

        /// <summary>
        /// Is raised when the patient is ready/unready for getting punch.
        /// </summary>
        public IObservable<bool> ReadyForPunch { get; }

        /// <summary>
        /// Returns root rigidbody of the patient if it exists.
        /// </summary>
        Rigidbody Rigidbody { get; }

        /// <summary>
        /// Updates patient look due to damage.
        /// </summary>
        /// <param name="areaId"> damageable area Id </param>
        /// <param name="position"> hit positiion </param>
        /// <param name="normal"> hit normal </param>
        /// <param name="damageValue"> damage value </param>
        void Damage(string areaId, Vector3 position, Vector3 normal, float damageValue);

        /// <summary>
        /// Heals themself.
        /// </summary>
        void Heal();

        /// <summary>
        /// Gets a knock out with optional force impulse applied.
        /// </summary>
        public void KnockOut(Vector3 punchImpulse);
    }
}

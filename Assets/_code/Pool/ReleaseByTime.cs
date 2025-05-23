using R3;
using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Coolball {

    /// <summary>
    /// Component that releases an object back to pool.
    /// </summary>
    public class ReleaseByTime : MonoBehaviour {

        public void ReleaseAfter<T>(IObjectPool<T> pool, T obj, float time) where T : class {
            Observable.Timer(TimeSpan.FromSeconds(time)).Subscribe(_ => pool.Release(obj)).AddTo(this);
        }
    }
}
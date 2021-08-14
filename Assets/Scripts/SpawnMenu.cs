using System;

using Cysharp.Threading.Tasks;

using UnityEngine;

public class SpawnMenu : MonoBehaviour
{
    public GameObject Prefab;

    private void Awake()
    {
        Routine().Forget();

        async UniTaskVoid Routine()
        {
            var p = Instantiate(Prefab, transform.position, transform.rotation);
            await UniTask.Yield();
            Destroy(p);
        }
    }

    private void OnGUI()
    {

    }
}

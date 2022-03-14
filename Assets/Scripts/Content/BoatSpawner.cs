using System.Collections.Generic;
using UnityEngine;

public enum BoatSpawnerSpawnMode
{
    Random,
    SingleBoat
}

public class BoatSpawner : MonoBehaviour
{
    public GameObject prefab;
    [LogRange(-4.0f, 0.0f)] public float spawnProbability = 0.01f;
    public int maxBoats = 10;
    public Transform parentTo;
    public BoatSpawnerSpawnMode spawnMode = BoatSpawnerSpawnMode.Random;
    public SeaObjectSpawner seaObjectSpawner;

    private BoatBehaviour singleBoat = null;
    private readonly List<BoatBehaviour> boatRefs = new List<BoatBehaviour>();

    void Update()
    {
        if(spawnMode == BoatSpawnerSpawnMode.Random) {
            int boatCount = 0;

            //FIXME: Should use events. I'm just lazy, but this is terrible in terms of performances...
            for(int i = boatRefs.Count - 1; i >= 0; i--) {
                if(boatRefs[i])
                    boatCount++;
                else
                    boatRefs.RemoveAt(i);
            }

            if(Random.value < spawnProbability && (maxBoats <= 0 || boatCount < maxBoats)) {
                BoatBehaviour boat = SpawnBoat(seaObjectSpawner, prefab, parentTo);

                if(boat)
                    boatRefs.Add(boat);
            }
        } else if(spawnMode == BoatSpawnerSpawnMode.SingleBoat) {
            if(!singleBoat)
                singleBoat = SpawnBoat(seaObjectSpawner, prefab, parentTo);
        }
    }

    public static BoatBehaviour SpawnBoat(SeaObjectSpawner sos, GameObject prefab, Transform parentTo)
    {
        Vector2 localPos;
        if(!sos.FindRandomSpawnPosition(out localPos, BoatBehaviour.BOAT_RADIUS, UnityRandom.INSTANCE))
            return null;
        
        BoatBehaviour bb = Instantiate(prefab, parentTo).GetComponent<BoatBehaviour>();
        bb.transform.localPosition = new Vector3(localPos.x, 0.0f, localPos.y);

        sos.AddBoat(bb);
        return bb;
    }
}

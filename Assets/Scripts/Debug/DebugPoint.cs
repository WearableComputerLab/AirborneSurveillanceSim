using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class DebugPoint : MonoBehaviour
{
    public Material material;

    private enum PointState
    {
        Configuring,
        Alive,
        Dead
    }

    private float deathTime;
    private PointState state = PointState.Configuring;

    public void SpawnPoint(Vector3 pos, Color color, float lifeTime)
    {
        if(state == PointState.Configuring) {
            Material mat = Instantiate(material);
            mat.SetColor("_Color", color);

            transform.localPosition = pos;
            GetComponent<MeshRenderer>().material = mat;

            deathTime = Time.time + lifeTime;
            state = PointState.Alive;
        }
    }

    void Update()
    {
        if(state == PointState.Alive && Time.time >= deathTime) {
            state = PointState.Dead;
            Destroy(gameObject);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>
/// Continuously generates a 3D simplex noise texture. This is used to displace vertices vertically on a mesh grid
/// which results in waves. Using Unity's AsyncGPUReadback, the texture is also available on the CPU side, meaning
/// that components can access it in real time. This useful to make sea objects look like their are floating on the
/// sea.
///
/// Only one instance of this component shall exist in one scene.
///
/// <see cref="Floating"/>
/// </summary>
public class SeaNoiseGenerator : MonoBehaviour
{
    /// <summary>
    /// The instance of this class
    /// </summary>
    public static SeaNoiseGenerator instance { get; private set; }

    private class AsyncBlock
    {
        public readonly RenderTexture renderBuffer;
        public readonly float[] grid = new float[(GRID_SIZE + 1) * (GRID_SIZE + 1) * 4];
        public bool done;

        public AsyncBlock(RenderTexture rt)
        {
            renderBuffer = rt;
        }
    }

    public const int GRID_SIZE = 128;
    private int lastBlock = -1;
    private int nextBlock = 0;
    private readonly List<AsyncBlock> asyncBlocks = new List<AsyncBlock>();
    [SerializeField] private Shader noiseShader;
    [SerializeField] private MeshRenderer sea;
    private Material noiseMaterial;
    private Material seaMaterial;
    private int seaTexLocation;

    public int debugNumBlocks => asyncBlocks.Count;

    void Start()
    {
        seaMaterial = Instantiate(sea.material);
        sea.material = seaMaterial;

        seaTexLocation = Shader.PropertyToID("_Simplex3D");
        noiseMaterial = new Material(noiseShader);
        asyncBlocks.Add(CreateAsyncBlock());
    }

    void Update()
    {
        if(lastBlock >= 0)
            LaunchRender(asyncBlocks[lastBlock]);

        AsyncBlock next = asyncBlocks[nextBlock];

        if(next.done) {
            lastBlock = nextBlock;
            seaMaterial.SetTexture(seaTexLocation, next.renderBuffer);
            nextBlock = (nextBlock + 1) % asyncBlocks.Count;
        } else {
            //We didn't have enough blocks
            lastBlock = -1;
            seaMaterial.SetTexture(seaTexLocation, Texture2D.blackTexture);
            asyncBlocks.Add(CreateAsyncBlock());
        }
    }

    void LaunchRender(AsyncBlock ab)
    {
        ab.done = false;
        Graphics.Blit(null, ab.renderBuffer, noiseMaterial);
        
        AsyncGPUReadback.Request(ab.renderBuffer, 0, result => {
            result.GetData<float>().CopyTo(ab.grid);
            ab.done = true;
        });
    }

    AsyncBlock CreateAsyncBlock()
    {
        RenderTexture renderBuffer = new RenderTexture(GRID_SIZE + 1, GRID_SIZE + 1, 1, GraphicsFormat.R32G32B32A32_SFloat, 1);
        renderBuffer.filterMode = FilterMode.Point;
        renderBuffer.wrapMode = TextureWrapMode.Clamp;

        AsyncBlock ab = new AsyncBlock(renderBuffer);
        LaunchRender(ab);
        
        return ab;
    }

    public bool gridNoiseAvailable => lastBlock >= 0;
    private static readonly Vector4 DEFAULT_VEC = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);

    Vector4 GetGridNoiseRaw(int ix, int iy)
    {
        if(lastBlock < 0)
            return DEFAULT_VEC;
        
        if(ix < 0 || ix > GRID_SIZE || iy < 0 || iy > GRID_SIZE)
            return DEFAULT_VEC;

        float[] grid = asyncBlocks[lastBlock].grid;
        int idx = (iy * (GRID_SIZE + 1) + ix) * 4;
        
        return new Vector4(grid[idx], grid[idx + 1], grid[idx + 2], grid[idx + 3]);
    }

    /// <summary>
    /// Gets the height and normal vector of the generated noise value at the given coordinates. Values are smoothly
    /// interpolated in a bilinear fashion.
    ///
    /// If x and y are outside the grid range, default values shall be returned (0 height and up-pointing normal vector).
    /// </summary>
    /// 
    /// <param name="x">The x coordinate of the noise to retrieve</param>
    /// <param name="y">The y coordinate (world Z) of the noise to retrieve</param>
    /// <returns>The interpolated noise at the given coordinates. x is the height and (y, z, w) is the normal.</returns>
    public Vector4 GetGridNoise(float x, float y)
    {
        x /= 0.0078125f;
        y /= 0.0078125f;
        
        int ix = Mathf.FloorToInt(x);
        int iy = Mathf.FloorToInt(y);

        float fx = x - (float) ix;
        float fy = y - (float) iy;

        ix += GRID_SIZE >> 1;
        iy += GRID_SIZE >> 1;

        Vector4 a = Vector4.Lerp(GetGridNoiseRaw(ix, iy), GetGridNoiseRaw(ix + 1, iy), fx);
        Vector4 b = Vector4.Lerp(GetGridNoiseRaw(ix, iy + 1), GetGridNoiseRaw(ix + 1, iy + 1), fx);

        return Vector4.Lerp(a, b, fy);
    }

    /// <summary>
    /// Returns the computed noise texture. Use this texture in a vertex shader to displace the vertices.
    /// </summary>
    /// 
    /// <returns>The computed noise texture</returns>
    public Texture GetGridTexture()
    {
        if(lastBlock < 0)
            return Texture2D.blackTexture;

        return asyncBlocks[lastBlock].renderBuffer;
    }

    void OnEnable()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }

    void OnDisable()
    {
        instance = null;
    }
}

using System;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DigitalStory.View
{
    /// <summary>
    /// Development tool, not game code. Counts how many glow quads cover each pixel of the game camera's picture, by
    /// drawing every quad from the swarm's own buffers with a shader that adds 1 per fragment, then reading the count back.
    /// The read waits for the GPU, so call it only outside measured frames.
    /// </summary>
    /// <remarks>
    /// The real glow is depth-tested against the scene; this count is not, so quads hidden behind opaque geometry are
    /// counted too. Over open ground at the swarm that difference is small.
    /// </remarks>
    internal sealed class GlowOverdrawProbe : IDisposable
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string ShaderResource = "F010GlowOverdraw";
        private const int MostCount = 4096;
        private const float CoveredThreshold = 0.5f;

        private static readonly int StaticId = Shader.PropertyToID("_Static");
        private static readonly int DynamicId = Shader.PropertyToID("_Dynamic");
        private static readonly string[] CopiedFloats = { "_GlowSize", "_DyingSeconds", "_BuzzAmplitude", "_BuzzFrequency" };

        private readonly SwarmGpuView _swarmView;
        private readonly Camera _camera;
        private readonly Material _material;
        private readonly int[] _histogram = new int[MostCount + 1];
        private readonly CommandBuffer _commands = new CommandBuffer { name = "F010 glow overdraw" };
        private RenderTexture _target;

        public GlowOverdrawProbe(SwarmGpuView swarmView, Camera camera)
        {
            _swarmView = swarmView != null ? swarmView : throw new ArgumentNullException(nameof(swarmView));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            var shader = Resources.Load<Shader>(ShaderResource);
            if (shader == null || !shader.isSupported)
            {
                throw new InvalidOperationException($"The overdraw shader '{ShaderResource}' is missing or not supported.");
            }

            _material = new Material(shader) { name = "F010 Glow Overdraw" };
        }

        /// <summary>Draws the count and reads it back. False when the swarm has no glow draw.</summary>
        public bool TryMeasure(out GlowOverdrawResult result)
        {
            result = default;
            var mesh = (Mesh)Read("_glowMesh");
            var args = (GraphicsBuffer)Read("_glowArgsBuffer");
            var staticBuffer = (GraphicsBuffer)Read("_staticBuffer");
            var dynamicBuffer = (GraphicsBuffer)Read("_dynamicBuffer");
            var glowMaterial = (Material)Read("_glowMaterial");
            if (mesh == null || args == null || staticBuffer == null || dynamicBuffer == null || glowMaterial == null)
            {
                return false;
            }

            int width = _camera.pixelWidth;
            int height = _camera.pixelHeight;
            PrepareTarget(width, height);
            _material.SetBuffer(StaticId, staticBuffer);
            _material.SetBuffer(DynamicId, dynamicBuffer);
            foreach (string property in CopiedFloats)
            {
                _material.SetFloat(property, glowMaterial.GetFloat(property));
            }

            _commands.Clear();
            _commands.SetRenderTarget(_target);
            _commands.ClearRenderTarget(false, true, Color.clear);
            _commands.SetViewProjectionMatrices(_camera.worldToCameraMatrix, _camera.projectionMatrix);
            _commands.DrawMeshInstancedIndirect(mesh, 0, _material, 0, args, 0);
            Graphics.ExecuteCommandBuffer(_commands);

            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(_target);
            request.WaitForCompletion();
            if (request.hasError)
            {
                return false;
            }

            result = Count(request.GetData<ushort>(), width, height);
            return true;
        }

        public void Dispose()
        {
            if (_target != null)
            {
                _target.Release();
                UnityEngine.Object.Destroy(_target);
            }

            UnityEngine.Object.Destroy(_material);
            _commands.Release();
        }

        private object Read(string field)
        {
            FieldInfo info = typeof(SwarmGpuView).GetField(field, Private)
                ?? throw new InvalidOperationException($"SwarmGpuView has no field '{field}' any more. Update GlowOverdrawProbe.");
            return info.GetValue(_swarmView);
        }

        private void PrepareTarget(int width, int height)
        {
            if (_target != null && _target.width == width && _target.height == height)
            {
                return;
            }

            if (_target != null)
            {
                _target.Release();
                UnityEngine.Object.Destroy(_target);
            }

            _target = new RenderTexture(width, height, 0, RenderTextureFormat.RHalf) { name = "F010 Glow Overdraw" };
            _target.Create();
        }

        private GlowOverdrawResult Count(NativeArray<ushort> halves, int width, int height)
        {
            Array.Clear(_histogram, 0, _histogram.Length);
            long covered = 0;
            double fragments = 0.0;
            int most = 0;
            for (int index = 0; index < halves.Length; index++)
            {
                float value = Mathf.HalfToFloat(halves[index]);
                if (value < CoveredThreshold)
                {
                    continue;
                }

                int count = Mathf.Min(Mathf.RoundToInt(value), MostCount);
                covered++;
                fragments += value;
                most = Mathf.Max(most, count);
                _histogram[count]++;
            }

            return new GlowOverdrawResult(
                width, height, covered, fragments, most, Percentile(covered, 0.5), Percentile(covered, 0.95), Percentile(covered, 0.99));
        }

        private int Percentile(long covered, double share)
        {
            if (covered == 0)
            {
                return 0;
            }

            long wanted = (long)Math.Ceiling(covered * share);
            long seen = 0;
            for (int count = 1; count <= MostCount; count++)
            {
                seen += _histogram[count];
                if (seen >= wanted)
                {
                    return count;
                }
            }

            return MostCount;
        }
    }
}

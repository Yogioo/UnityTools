using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Model {
    public MeshFilter meshFilter;
}

public class VaryingData {
    public List<Vector4> sv_position = new List<Vector4> (); // positions in clip space
}

public class FragmentData {
    public List<int> index = new List<int> (); // index of color buffer and depth buffer
    public List<float> sv_depth = new List<float> (); // positions in clip space and NDC
}

public class Rasterizer {

    int width, height;
    Vector2 pixelSize;
    float[] depthBuffer;

    public Rasterizer (int width, int height) {
        this.width = width;
        this.height = height;
        pixelSize = new Vector2 (1f / width, 1f / height);

        var length = width * height;
        depthBuffer = new float[length];
    }

    public void Draw (Camera camera, Model[] models, bool reversedZ = true) {
        Clear (reversedZ ? 0f : 1f);

        var vpMatrix = GetViewProjectionMatrix (camera, reversedZ);

        foreach (var model in models) {
            var mesh = model.meshFilter.sharedMesh;
            var mvp = vpMatrix * model.meshFilter.transform.localToWorldMatrix;
            var varyings = GeometryProcessing (mesh.vertices, mvp);
            var fragmentData = ClippingAndRasterization (varyings, mesh.GetIndices (0));
            PixelProcessingAndMerge (fragmentData,  reversedZ);
        }
    }

    public Texture2D ExportDepthBuffer () {
        var result = new Texture2D (width, height);
        result.filterMode = FilterMode.Point; ;
        result.SetPixels (depthBuffer.Select (z => new Color (z, z, z, 1f)).ToArray ());
        result.Apply ();
        return result;
    }

    void Clear (float depth) {
        var length = depthBuffer.Length;
        for (int i = 0; i < length; i++) {
            depthBuffer[i] = depth;
        }
    }

    VaryingData GeometryProcessing (Vector3[] vertices, Matrix4x4 mvp) {
        var result = new VaryingData ();
        var count = vertices.Length;
        for (int i = 0; i < count; i++) {
            result.sv_position.Add (mvp * new Vector4 (vertices[i].x, vertices[i].y, vertices[i].z, 1));
        }
        return result;
    }

    FragmentData ClippingAndRasterization (VaryingData varyingData, int[] indexes) {
        var result = new FragmentData ();
        var count = indexes.Length;
        for (int i = 0; i < count; i += 3) {
            var index0 = indexes[i];
            var index1 = indexes[i + 1];
            var index2 = indexes[i + 2];
            var v0 = new Vector4 (varyingData.sv_position[index0].x, varyingData.sv_position[index0].y, varyingData.sv_position[index0].z, 1f) / varyingData.sv_position[index0].w;
            var v1 = new Vector4 (varyingData.sv_position[index1].x, varyingData.sv_position[index1].y, varyingData.sv_position[index1].z, 1f) / varyingData.sv_position[index1].w;
            var v2 = new Vector4 (varyingData.sv_position[index2].x, varyingData.sv_position[index2].y, varyingData.sv_position[index2].z, 1f) / varyingData.sv_position[index2].w;
            var area = EdgeFunction (v0, v1, v2);

            var xMin = Mathf.Max (Mathf.FloorToInt ((Mathf.Min (v0.x, v1.x, v2.x) * 0.5f + 0.5f) / pixelSize.x), 0);
            var xMax = Mathf.Min (Mathf.CeilToInt ((Mathf.Max (v0.x, v1.x, v2.x) * 0.5f + 0.5f) / pixelSize.x) + 1, width);
            var yMin = Mathf.Max (Mathf.FloorToInt ((Mathf.Min (v0.y, v1.y, v2.y) * 0.5f + 0.5f) / pixelSize.y), 0);
            var yMax = Mathf.Min (Mathf.CeilToInt ((Mathf.Max (v0.y, v1.y, v2.y) * 0.5f + 0.5f) / pixelSize.y) + 1, height);
            for (int x = xMin; x < xMax; x++) {
                for (int y = yMin; y < yMax; y++) {
                    var p = new Vector2 (x * pixelSize.x, y * pixelSize.y) + pixelSize * 0.5f;
                    p = p * 2f - new Vector2 (1f, 1f);

                    var w0 = EdgeFunction (v1, v2, p);
                    var w1 = EdgeFunction (v2, v0, p);
                    var w2 = EdgeFunction (v0, v1, p);
                    if (w0 < 0 || w1 < 0 || w2 < 0) {
                        // triangle not contains the point
                        continue;
                    }

                    w0 /= area;
                    w1 /= area;
                    w2 /= area;
                    var interpolated_z = v0.z * w0 + v1.z * w1 + v2.z * w2;
                    var interpolated_w = v0.w * w0 + v1.w * w1 + v2.w * w2;

                    if (interpolated_z < 0f || interpolated_z > 1f) {
                        // clip fragments outside the near/far planes
                        continue;
                    }

                    // perspective correct interpolation
                    // w0 = w0 * v0.w / interpolated_w;
                    // w1 = w1 * v1.w / interpolated_w;
                    // w2 = w2 * v2.w / interpolated_w;
                    // var varying = varying0 * w0 + varying1 * w1 + varying2 * w2; 

                    var depth = interpolated_z;
                    var index = y * width + x;
                    result.index.Add (index);
                    result.sv_depth.Add (depth);

                }
            }
        }
        return result;
    }

    void PixelProcessingAndMerge (FragmentData fragmentData, bool reversedZ = true) {
        var size = fragmentData.index.Count;
        for (int i = 0; i < size; i++) {
            var index = fragmentData.index[i];
            var currentZ = fragmentData.sv_depth[i];
            if (reversedZ && currentZ > depthBuffer[index]) {
                depthBuffer[index] = currentZ;
            } else if (!reversedZ && currentZ < depthBuffer[index]) {
                depthBuffer[index] = currentZ;
            }
        }
    }

    // ************** //
    // helper methods //
    // ************** //
    public static Matrix4x4 GetViewProjectionMatrix (Camera camera, bool reversedZ = true) {
        var view = camera.transform.worldToLocalMatrix;
        var project = GetProjectionMatrix (camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane, reversedZ);
        return project * view;
    }

    public static Matrix4x4 GetProjectionMatrix (float fov, float aspect, float zNear, float zFar, bool reversedZ = true) {
        var project = new Matrix4x4 ();

        float halfHeight = zNear * Mathf.Tan (Mathf.Deg2Rad * fov * 0.5f); // unity fov is vertical fov
        float halfWidth = halfHeight * aspect;

        project[0, 0] = -zNear / halfWidth;
        project[1, 1] = -zNear / halfHeight;
        project[3, 2] = -1;

        if (reversedZ) {
            // z[near, far] -> ndc[1, 0] (reversed-Z)
            project[2, 2] = zNear / (zFar - zNear);
            project[2, 3] = -1f * zFar * zNear / (zFar - zNear);
        } else {
            // z[near, far] -> ndc[0, 1]
            project[2, 2] = -1f * zFar / (zFar - zNear);
            project[2, 3] = zFar * zNear / (zFar - zNear);
        }

        return project;
    }

    static float EdgeFunction (Vector2 a, Vector2 b, Vector2 c) {
        return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
    }
}

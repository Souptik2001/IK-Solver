using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public int count = 2;
    public ComputeShader RayTracingShader;
    public Texture skyBoxTexture;
    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 color;
    }
    RenderTexture _target;
    Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void Start()
    {
        
    }

    
    void Update()
    {
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Render(destination);
    }

    void Render(RenderTexture dest)
    {
        InitRenderTexture();

        SendRenderTextureToShader();
        Graphics.Blit(_target, dest);
    }


    void SendRenderTextureToShader()
    {
        RayTracingShader.SetTexture(RayTracingShader.FindKernel("CSMain"), "Result", _target);
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(RayTracingShader.FindKernel("CSMain"), "_SkyBoxTexture", skyBoxTexture);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8);
        RayTracingShader.Dispatch(RayTracingShader.FindKernel("CSMain"), threadGroupsX, threadGroupsY, 1);
    }


    void InitRenderTexture()
    {
        if (_target==null || _target.width != Screen.width || _target.height != Screen.height)
        {
            if (_target != null) { _target.Release(); }
            _target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    void CreateScene()
    {
        Sphere[] spheres = GenerateRandomSpheres();
    }

    private Sphere[] GenerateRandomSpheres()
    {
        Sphere[] spheres = new Sphere[count];
        for (int i = 0; i < spheres.Length; i++) {
            spheres[i] = new Sphere();
            spheres[i].position = new Vector3();
            Color randomColor = UnityEngine.Random.ColorHSV();
            spheres[i].color = new Vector3(randomColor.r, randomColor.g, randomColor.b);
            spheres[i].radius = UnityEngine.Random.Range(1, 15);
           
        }
        return spheres;
    }
}

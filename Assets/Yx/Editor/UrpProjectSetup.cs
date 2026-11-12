using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yx.EditorTools
{
    public static class UrpProjectSetup
    {
        const string Folder = "Assets/Settings";
        const string RendererPath = "Assets/Settings/YxRenderer2D.asset";
        const string PipelinePath = "Assets/Settings/YxURP.asset";

        [MenuItem("余响测绘/启用 2D URP")]
        public static void EnableFromMenu()
        {
            bool ok = TryEnable(true);
            if (!ok) Debug.LogWarning("没能启用 2D URP。直接 Play 仍可使用内置着色器 Yx/Block。");
        }

        static bool TryEnable(bool log)
        {
            try
            {
                if (GraphicsSettings.defaultRenderPipeline != null) return true;
                if (File.Exists(PipelinePath)) return false;
                Type pipelineType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
                Type rendererType = Type.GetType("UnityEngine.Rendering.Universal.Renderer2DData, Unity.RenderPipelines.Universal.Runtime");
                Type baseType = Type.GetType("UnityEngine.Rendering.Universal.ScriptableRendererData, Unity.RenderPipelines.Universal.Runtime");
                if (pipelineType == null || rendererType == null || baseType == null) return false;
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Settings");
                ScriptableObject renderer = ScriptableObject.CreateInstance(rendererType);
                AssetDatabase.CreateAsset(renderer, RendererPath);
                MethodInfo create = pipelineType.GetMethod("Create", new[] { baseType });
                if (create == null)
                {
                    if (log) Debug.LogWarning("当前 URP 没有 Create(ScriptableRendererData)。");
                    return false;
                }
                var pipeline = create.Invoke(null, new object[] { renderer }) as RenderPipelineAsset;
                if (pipeline == null) return false;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                AssetDatabase.SaveAssets();
                if (log) Debug.Log("已启用余响测绘 2D URP。");
                return true;
            }
            catch (Exception e)
            {
                if (log) Debug.LogWarning(e.Message);
                return false;
            }
        }
    }
}

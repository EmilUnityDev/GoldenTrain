using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SymbolScripts
{
    public class SymbolOverlayManager : MonoBehaviour
    {
        private struct OriginalSymbolState
        {
            public int Layer;
        }

        private Dictionary<SymbolBase, OriginalSymbolState> originalSymbolStates = new Dictionary<SymbolBase, OriginalSymbolState>();

        private bool settingsChanged = false;
        private int originalMainCullingMask;
        private Camera symbolsCam;

        public void SetAllOnTop()
        {
            SymbolBase[] symbols = FindObjectsOfType<SymbolBase>();
            Setup();

            foreach (var sym in symbols)
            {
                SetToTop(sym);
            }
        }

        public void SetAllOnTopExceptWildAndScatter()
        {
            SymbolBase[] symbols = FindObjectsOfType<SymbolBase>();
            Setup();

            foreach (var sym in symbols)
            {
                if (!sym.IsWild && !sym.IsScatter)
                {
                    SetToTop(sym);
                }
                else
                {
                    RevertOne(sym);
                }
            }
        }

        public void RevertToOriginal()
        {
            if (settingsChanged)
            {
                Camera mainCam = Camera.main;
                mainCam.cullingMask = originalMainCullingMask;

                UniversalAdditionalCameraData mainData = mainCam.GetComponent<UniversalAdditionalCameraData>();
                if (mainData != null && symbolsCam != null)
                {
                    mainData.cameraStack.Remove(symbolsCam);
                    Destroy(symbolsCam.gameObject);
                    symbolsCam = null;
                }

                settingsChanged = false;
            }

            foreach (var kvp in originalSymbolStates.ToArray())
            {
                SymbolBase sym = kvp.Key;
                if (sym == null)
                {
                    originalSymbolStates.Remove(sym);
                    continue;
                }
                RevertOne(sym);
            }
        }

        private void Setup()
        {
            if (settingsChanged) return;

            Camera mainCam = Camera.main;
            originalMainCullingMask = mainCam.cullingMask;

            int specialLayer = LayerMask.NameToLayer("TransparentFX");
            mainCam.cullingMask = originalMainCullingMask & ~(1 << specialLayer);

            UniversalAdditionalCameraData mainData = mainCam.GetComponent<UniversalAdditionalCameraData>();

            if (mainData != null)
            {
                GameObject newCamGO = new GameObject("SymbolsOverlayCamera");
                symbolsCam = newCamGO.AddComponent<Camera>();
                symbolsCam.CopyFrom(mainCam);
                symbolsCam.clearFlags = CameraClearFlags.Depth;
                symbolsCam.cullingMask = 1 << specialLayer;
                symbolsCam.depth = mainCam.depth + 2;

                UniversalAdditionalCameraData newData = newCamGO.AddComponent<UniversalAdditionalCameraData>();
                newData.renderType = CameraRenderType.Overlay;

                newCamGO.transform.parent = mainCam.transform;
                newCamGO.transform.localPosition = Vector3.zero;
                newCamGO.transform.localRotation = Quaternion.identity;
                newCamGO.transform.localScale = Vector3.one;

                mainData.cameraStack.Add(symbolsCam);
            }

            settingsChanged = true;
        }

        private void SetToTop(SymbolBase sym)
        {
            if (!originalSymbolStates.ContainsKey(sym))
            {
                originalSymbolStates[sym] = new OriginalSymbolState
                {
                    Layer = sym.gameObject.layer
                };
            }

            int specialLayer = LayerMask.NameToLayer("TransparentFX");
            SetLayerRecursively(sym.transform, specialLayer);
        }

        private void SetLayerRecursively(Transform trans, int layer)
        {
            trans.gameObject.layer = layer;
            foreach (Transform child in trans)
            {
                SetLayerRecursively(child, layer);
            }
        }

        private void RevertOne(SymbolBase sym)
        {
            if (originalSymbolStates.ContainsKey(sym))
            {
                OriginalSymbolState state = originalSymbolStates[sym];
                SetLayerRecursively(sym.transform, state.Layer);
                originalSymbolStates.Remove(sym);
            }
        }
    }
}
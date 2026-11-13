using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Applies high-quality import settings the moment art arrives, so models and textures
    /// look right without manual clicking: 32-bit meshes, full-res mipmapped textures.
    /// </summary>
    public class ArtImportSettings : AssetPostprocessor
    {
        const string ArtRoot = "Assets/_Project/Art/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var m = (ModelImporter)assetImporter;
            m.indexFormat = ModelImporterIndexFormat.UInt32;   // earth/sun meshes exceed 65k vertices
            m.importNormals = ModelImporterNormals.Import;
            m.importBlendShapes = false;
            m.importCameras = false;
            m.importLights = false;
            m.meshCompression = ModelImporterMeshCompression.Off;
            m.optimizeMeshPolygons = true;
            m.optimizeMeshVertices = true;
            m.isReadable = false;
            m.addCollider = false;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot) || !assetPath.Contains("/Textures/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default;
            t.sRGBTexture = true;
            t.mipmapEnabled = true;
            t.streamingMipmaps = false;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 8;
            t.maxTextureSize = 8192;
            t.textureCompression = TextureImporterCompression.CompressedHQ;
            t.wrapMode = assetPath.Contains("stars_milkyway") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        }
    }
}

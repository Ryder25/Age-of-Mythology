﻿using AoMEngineLibrary.Graphics.Brg;
using glTFLoader.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AoMModelViewer
{
    internal static class GltfHelper
    {
        public static int PaddingBytes(Accessor.ComponentTypeEnum componentType)
        {
            switch (componentType)
            {
                case Accessor.ComponentTypeEnum.BYTE:
                case Accessor.ComponentTypeEnum.UNSIGNED_BYTE:
                    return 1;
                case Accessor.ComponentTypeEnum.SHORT:
                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                    return 2;
                case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                case Accessor.ComponentTypeEnum.FLOAT:
                default:
                    return 4;
            }
        }
    }

    public class GltfFormatter
    {
        Gltf gltf;
        readonly List<Accessor> accessors;
        readonly List<BufferView> bufferViews;
        readonly List<Node> nodes;

        public GltfFormatter()
        {
            this.gltf = new Gltf();
            accessors = new List<Accessor>();
            bufferViews = new List<BufferView>();
            nodes = new List<Node>();
        }

        public BrgFile ToBrg(Stream gltfStream)
        {
            BrgFile brg = new BrgFile();

            //var model = SharpGLTF.Schema2.ModelRoot.Read(gltfStream, new SharpGLTF.Schema2.ReadSettings());

            //var modelTemplate = SharpGLTF.Runtime.SceneTemplate.Create(model.DefaultScene, true);

            //var inst = modelTemplate.CreateInstance();

            //foreach (var drawable in inst.DrawableReferences)
            //{
            //    var gpuMesh = model.LogicalMeshes[drawable.Item1];

            //    // What to do here? How would I go about using the transforms
            //    // to get the proper location of each vertex and normal in the mesh?
            //    if (drawable.Item2 is SharpGLTF.Transforms.StaticTransform statXform)
            //    {
            //        //AwesomeEngine.DrawMesh(gpuMesh, modelMatrix, statXform.WorldMatrix);
            //    }

            //    if (drawable.Item2 is SharpGLTF.Transforms.SkinTransform skinXform)
            //    {
            //        //AwesomeEngine.DrawMesh(gpuMesh, modelMatrix, skinXform.SkinMatrices);
            //    }
            //}

            return brg;
        }

        public Gltf FromBrg(BrgFile brg, Stream bufferStream)
        {
            // TODO clear class fields

            gltf.Asset = new Asset();
            gltf.Asset.Version = "2.0";

            Scene scene = new Scene();

            gltf.Scenes = new[] { scene }; 
            gltf.Scene = 0;
            
            Node node = new Node();
            node.Mesh = 0;
            node.Name = "node";
            
            nodes.Add(node);

            // Attachpoint Mesh (X Axis Points towards the model's right side, Y Axis points up, Z Axis points forward
            // Note: the coord system has x pointing to the left, but to match how the axes of a dummy are drawn in AoM
            // We set it's coordinate negative so that it is drawn to the right of a model.
            var xAxisPrimitive = new MeshPrimitive();
            new AxisPrimitive(-0.5f, 0.05f, 0.05f, new Vector3(0, 0, 1.0f)).Serialize(xAxisPrimitive, this, bufferStream);
            var yAxisPrimitive = new MeshPrimitive();
            new AxisPrimitive(0.05f, 0.5f, 0.05f, new Vector3(0, 1.0f, 0)).Serialize(yAxisPrimitive, this, bufferStream);
            var zAxisPrimitive = new MeshPrimitive();
            new AxisPrimitive(0.05f, 0.05f, 0.5f, new Vector3(1.0f, 0, 0)).Serialize(zAxisPrimitive, this, bufferStream);
            //var axisPrimitives = new MeshPrimitive[] { xAxisPrimitive, yAxisPrimitive, zAxisPrimitive };
            glTFLoader.Schema.Mesh attachpointMesh = new glTFLoader.Schema.Mesh();
            attachpointMesh.Name = "attachpointMesh";
            attachpointMesh.Primitives = new MeshPrimitive[] { xAxisPrimitive, yAxisPrimitive, zAxisPrimitive };

            // Create materials / textures
            Sampler sampler = new Sampler();
            sampler.MinFilter = Sampler.MinFilterEnum.LINEAR;
            sampler.MagFilter = Sampler.MagFilterEnum.LINEAR;
            sampler.WrapS = Sampler.WrapSEnum.CLAMP_TO_EDGE;
            sampler.WrapT = Sampler.WrapTEnum.CLAMP_TO_EDGE;
            gltf.Samplers = new Sampler[] { sampler };

            List<glTFLoader.Schema.Image> images = new List<Image>();
            List<glTFLoader.Schema.Texture> textures = new List<glTFLoader.Schema.Texture>();
            List<glTFLoader.Schema.Material> materials = new List<glTFLoader.Schema.Material>();
            Dictionary<int, int> brgMatGltfMatIndexMap = new Dictionary<int, int>();
            var uniqueTextures = from mat in brg.Materials
                                 group mat by mat.DiffuseMapName into matGroup
                                 select (TexName: matGroup.Key, Materials: matGroup.ToList());
            foreach (var matGroup in uniqueTextures)
            {
                int texIndex = textures.Count;
                bool hasTex = false;
                if (!string.IsNullOrWhiteSpace(matGroup.TexName))
                {
                    Image im = new Image();
                    im.Name = matGroup.TexName;
                    im.Uri = Uri.EscapeUriString(matGroup.TexName + ".png");

                    glTFLoader.Schema.Texture tex = new glTFLoader.Schema.Texture();
                    tex.Name = matGroup.TexName;
                    tex.Source = images.Count;
                    tex.Sampler = 0;

                    images.Add(im);
                    textures.Add(tex);
                    hasTex = true;
                }

                foreach (var brgMat in matGroup.Materials)
                {
                    glTFLoader.Schema.Material mat = new glTFLoader.Schema.Material();
                    mat.Name = GetMaterialNameWithFlags(brgMat);
                    mat.PbrMetallicRoughness = new MaterialPbrMetallicRoughness();
                    mat.PbrMetallicRoughness.MetallicFactor = 0.2f;
                    mat.PbrMetallicRoughness.RoughnessFactor = 0.5f;

                    if (hasTex)
                    {
                        mat.PbrMetallicRoughness.BaseColorTexture = new TextureInfo();
                        mat.PbrMetallicRoughness.BaseColorTexture.Index = texIndex;
                    }
                    else
                    {
                        mat.PbrMetallicRoughness.BaseColorFactor =
                            new float[] { brgMat.DiffuseColor.X, brgMat.DiffuseColor.Y, brgMat.DiffuseColor.Z, brgMat.Opacity };
                    }

                    brgMatGltfMatIndexMap.Add(brgMat.Id, materials.Count);
                    materials.Add(mat);
                }
            }
            gltf.Images = images.ToArray();
            gltf.Textures = textures.ToArray();
            gltf.Materials = materials.ToArray();

            // Create primitives from first brg mesh
            // TODO: check if there is at least 1 mesh, and 1 face
            var primitives = (from face in brg.Meshes[0].Faces
                             group face by face.MaterialIndex into faceGroup
                             select new BrgMeshPrimitive(faceGroup.ToList(), brgMatGltfMatIndexMap[faceGroup.Key])).ToList();

            // Load mesh data
            glTFLoader.Schema.Mesh mesh = new glTFLoader.Schema.Mesh();
            mesh.Primitives = new MeshPrimitive[primitives.Count];
            for (int i = 0; i < mesh.Primitives.Length; ++i)
            {
                mesh.Primitives[i] = new MeshPrimitive();
            }
            for (int i = 0; i < brg.Meshes.Count; ++i)
            {
                for (int j = 0; j < primitives.Count; ++j)
                {
                    primitives[j].Serialize(mesh.Primitives[j], brg.Meshes[i], this, bufferStream);
                }
            }
            gltf.Meshes = new[] { mesh, attachpointMesh };

            // Create Animation
            if (brg.Meshes.Count > 1)
            {
                for (int i = 0; i < mesh.Primitives.Length; ++i)
                {
                    mesh.Primitives[i].Targets = primitives[i].Targets.ToArray();
                }

                mesh.Weights = new float[brg.Meshes.Count - 1];

                gltf.Animations = new[] { CreateAnimation(brg.Animation, mesh.Weights.Length, bufferStream) };
            }

            // Create Attachpoints
            for (int i = 0; i < brg.Meshes[0].Attachpoints.Count; ++i)
            {
                var attNode = new BrgAttachpointNode(i, 1);
                attNode.CommitData(brg.Meshes, bufferStream);

                if (gltf.Animations.Length == 0)
                {
                    attNode.CommitStructure(this, null, AnimationSampler.InterpolationEnum.LINEAR, -1);
                }
                else
                {
                    attNode.CommitStructure(this, gltf.Animations[0], AnimationSampler.InterpolationEnum.LINEAR, gltf.Animations[0].Samplers[0].Input);
                }
            }

            scene.Nodes = Enumerable.Range(0, nodes.Count).ToArray();
            gltf.Nodes = nodes.ToArray();
            gltf.BufferViews = bufferViews.ToArray();
            gltf.Accessors = accessors.ToArray();

            // Create buffer stream
            var buffer = new glTFLoader.Schema.Buffer();
            gltf.Buffers = new[] { buffer };
            buffer.ByteLength = (int)bufferStream.Length;
            buffer.Uri = "dataBuffer.bin";

            return gltf;
        }

        private string GetMaterialNameWithFlags(BrgMaterial mat)
        {
            string name = Path.GetFileNameWithoutExtension(mat.DiffuseMapName) ?? string.Empty;

            if (mat.Flags.HasFlag(BrgMatFlag.PlayerXFormColor1))
            {
                name += " colorxform1";
            }

            if (mat.Flags.HasFlag(BrgMatFlag.PixelXForm1))
            {
                name += " pixelxform1";
            }

            if (mat.Flags.HasFlag(BrgMatFlag.TwoSided))
            {
                name += " 2-sided";
            }

            return name;
        }

        private glTFLoader.Schema.Animation CreateAnimation(BrgAnimation animation, int weightCount, Stream bufferStream)
        {
            var sampler = new AnimationSampler();
            sampler.Interpolation = AnimationSampler.InterpolationEnum.LINEAR;

            CreateKeysBuffer(animation, bufferStream);
            sampler.Input = accessors.Count - 1;

            CreateWeightsBuffer(animation, weightCount, bufferStream);
            sampler.Output = accessors.Count - 1;

            var target = new AnimationChannelTarget();
            target.Node = 0;
            target.Path = AnimationChannelTarget.PathEnum.weights;

            var channel = new AnimationChannel();
            channel.Sampler = 0;
            channel.Target = target;

            var gltfAnimation = new glTFLoader.Schema.Animation();
            gltfAnimation.Samplers = new[] { sampler };
            gltfAnimation.Channels = new[] { channel };

            return gltfAnimation;
        }


        private void CreateKeysBuffer(BrgAnimation animation, Stream bufferStream)
        {
            long bufferViewOffset;
            float min = float.MaxValue;
            float max = float.MinValue;
            using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
            {
                // padding
                writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.FLOAT) - 1)]);
                bufferViewOffset = bufferStream.Length;

                for (int i = 0; i < animation.MeshKeys.Count; ++i)
                {
                    min = Math.Min(min, animation.MeshKeys[i]);
                    max = Math.Max(max, animation.MeshKeys[i]);

                    writer.Write(animation.MeshKeys[i]);
                }
            }

            BufferView posBufferView = new BufferView();
            posBufferView.Buffer = 0;
            posBufferView.ByteLength = animation.MeshKeys.Count * 4;
            posBufferView.ByteOffset = (int)bufferViewOffset;
            posBufferView.Name = "keysBufferView";

            Accessor posAccessor = new Accessor();
            posAccessor.BufferView = bufferViews.Count;
            posAccessor.ByteOffset = 0;
            posAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
            posAccessor.Count = animation.MeshKeys.Count;
            posAccessor.Max = new[] { max };
            posAccessor.Min = new[] { min };
            posAccessor.Name = "keysBufferViewAccessor";
            posAccessor.Type = Accessor.TypeEnum.SCALAR;

            bufferViews.Add(posBufferView);
            accessors.Add(posAccessor);
        }
        private void CreateWeightsBuffer(BrgAnimation animation, int weightCount, Stream bufferStream)
        {
            long bufferViewOffset;
            using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
            {
                // padding
                writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.FLOAT) - 1)]);
                bufferViewOffset = bufferStream.Length;

                for (int i = 0; i < weightCount; ++i)
                {
                    writer.Write(0.0f);
                }

                for (int i = 1; i < animation.MeshKeys.Count; ++i)
                {
                    for (int j = 0; j < weightCount; ++j)
                    {
                        writer.Write(i == j + 1 ? 1.0f : 0.0f);
                    }
                }
            }

            BufferView posBufferView = new BufferView();
            posBufferView.Buffer = 0;
            posBufferView.ByteLength = animation.MeshKeys.Count * weightCount * 4;
            posBufferView.ByteOffset = (int)bufferViewOffset;
            posBufferView.Name = "weightsBufferView";

            Accessor posAccessor = new Accessor();
            posAccessor.BufferView = bufferViews.Count;
            posAccessor.ByteOffset = 0;
            posAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
            posAccessor.Count = animation.MeshKeys.Count * weightCount;
            posAccessor.Max = new[] { 1.0f };
            posAccessor.Min = new[] { 0.0f };
            posAccessor.Name = "weightsBufferViewAccessor";
            posAccessor.Type = Accessor.TypeEnum.SCALAR;

            bufferViews.Add(posBufferView);
            accessors.Add(posAccessor);
        }

        private int PaddingBytes(Accessor.ComponentTypeEnum componentType)
        {
            switch (componentType)
            {
                case Accessor.ComponentTypeEnum.BYTE:
                case Accessor.ComponentTypeEnum.UNSIGNED_BYTE:
                    return 1;
                case Accessor.ComponentTypeEnum.SHORT:
                case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                    return 2;
                case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                case Accessor.ComponentTypeEnum.FLOAT:
                default:
                    return 4;
            }
        }

        private void FromBrgMesh(BrgMesh brgMesh)
        {
            Vector3 max = new Vector3(float.MinValue);
            Vector3 min = new Vector3(float.MaxValue);
            using (FileStream fs = File.Open("posBuffer.bin", FileMode.Create, FileAccess.Write, FileShare.Read))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                foreach (Vector3 vec in brgMesh.Vertices)
                {
                    max.X = Math.Max(max.X, vec.X);
                    max.Y = Math.Max(max.Y, vec.Y);
                    max.Z = Math.Max(max.Z, vec.Z);

                    min.X = Math.Min(min.X, vec.X);
                    min.Y = Math.Min(min.Y, vec.Y);
                    min.Z = Math.Min(min.Z, vec.Z);

                    writer.Write(vec.X);
                    writer.Write(vec.Y);
                    writer.Write(vec.Z);
                }
            }

            glTFLoader.Schema.Buffer posBuffer = new glTFLoader.Schema.Buffer();
            posBuffer.ByteLength = brgMesh.Vertices.Count * 12;
            posBuffer.Uri = "posBuffer.bin";

            BufferView posBufferView = new BufferView();
            posBufferView.Buffer = 0;
            posBufferView.ByteLength = posBuffer.ByteLength;
            posBufferView.ByteOffset = 0;
            posBufferView.ByteStride = 12;
            posBufferView.Name = "posBufferView";
            posBufferView.Target = BufferView.TargetEnum.ARRAY_BUFFER;

            Accessor posAccessor = new Accessor();
            posAccessor.BufferView = 0;
            posAccessor.ByteOffset = 0;
            posAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
            posAccessor.Count = brgMesh.Vertices.Count;
            posAccessor.Max = new[] { max.X, max.Y, max.Z };
            posAccessor.Min = new[] { min.X, min.Y, min.Z };
            posAccessor.Name = "posBufferViewAccessor";
            posAccessor.Type = Accessor.TypeEnum.VEC3;

            var faceMin = ushort.MaxValue;
            var faceMax = ushort.MinValue;
            using (FileStream fs = File.Open("indexBuffer.bin", FileMode.Create, FileAccess.Write, FileShare.Read))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                foreach (var face in brgMesh.Faces)
                {
                    faceMin = Math.Min(faceMin, face.Indices[0]);
                    faceMin = Math.Min(faceMin, face.Indices[1]);
                    faceMin = Math.Min(faceMin, face.Indices[2]);

                    faceMax = Math.Max(faceMax, face.Indices[0]);
                    faceMax = Math.Max(faceMax, face.Indices[1]);
                    faceMax = Math.Max(faceMax, face.Indices[2]);

                    writer.Write(face.Indices[0]);
                    writer.Write(face.Indices[1]);
                    writer.Write(face.Indices[2]);
                }
            }

            glTFLoader.Schema.Buffer indexBuffer = new glTFLoader.Schema.Buffer();
            indexBuffer.ByteLength = brgMesh.Faces.Count * 6;
            indexBuffer.Uri = "indexBuffer.bin";

            BufferView indexBufferView = new BufferView();
            indexBufferView.Buffer = 1;
            indexBufferView.ByteLength = indexBuffer.ByteLength;
            indexBufferView.ByteOffset = 0;
            indexBufferView.Name = "indexBufferView";
            indexBufferView.Target = BufferView.TargetEnum.ELEMENT_ARRAY_BUFFER;

            Accessor indexAccessor = new Accessor();
            indexAccessor.BufferView = 1;
            indexAccessor.ByteOffset = 0;
            indexAccessor.ComponentType = Accessor.ComponentTypeEnum.UNSIGNED_SHORT;
            indexAccessor.Count = brgMesh.Faces.Count * 3;
            indexAccessor.Max = new[] { (float)faceMax };
            indexAccessor.Min = new[] { (float)faceMin };
            indexAccessor.Name = "indexBufferViewAccessor";
            indexAccessor.Type = Accessor.TypeEnum.SCALAR;

            gltf.Buffers = new[] { posBuffer, indexBuffer };
            gltf.BufferViews = new[] { posBufferView, indexBufferView };
            gltf.Accessors = new[] { posAccessor, indexAccessor };

            MeshPrimitive meshPrimitive = new MeshPrimitive();
            meshPrimitive.Attributes = new Dictionary<string, int>();
            meshPrimitive.Attributes.Add("POSITION", 0);
            meshPrimitive.Indices = 1;
            meshPrimitive.Mode = MeshPrimitive.ModeEnum.TRIANGLES;

            var mesh = new glTFLoader.Schema.Mesh();
            mesh.Name = "mesh";
            mesh.Primitives = new[] { meshPrimitive };

            gltf.Meshes = new[] { mesh };

            Node node = new Node();
            node.Mesh = 0;
            node.Name = "node";

            gltf.Nodes = new[] { node };
        }

        private class BrgMeshPrimitive
        {
            public int MaterialIndex { get; }
            public List<Dictionary<string, int>> Targets { get; }
            public List<BrgFace> Faces { get; }
            public List<ushort> Indices { get; }

            private BrgMesh? baseMesh;

            public BrgMeshPrimitive(List<BrgFace> faces, int materialIndex)
            {
                Faces = faces;
                if (faces.Count > 0) MaterialIndex = materialIndex;
                Targets = new List<Dictionary<string, int>>();

                var mapNewIndices = new Dictionary<ushort, ushort>();
                Indices = new List<ushort>();
                foreach (BrgFace face in Faces)
                {
                    for (int i = 0; i < face.Indices.Count; ++i)
                    {
                        var oldIndex = face.Indices[i];

                        if (mapNewIndices.ContainsKey(oldIndex))
                        {
                            face.Indices[i] = mapNewIndices[oldIndex];
                        }
                        else
                        {
                            var newIndex = (ushort)Indices.Count;
                            mapNewIndices.Add(oldIndex, newIndex);
                            face.Indices[i] = newIndex;
                            Indices.Add(oldIndex);
                        }
                    }
                }
            }

            // The new indices array tells us in what order to write the vertex data
            // This is because we can't use the original mappings since the brg mesh
            // doesn't store the vertices per material, and that's what we're doing here
            // we're separating the mesh into multiple primitves per material
            public void Serialize(MeshPrimitive primitive, BrgMesh mesh, GltfFormatter formatter, Stream bufferStream)
            {
                if (!mesh.Header.Flags.HasFlag(BrgMeshFlag.SECONDARYMESH))
                {
                    baseMesh = mesh;
                    primitive.Mode = MeshPrimitive.ModeEnum.TRIANGLES;
                    CreateIndexBuffer(mesh, formatter, bufferStream);
                    primitive.Indices = formatter.accessors.Count - 1;
                    primitive.Material = MaterialIndex;

                    primitive.Attributes = CreateAttributes(mesh, formatter, bufferStream);
                }
                else
                {
                    Targets.Add(CreateAttributes(mesh, formatter, bufferStream));
                }
            }

            private Dictionary<string, int> CreateAttributes(BrgMesh mesh, GltfFormatter formatter, Stream bufferStream)
            {
                var attributes = new Dictionary<string, int>();

                CreatePositionBuffer(mesh, formatter, bufferStream);
                int posAccessor = formatter.accessors.Count - 1;
                attributes.Add("POSITION", posAccessor);

                CreateNormalBuffer(mesh, formatter, bufferStream);
                int normAccessor = formatter.accessors.Count - 1;
                attributes.Add("NORMAL", normAccessor);

                if (!mesh.Header.Flags.HasFlag(BrgMeshFlag.SECONDARYMESH))
                {
                    CreateTexcoordBuffer(mesh, formatter, bufferStream);
                    int texcoordAccessor = formatter.accessors.Count - 1;
                    attributes.Add("TEXCOORD_0", texcoordAccessor);
                }

                return attributes;
            }
            private void CreateIndexBuffer(BrgMesh mesh, GltfFormatter formatter, Stream bufferStream)
            {
                long bufferViewOffset;
                var faceMin = ushort.MaxValue;
                var faceMax = ushort.MinValue;
                using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
                {
                    // padding
                    writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.UNSIGNED_SHORT) - 1)]);
                    bufferViewOffset = bufferStream.Length;

                    foreach (var face in Faces)
                    {
                        faceMin = Math.Min(faceMin, face.Indices[0]);
                        faceMin = Math.Min(faceMin, face.Indices[1]);
                        faceMin = Math.Min(faceMin, face.Indices[2]);

                        faceMax = Math.Max(faceMax, face.Indices[0]);
                        faceMax = Math.Max(faceMax, face.Indices[1]);
                        faceMax = Math.Max(faceMax, face.Indices[2]);

                        writer.Write(face.Indices[0]);
                        writer.Write(face.Indices[2]);
                        writer.Write(face.Indices[1]);
                    }
                }
                
                BufferView indexBufferView = new BufferView();
                indexBufferView.Buffer = 0;
                indexBufferView.ByteLength = Faces.Count * 6;
                indexBufferView.ByteOffset = (int)bufferViewOffset;
                indexBufferView.Name = "indexBufferView";
                indexBufferView.Target = BufferView.TargetEnum.ELEMENT_ARRAY_BUFFER;

                Accessor indexAccessor = new Accessor();
                indexAccessor.BufferView = formatter.bufferViews.Count;
                indexAccessor.ByteOffset = 0;
                indexAccessor.ComponentType = Accessor.ComponentTypeEnum.UNSIGNED_SHORT;
                indexAccessor.Count = Faces.Count * 3;
                indexAccessor.Max = new[] { (float)faceMax };
                indexAccessor.Min = new[] { (float)faceMin };
                indexAccessor.Name = "indexBufferViewAccessor";
                indexAccessor.Type = Accessor.TypeEnum.SCALAR;

                formatter.bufferViews.Add(indexBufferView);
                formatter.accessors.Add(indexAccessor);
            }
            private void CreatePositionBuffer(BrgMesh mesh, GltfFormatter formatter, Stream bufferStream)
            {
                long bufferViewOffset;
                Vector3 max = new Vector3(float.MinValue);
                Vector3 min = new Vector3(float.MaxValue);
                using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
                {
                    // padding
                    writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.FLOAT) - 1)]);
                    bufferViewOffset = bufferStream.Length;

                    foreach (int index in Indices)
                    {
                        Vector3 vec = mesh.Vertices[index];

                        // Morph Target mesh must contain offsets
                        if (mesh.Header.Flags.HasFlag(BrgMeshFlag.SECONDARYMESH) && baseMesh != null)
                        {
                            vec -= baseMesh.Vertices[index];
                        }
                        vec.X = -vec.X;

                        max.X = Math.Max(max.X, vec.X);
                        max.Y = Math.Max(max.Y, vec.Y);
                        max.Z = Math.Max(max.Z, vec.Z);

                        min.X = Math.Min(min.X, vec.X);
                        min.Y = Math.Min(min.Y, vec.Y);
                        min.Z = Math.Min(min.Z, vec.Z);

                        writer.Write(vec.X);
                        writer.Write(vec.Y);
                        writer.Write(vec.Z);
                    }
                }

                BufferView posBufferView = new BufferView();
                posBufferView.Buffer = 0;
                posBufferView.ByteLength = Indices.Count * 12;
                posBufferView.ByteOffset = (int)bufferViewOffset;
                posBufferView.ByteStride = 12;
                posBufferView.Name = "posBufferView";
                posBufferView.Target = BufferView.TargetEnum.ARRAY_BUFFER;

                Accessor posAccessor = new Accessor();
                posAccessor.BufferView = formatter.bufferViews.Count;
                posAccessor.ByteOffset = 0;
                posAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
                posAccessor.Count = Indices.Count;
                posAccessor.Max = new[] { max.X, max.Y, max.Z };
                posAccessor.Min = new[] { min.X, min.Y, min.Z };
                posAccessor.Name = "posBufferViewAccessor";
                posAccessor.Type = Accessor.TypeEnum.VEC3;

                formatter.bufferViews.Add(posBufferView);
                formatter.accessors.Add(posAccessor);
            }
            private void CreateNormalBuffer(BrgMesh mesh, GltfFormatter formatter, Stream bufferStream)
            {
                long bufferViewOffset;
                Vector3 max = new Vector3(float.MinValue);
                Vector3 min = new Vector3(float.MaxValue);
                using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
                {
                    // padding
                    writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.FLOAT) - 1)]);
                    bufferViewOffset = bufferStream.Length;

                    foreach (int index in Indices)
                    {
                        Vector3 vec = mesh.Normals[index];
                        vec.X = -vec.X;
                        vec = vec / vec.Length();

                        max.X = Math.Max(max.X, vec.X);
                        max.Y = Math.Max(max.Y, vec.Y);
                        max.Z = Math.Max(max.Z, vec.Z);

                        min.X = Math.Min(min.X, vec.X);
                        min.Y = Math.Min(min.Y, vec.Y);
                        min.Z = Math.Min(min.Z, vec.Z);

                        writer.Write(vec.X);
                        writer.Write(vec.Y);
                        writer.Write(vec.Z);
                    }
                }

                BufferView posBufferView = new BufferView();
                posBufferView.Buffer = 0;
                posBufferView.ByteLength = Indices.Count * 12;
                posBufferView.ByteOffset = (int)bufferViewOffset;
                posBufferView.ByteStride = 12;
                posBufferView.Name = "normalBufferView";
                posBufferView.Target = BufferView.TargetEnum.ARRAY_BUFFER;

                Accessor posAccessor = new Accessor();
                posAccessor.BufferView = formatter.bufferViews.Count;
                posAccessor.ByteOffset = 0;
                posAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
                posAccessor.Count = Indices.Count;
                posAccessor.Max = new[] { max.X, max.Y, max.Z };
                posAccessor.Min = new[] { min.X, min.Y, min.Z };
                posAccessor.Name = "normalBufferViewAccessor";
                posAccessor.Type = Accessor.TypeEnum.VEC3;

                formatter.bufferViews.Add(posBufferView);
                formatter.accessors.Add(posAccessor);
            }
            private void CreateTexcoordBuffer(BrgMesh mesh, GltfFormatter formatter, Stream bufferStream)
            {
                long bufferViewOffset;
                using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
                {
                    // padding
                    writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.FLOAT) - 1)]);
                    bufferViewOffset = bufferStream.Length;

                    foreach (int index in Indices)
                    {
                        Vector2 vec = mesh.TextureCoordinates[index];

                        writer.Write(vec.X);
                        writer.Write(1.0f - vec.Y);
                    }
                }

                BufferView posBufferView = new BufferView();
                posBufferView.Buffer = 0;
                posBufferView.ByteLength = Indices.Count * 8;
                posBufferView.ByteOffset = (int)bufferViewOffset;
                posBufferView.ByteStride = 8;
                posBufferView.Name = "uvBufferView";
                posBufferView.Target = BufferView.TargetEnum.ARRAY_BUFFER;

                Accessor posAccessor = new Accessor();
                posAccessor.BufferView = formatter.bufferViews.Count;
                posAccessor.ByteOffset = 0;
                posAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
                posAccessor.Count = Indices.Count;
                posAccessor.Name = "uvBufferViewAccessor";
                posAccessor.Type = Accessor.TypeEnum.VEC2;

                formatter.bufferViews.Add(posBufferView);
                formatter.accessors.Add(posAccessor);
            }

            private int PaddingBytes(Accessor.ComponentTypeEnum componentType)
            {
                switch (componentType)
                {
                    case Accessor.ComponentTypeEnum.BYTE:
                    case Accessor.ComponentTypeEnum.UNSIGNED_BYTE:
                        return 1;
                    case Accessor.ComponentTypeEnum.SHORT:
                    case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                        return 2;
                    case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                    case Accessor.ComponentTypeEnum.FLOAT:
                    default:
                        return 4;
                }
            }
        }

        private class BrgAttachpointNode
        {
            public Node Node { get; }
            public int BrgIndex { get; }

            public AnimationChannel PositionChannel { get; }
            public AnimationChannel RotationChannel { get; }
            public AnimationChannel ScaleChannel { get; }

            public AnimationSampler PositionSampler { get; }
            public AnimationSampler RotationSampler { get; }
            public AnimationSampler ScaleSampler { get; }

            public BufferView PositionBufferView { get; }
            public BufferView RotationBufferView { get; }
            public BufferView ScaleBufferView { get; }

            public Accessor PositionAccessor { get; }
            public Accessor RotationAccessor { get; }
            public Accessor ScaleAccessor { get; }

            public BrgAttachpointNode(int attachpointIndex, int meshIndex)
            {
                BrgIndex = attachpointIndex;
                Node = new Node();
                Node.Mesh = meshIndex;

                PositionChannel = new AnimationChannel();
                PositionChannel.Target = new AnimationChannelTarget();
                PositionChannel.Target.Path = AnimationChannelTarget.PathEnum.translation;

                RotationChannel = new AnimationChannel();
                RotationChannel.Target = new AnimationChannelTarget();
                RotationChannel.Target.Path = AnimationChannelTarget.PathEnum.rotation;

                ScaleChannel = new AnimationChannel();
                ScaleChannel.Target = new AnimationChannelTarget();
                ScaleChannel.Target.Path = AnimationChannelTarget.PathEnum.scale;

                PositionSampler = new AnimationSampler();
                RotationSampler = new AnimationSampler();
                ScaleSampler = new AnimationSampler();

                PositionBufferView = new BufferView();
                RotationBufferView = new BufferView();
                ScaleBufferView = new BufferView();

                PositionAccessor = new Accessor();
                RotationAccessor = new Accessor();
                ScaleAccessor = new Accessor();
            }

            public void CommitData(List<BrgMesh> meshes, Stream bufferStream)
            {
                bool exportAnim = meshes.Count > 1;
                BrgMesh mesh = meshes[0];

                long bufferViewOffset;
                using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
                {
                    // padding
                    writer.Write(new byte[(-bufferStream.Length) & (GltfHelper.PaddingBytes(Accessor.ComponentTypeEnum.FLOAT) - 1)]);
                    bufferViewOffset = bufferStream.Length;

                    var attp = mesh.Attachpoints[BrgIndex];
                    GetAttachpointPosRotScale(attp, out Vector3 pos, out Quaternion rot, out Vector3 sca);

                    Node.Name = "Dummy_" + attp.Name;
                    Node.Translation = new float[] { pos.X, pos.Y, pos.Z };
                    Node.Rotation = new float[] { rot.X, rot.Y, rot.Z, rot.W };
                    Node.Scale = new float[] { sca.X, sca.Y, sca.Z };

                    if (exportAnim)
                    {
                        writer.Write(pos.X);
                        writer.Write(pos.Y);
                        writer.Write(pos.Z);
                        writer.Write(rot.X);
                        writer.Write(rot.Y);
                        writer.Write(rot.Z);
                        writer.Write(rot.W);
                        writer.Write(sca.X);
                        writer.Write(sca.Y);
                        writer.Write(sca.Z);

                        for (int i = 1; i < meshes.Count; ++i)
                        {
                            BrgMesh meshAnim = meshes[i];
                            attp = meshAnim.Attachpoints[BrgIndex];
                            GetAttachpointPosRotScale(attp, out pos, out rot, out sca);
                            writer.Write(pos.X);
                            writer.Write(pos.Y);
                            writer.Write(pos.Z);
                            writer.Write(rot.X);
                            writer.Write(rot.Y);
                            writer.Write(rot.Z);
                            writer.Write(rot.W);
                            writer.Write(sca.X);
                            writer.Write(sca.Y);
                            writer.Write(sca.Z);
                        }
                    }
                }

                if (exportAnim)
                {
                    PositionBufferView.Buffer = 0;
                    PositionBufferView.ByteLength = 10 * 4 * (meshes.Count);
                    PositionBufferView.ByteOffset = (int)bufferViewOffset;
                    PositionBufferView.ByteStride = 10 * 4;
                    PositionBufferView.Name = mesh.Attachpoints[BrgIndex].Name + "_attBuffView";

                    PositionAccessor.ByteOffset = 0;
                    PositionAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
                    PositionAccessor.Count = meshes.Count;
                    PositionAccessor.Name = mesh.Attachpoints[BrgIndex].Name + "_attPosAccessor";
                    PositionAccessor.Type = Accessor.TypeEnum.VEC3;

                    RotationAccessor.ByteOffset = 3 * 4;
                    RotationAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
                    RotationAccessor.Count = meshes.Count;
                    RotationAccessor.Name = mesh.Attachpoints[BrgIndex].Name + "_attRotAccessor";
                    RotationAccessor.Type = Accessor.TypeEnum.VEC4;

                    ScaleAccessor.ByteOffset = 7 * 4;
                    ScaleAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
                    ScaleAccessor.Count = meshes.Count;
                    ScaleAccessor.Name = mesh.Attachpoints[BrgIndex].Name + "_attScaAccessor";
                    ScaleAccessor.Type = Accessor.TypeEnum.VEC3;
                }
            }

            public void CommitStructure(GltfFormatter formatter, glTFLoader.Schema.Animation? animation, AnimationSampler.InterpolationEnum samplerInterpolation, int animKeyAccessorIndex)
            {
                int nodeIndex = formatter.nodes.Count;
                formatter.nodes.Add(Node);

                if (animation != null)
                {
                    // Buffer Views
                    int buffViewIndex = formatter.bufferViews.Count;
                    formatter.bufferViews.Add(PositionBufferView);

                    // Accessors
                    int posAccIndex = formatter.accessors.Count;
                    int rotAccIndex = posAccIndex + 1;
                    int scaAccIndex = rotAccIndex + 1;
                    formatter.accessors.Add(PositionAccessor);
                    formatter.accessors.Add(RotationAccessor);
                    formatter.accessors.Add(ScaleAccessor);

                    PositionAccessor.BufferView = buffViewIndex;
                    RotationAccessor.BufferView = buffViewIndex;
                    ScaleAccessor.BufferView = buffViewIndex;

                    // Animation
                    List<AnimationChannel> channels = new List<AnimationChannel>(animation.Channels);
                    List<AnimationSampler> samplers = new List<AnimationSampler>(animation.Samplers);

                    // Samplers
                    int posSamIndex = samplers.Count;
                    int rotSamIndex = posSamIndex + 1;
                    int scaSamIndex = rotSamIndex + 1;
                    samplers.Add(PositionSampler);
                    samplers.Add(RotationSampler);
                    samplers.Add(ScaleSampler);

                    PositionSampler.Input = animKeyAccessorIndex;
                    PositionSampler.Output = posAccIndex;
                    PositionSampler.Interpolation = samplerInterpolation;
                    RotationSampler.Input = animKeyAccessorIndex;
                    RotationSampler.Output = rotAccIndex;
                    RotationSampler.Interpolation = samplerInterpolation;
                    ScaleSampler.Input = animKeyAccessorIndex;
                    ScaleSampler.Output = scaAccIndex;
                    ScaleSampler.Interpolation = samplerInterpolation;

                    // Channels
                    channels.Add(PositionChannel);
                    channels.Add(RotationChannel);
                    channels.Add(ScaleChannel);

                    PositionChannel.Sampler = posSamIndex;
                    PositionChannel.Target.Node = nodeIndex;
                    RotationChannel.Sampler = rotSamIndex;
                    RotationChannel.Target.Node = nodeIndex;
                    ScaleChannel.Sampler = scaSamIndex;
                    ScaleChannel.Target.Node = nodeIndex;

                    animation.Channels = channels.ToArray();
                    animation.Samplers = samplers.ToArray();
                }
            }

            private void GetAttachpointPosRotScale(BrgAttachpoint attp, out Vector3 position, out Quaternion rotation, out Vector3 scale)
            {
                Matrix4x4 mat = new Matrix4x4();
                mat.M11 = attp.Right.X; mat.M12 = attp.Right.Y; mat.M13 = attp.Right.Z;
                mat.M21 = attp.Up.X; mat.M22 = attp.Up.Y; mat.M23 = attp.Up.Z;
                mat.M31 = attp.Forward.X; mat.M32 = attp.Forward.Y; mat.M33 = attp.Forward.Z;
                mat.M44 = 1;

                bool suc = Matrix4x4.Decompose(mat, out scale, out Quaternion rot, out _);
                rot = Quaternion.Normalize(rot);

                if (!suc || scale.X < 0 || scale.Y < 0 || scale.Z < 0)
                    throw new Exception();

                position = new Vector3(-attp.Position.X, attp.Position.Y, attp.Position.Z);
                rotation = rot;
            }
        }


        private class AxisPrimitive
        {
            public List<BrgFace> Faces { get; }
            public List<Vector3> Vertices { get; }
            public Vector3 Color { get; }

            public AxisPrimitive(float xLength, float yLength, float zLength, Vector3 color)
            {
                Color = color;

                Vertices = new List<Vector3>()
                {
                    new Vector3(0, 0, 0),
                    new Vector3(xLength, 0, 0),
                    new Vector3(xLength, yLength, 0),
                    new Vector3(0, yLength, 0),
                    new Vector3(0, yLength, zLength),
                    new Vector3(xLength, yLength, zLength),
                    new Vector3(xLength, 0, zLength),
                    new Vector3(0, 0, zLength)
                };

                Faces = new List<BrgFace>()
                {
                    new BrgFace() { Indices = new List<ushort>() { 0, 2, 1 } },
                    new BrgFace() { Indices = new List<ushort>() { 0, 3, 2 } },
                    new BrgFace() { Indices = new List<ushort>() { 2, 3, 4 } },
                    new BrgFace() { Indices = new List<ushort>() { 2, 4, 5 } },
                    new BrgFace() { Indices = new List<ushort>() { 1, 2, 5 } },
                    new BrgFace() { Indices = new List<ushort>() { 1, 5, 6 } },
                    new BrgFace() { Indices = new List<ushort>() { 0, 7, 4 } },
                    new BrgFace() { Indices = new List<ushort>() { 0, 4, 3 } },
                    new BrgFace() { Indices = new List<ushort>() { 5, 4, 7 } },
                    new BrgFace() { Indices = new List<ushort>() { 5, 7, 6 } },
                    new BrgFace() { Indices = new List<ushort>() { 0, 6, 7 } },
                    new BrgFace() { Indices = new List<ushort>() { 0, 1, 6 } }
                };
            }

            public void Serialize(MeshPrimitive primitive, GltfFormatter formatter, Stream bufferStream)
            {
                primitive.Mode = MeshPrimitive.ModeEnum.TRIANGLES;
                CreateIndexBuffer(formatter, bufferStream);
                primitive.Indices = formatter.accessors.Count - 1;
                //primitive.Material = MaterialIndex;

                primitive.Attributes = CreateAttributes(formatter, bufferStream);
            }

            private Dictionary<string, int> CreateAttributes(GltfFormatter formatter, Stream bufferStream)
            {
                var attributes = new Dictionary<string, int>();

                CreatePositionBuffer(formatter, bufferStream);
                int posAccessor = formatter.accessors.Count - 1;
                attributes.Add("POSITION", posAccessor);

                CreateColorBuffer(formatter, bufferStream);
                int normAccessor = formatter.accessors.Count - 1;
                attributes.Add("COLOR_0", normAccessor);

                return attributes;
            }
            private void CreateIndexBuffer(GltfFormatter formatter, Stream bufferStream)
            {
                long bufferViewOffset;
                var faceMin = ushort.MaxValue;
                var faceMax = ushort.MinValue;
                using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
                {
                    // padding
                    writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.UNSIGNED_SHORT) - 1)]);
                    bufferViewOffset = bufferStream.Length;

                    foreach (var face in Faces)
                    {
                        faceMin = Math.Min(faceMin, face.Indices[0]);
                        faceMin = Math.Min(faceMin, face.Indices[1]);
                        faceMin = Math.Min(faceMin, face.Indices[2]);

                        faceMax = Math.Max(faceMax, face.Indices[0]);
                        faceMax = Math.Max(faceMax, face.Indices[1]);
                        faceMax = Math.Max(faceMax, face.Indices[2]);

                        writer.Write(face.Indices[0]);
                        writer.Write(face.Indices[1]);
                        writer.Write(face.Indices[2]);
                    }
                }

                BufferView indexBufferView = new BufferView();
                indexBufferView.Buffer = 0;
                indexBufferView.ByteLength = Faces.Count * 6;
                indexBufferView.ByteOffset = (int)bufferViewOffset;
                indexBufferView.Name = "indexBufferView";
                indexBufferView.Target = BufferView.TargetEnum.ELEMENT_ARRAY_BUFFER;

                Accessor indexAccessor = new Accessor();
                indexAccessor.BufferView = formatter.bufferViews.Count;
                indexAccessor.ByteOffset = 0;
                indexAccessor.ComponentType = Accessor.ComponentTypeEnum.UNSIGNED_SHORT;
                indexAccessor.Count = Faces.Count * 3;
                indexAccessor.Max = new[] { (float)faceMax };
                indexAccessor.Min = new[] { (float)faceMin };
                indexAccessor.Name = "indexBufferViewAccessor";
                indexAccessor.Type = Accessor.TypeEnum.SCALAR;

                formatter.bufferViews.Add(indexBufferView);
                formatter.accessors.Add(indexAccessor);
            }
            private void CreatePositionBuffer(GltfFormatter formatter, Stream bufferStream)
            {
                long bufferViewOffset;
                Vector3 max = new Vector3(float.MinValue);
                Vector3 min = new Vector3(float.MaxValue);
                using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
                {
                    // padding
                    writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.FLOAT) - 1)]);
                    bufferViewOffset = bufferStream.Length;

                    for (int i = 0; i < Vertices.Count; ++i)
                    {
                        Vector3 vec = Vertices[i];

                        max.X = Math.Max(max.X, vec.X);
                        max.Y = Math.Max(max.Y, vec.Y);
                        max.Z = Math.Max(max.Z, vec.Z);

                        min.X = Math.Min(min.X, vec.X);
                        min.Y = Math.Min(min.Y, vec.Y);
                        min.Z = Math.Min(min.Z, vec.Z);

                        writer.Write(vec.X);
                        writer.Write(vec.Y);
                        writer.Write(vec.Z);
                    }
                }

                BufferView posBufferView = new BufferView();
                posBufferView.Buffer = 0;
                posBufferView.ByteLength = Vertices.Count * 12;
                posBufferView.ByteOffset = (int)bufferViewOffset;
                posBufferView.ByteStride = 12;
                posBufferView.Name = "posBufferView";
                posBufferView.Target = BufferView.TargetEnum.ARRAY_BUFFER;

                Accessor posAccessor = new Accessor();
                posAccessor.BufferView = formatter.bufferViews.Count;
                posAccessor.ByteOffset = 0;
                posAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
                posAccessor.Count = Vertices.Count;
                posAccessor.Max = new[] { max.X, max.Y, max.Z };
                posAccessor.Min = new[] { min.X, min.Y, min.Z };
                posAccessor.Name = "posBufferViewAccessor";
                posAccessor.Type = Accessor.TypeEnum.VEC3;

                formatter.bufferViews.Add(posBufferView);
                formatter.accessors.Add(posAccessor);
            }
            private void CreateColorBuffer(GltfFormatter formatter, Stream bufferStream)
            {
                long bufferViewOffset;
                Vector3 max = new Vector3(float.MinValue);
                Vector3 min = new Vector3(float.MaxValue);
                using (BinaryWriter writer = new BinaryWriter(bufferStream, Encoding.UTF8, true))
                {
                    // padding
                    writer.Write(new byte[(-bufferStream.Length) & (PaddingBytes(Accessor.ComponentTypeEnum.FLOAT) - 1)]);
                    bufferViewOffset = bufferStream.Length;
                    
                    for (int i = 0; i < Vertices.Count; ++i)
                    {
                        max.X = Math.Max(max.X, Color.X);
                        max.Y = Math.Max(max.Y, Color.Y);
                        max.Z = Math.Max(max.Z, Color.Z);

                        min.X = Math.Min(min.X, Color.X);
                        min.Y = Math.Min(min.Y, Color.Y);
                        min.Z = Math.Min(min.Z, Color.Z);

                        writer.Write(Color.X);
                        writer.Write(Color.Y);
                        writer.Write(Color.Z);
                    }
                }

                BufferView posBufferView = new BufferView();
                posBufferView.Buffer = 0;
                posBufferView.ByteLength = Vertices.Count * 12;
                posBufferView.ByteOffset = (int)bufferViewOffset;
                posBufferView.ByteStride = 12;
                posBufferView.Name = "colorBufferView";
                posBufferView.Target = BufferView.TargetEnum.ARRAY_BUFFER;

                Accessor posAccessor = new Accessor();
                posAccessor.BufferView = formatter.bufferViews.Count;
                posAccessor.ByteOffset = 0;
                posAccessor.ComponentType = Accessor.ComponentTypeEnum.FLOAT;
                posAccessor.Count = Vertices.Count;
                posAccessor.Name = "colorBufferViewAccessor";
                posAccessor.Type = Accessor.TypeEnum.VEC3;

                formatter.bufferViews.Add(posBufferView);
                formatter.accessors.Add(posAccessor);
            }

            private int PaddingBytes(Accessor.ComponentTypeEnum componentType)
            {
                switch (componentType)
                {
                    case Accessor.ComponentTypeEnum.BYTE:
                    case Accessor.ComponentTypeEnum.UNSIGNED_BYTE:
                        return 1;
                    case Accessor.ComponentTypeEnum.SHORT:
                    case Accessor.ComponentTypeEnum.UNSIGNED_SHORT:
                        return 2;
                    case Accessor.ComponentTypeEnum.UNSIGNED_INT:
                    case Accessor.ComponentTypeEnum.FLOAT:
                    default:
                        return 4;
                }
            }
        }
    }
}

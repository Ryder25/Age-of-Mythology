﻿using AoMEngineLibrary.Graphics.Brg;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace AoMModelEditor.Models.Brg
{
    public class BrgMeshViewModel : ReactiveObject, IModelObject
    {
        private readonly BrgFile _brg;
        private readonly BrgMesh _mesh;

        public string Name => "Mesh";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public int VertexCount
        {
            get => _mesh.Vertices.Count;
        }

        public int FaceCount
        {
            get => _mesh.Faces.Count;
        }

        public int FrameCount => _brg.Meshes.Count;

        public float AnimLength
        {
            get => _mesh.ExtendedHeader.AnimationLength;
        }

        public Vector3 HotspotPosition
        {
            get => _mesh.Header.HotspotPosition;
            set
            {
                _mesh.Header.HotspotPosition = value;
                this.RaisePropertyChanged(nameof(HotspotPosition));
            }
        }

        public Vector3 CenterPosition
        {
            get => _mesh.Header.CenterPosition;
            set
            {
                _mesh.Header.CenterPosition = value;
                this.RaisePropertyChanged(nameof(CenterPosition));
            }
        }

        public BrgMeshFlag Flags
        {
            get => _mesh.Header.Flags;
            set
            {
                _mesh.Header.Flags = value;
                _brg.UpdateMeshSettings();
                this.RaisePropertyChanged(nameof(Flags));
            }
        }

        public BrgMeshFormat Flags2
        {
            get => _mesh.Header.Format;
            set
            {
                _mesh.Header.Format = value;
                _brg.UpdateMeshSettings();
                this.RaisePropertyChanged(nameof(Flags2));
            }
        }

        public BrgMeshAnimType AnimationType
        {
            get => _mesh.Header.AnimationType;
            set
            {
                _mesh.Header.AnimationType = value;
                _brg.UpdateMeshSettings();
                this.RaisePropertyChanged(nameof(AnimationType));
            }
        }

        public BrgMeshInterpolationType InterpolationType
        {
            get => _mesh.Header.InterpolationType;
            set
            {
                _mesh.Header.InterpolationType = value;
                _brg.UpdateMeshSettings();
                this.RaisePropertyChanged(nameof(InterpolationType));
            }
        }

        public BrgMeshViewModel(BrgFile brg, BrgMesh mesh)
        {
            _brg = brg;
            _mesh = mesh;
        }
    }
}

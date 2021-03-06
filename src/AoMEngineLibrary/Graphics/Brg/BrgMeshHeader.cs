﻿namespace AoMEngineLibrary.Graphics.Brg
{
    using System;
    using System.Numerics;

    public class BrgMeshHeader
    {
        public ushort Version { get; set; }
        public BrgMeshFormat Format { get; set; }
        public ushort NumVertices { get; set; }
        public ushort NumFaces { get; set; }
        public BrgMeshInterpolationType InterpolationType { get; set; }
        public BrgMeshAnimType AnimationType { get; set; }
        public ushort UserDataEntryCount { get; set; }
        public Vector3 CenterPosition { get; set; }
        public Single CenterRadius { get; set; }
        public Vector3 MassPosition { get; set; }
        public Vector3 HotspotPosition { get; set; }
        public ushort ExtendedHeaderSize { get; set; }
        public BrgMeshFlag Flags { get; set; }
        public Vector3 MinimumExtent { get; set; }
        public Vector3 MaximumExtent { get; set; }

        public BrgMeshHeader()
        {

        }
        public BrgMeshHeader(BrgBinaryReader reader)
        {
            this.Version = reader.ReadUInt16();
            this.Format = (BrgMeshFormat)reader.ReadInt16();
            this.NumVertices = reader.ReadUInt16();
            this.NumFaces = reader.ReadUInt16();
            this.InterpolationType = (BrgMeshInterpolationType)reader.ReadByte();
            this.AnimationType = (BrgMeshAnimType)reader.ReadByte();
            this.UserDataEntryCount = reader.ReadUInt16();
            this.CenterPosition = reader.ReadVector3D(false);
            this.CenterRadius = reader.ReadSingle();
            this.MassPosition = reader.ReadVector3D(false);
            this.HotspotPosition = reader.ReadVector3D(false);
            this.ExtendedHeaderSize = reader.ReadUInt16();
            this.Flags = (BrgMeshFlag)reader.ReadInt16();
            this.MinimumExtent = reader.ReadVector3D(false);
            this.MaximumExtent = reader.ReadVector3D(false);
        }

        public void Write(BrgBinaryWriter writer)
        {
            writer.Write(this.Version);
            writer.Write((UInt16)this.Format);
            writer.Write(this.NumVertices);
            writer.Write(this.NumFaces);
            writer.Write((Byte)this.InterpolationType);
            writer.Write((Byte)this.AnimationType);
            writer.Write(this.UserDataEntryCount);
            writer.WriteVector3D(this.CenterPosition, false);
            writer.Write(this.CenterRadius);//unknown03
            writer.WriteVector3D(this.MassPosition, false);
            writer.WriteVector3D(this.HotspotPosition, false);
            writer.Write(this.ExtendedHeaderSize);
            writer.Write((UInt16)this.Flags);
            writer.WriteVector3D(this.MinimumExtent, false);
            writer.WriteVector3D(this.MaximumExtent, false);
        }
    }
}

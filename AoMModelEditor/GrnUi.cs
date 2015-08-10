﻿namespace AoMModelEditor
{
    using AoMEngineLibrary.Graphics;
    using AoMEngineLibrary.Graphics.Grn;
    using AoMEngineLibrary.Graphics.Model;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public class GrnUi : IModelUi
    {
        public GrnFile File { get; set; }
        public MainForm Plugin { get; set; }
        public string FileName { get; set; }
        public int FilterIndex { get { return 2; } }

        private GrnExportSetting ExportSetting { get; set; }

        public GrnUi(MainForm plugin)
        {
            this.File = new GrnFile();
            this.FileName = "Untitled";
            this.Plugin = plugin;
            this.ExportSetting = GrnExportSetting.Model;
        }

        #region Setup
        public void Read(FileStream stream)
        {
            this.File = new GrnFile();
            this.File.Read(stream);
            this.FileName = stream.Name;
        }
        public void Write(FileStream stream)
        {
            this.File.Write(stream);
            this.FileName = stream.Name;
        }
        public void Clear()
        {
            this.File = new GrnFile();
            this.FileName = Path.GetDirectoryName(this.FileName) + "\\Untitled";
        }
        #endregion

        #region Import/Export
        public void Import()
        {

        }

        public void Export()
        {
            this.Clear();

        }
        #endregion

        #region UI
        public void LoadUi()
        {
            this.Plugin.Text = MainForm.PluginTitle + " - " + Path.GetFileName(this.FileName);

            this.LoadDataExtensions();

            if (this.File.Meshes.Count > 0)
            {
                this.Plugin.vertsValueToolStripStatusLabel.Text = this.File.Meshes[0].Vertices.Count.ToString();
                this.Plugin.facesValueToolStripStatusLabel.Text = this.File.Meshes[0].Faces.Count.ToString();
            }
            else
            {
                this.Plugin.vertsValueToolStripStatusLabel.Text = "0";
                this.Plugin.facesValueToolStripStatusLabel.Text = "0";
            }
            this.Plugin.meshesValueToolStripStatusLabel.Text = this.File.Meshes.Count.ToString();
            this.Plugin.matsValueToolStripStatusLabel.Text = this.File.Materials.Count.ToString();
            this.Plugin.animLengthValueToolStripStatusLabel.Text = this.File.Animation.Duration.ToString();

            this.Plugin.grnExportModelCheckBox.Checked = this.ExportSetting.HasFlag(GrnExportSetting.Model);
            this.Plugin.grnExportAnimCheckBox.Checked = this.ExportSetting.HasFlag(GrnExportSetting.Animation);
        }
        private void LoadDataExtensions()
        {
            this.Plugin.grnObjectsListBox.Items.Clear();
            for (int i = 0; i < this.File.DataExtensions.Count; ++i)
            {
                this.Plugin.grnObjectsListBox.Items.Add(this.File.GetDataExtensionObjectName(i));
            }
        }

        public void SaveUi()
        {
            // Export Settings
            this.ExportSetting = (GrnExportSetting)0;
            if (this.Plugin.grnExportModelCheckBox.Checked)
            {
                this.ExportSetting |= GrnExportSetting.Model;
            }
            if (this.Plugin.grnExportAnimCheckBox.Checked)
            {
                this.ExportSetting |= GrnExportSetting.Animation;
            }
        }
        #endregion

        [Flags]
        private enum GrnExportSetting
        {
            Model = 0x1,
            Animation = 0x2
        }
    }
}

﻿using AoMModelEditor.Dialogs;
using AoMModelEditor.Models;
using AoMModelEditor.Settings;
using Microsoft.Win32;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Text;
using System.Windows;

namespace AoMModelEditor
{
    public class MainViewModel : ReactiveObject
    {
        private readonly AppSettings _appSettings;
        private readonly FileDialogService _fileDialogService;

        public string Title { get; private set; }

        public ModelsViewModel ModelsViewModel { get; }

        public ReactiveCommand<Unit, Unit> OpenCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public MainViewModel()
        {
            Title = Properties.Resources.AppTitleLong;
            _appSettings = new AppSettings();
            _appSettings.Read();

            _fileDialogService = new FileDialogService();

            ModelsViewModel = new ModelsViewModel(_appSettings, _fileDialogService);

            OpenCommand = ReactiveCommand.Create(Open);
            SaveCommand = ReactiveCommand.Create(Save,
                ModelsViewModel.WhenAnyValue(vm => vm.IsBrg, vm => vm.IsGrn, (b, g) => b || g));
        }

        private void Open()
        {
            try
            {
                var ofd = _fileDialogService.GetModelOpenFileDialog();
                ofd.Filter = "Model files (*.brg, *.grn)|*.brg;*.grn|All files (*.*)|*.*";

                if (!string.IsNullOrEmpty(_appSettings.OpenFileDialogFileName) &&
                    Directory.Exists(_appSettings.OpenFileDialogFileName))
                {
                    ofd.InitialDirectory = _appSettings.OpenFileDialogFileName;
                }
                //else if (!string.IsNullOrEmpty(_lastFilePath))
                //{
                //    var lastDir = Path.GetDirectoryName(_lastFilePath);
                //    if (Directory.Exists(lastDir))
                //        ofd.InitialDirectory = lastDir;
                //}
                //ofd.FileName = Path.GetFileNameWithoutExtension(_lastFilePath);

                var dr = ofd.ShowDialog();
                if (dr.HasValue && dr == true)
                {
                    ModelsViewModel.Load(ofd.FileName);
                    Title = Properties.Resources.AppTitleShort + " - " + Path.GetFileName(ofd.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open file.{Environment.NewLine}{ex.Message}", Properties.Resources.AppTitleLong,
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private void Save()
        {
            try
            {
                var sfd = _fileDialogService.GetModelSaveFileDialog();
                var openFilePath = _fileDialogService.GetModelOpenFileDialog().FileName;

                if (ModelsViewModel.IsBrg)
                {
                    sfd.Filter = "Brg files (*.brg)|*.brg|All files (*.*)|*.*";
                }
                else
                {
                    sfd.Filter = "Grn files (*.grn)|*.grn|All files (*.*)|*.*";
                }

                // Setup starting directory and file name
                if (!string.IsNullOrEmpty(_appSettings.SaveFileDialogFileName) && Directory.Exists(_appSettings.SaveFileDialogFileName))
                {
                    sfd.InitialDirectory = _appSettings.SaveFileDialogFileName;
                }
                else if (!string.IsNullOrEmpty(openFilePath))
                {
                    var lastDir = Path.GetDirectoryName(openFilePath);
                    if (Directory.Exists(lastDir))
                        sfd.InitialDirectory = lastDir;

                    var lastFileName = Path.GetFileNameWithoutExtension(openFilePath);
                    if (!string.IsNullOrEmpty(lastFileName))
                        sfd.FileName = lastFileName;
                }

                var dr = sfd.ShowDialog();
                if (dr.HasValue && dr == true)
                {
                    ModelsViewModel.Save(sfd.FileName);
                    Title = Properties.Resources.AppTitleShort + " - " + Path.GetFileName(sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save file.{Environment.NewLine}{ex.Message}", Properties.Resources.AppTitleLong,
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
        }
    }
}

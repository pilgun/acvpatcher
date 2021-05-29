﻿using Avalonia.Controls;
using QuestPatcher.Core.Modding;
using QuestPatcher.Core.Patching;
using QuestPatcher.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestPatcher.ViewModels.Modding
{
    public class ModListViewModel : ViewModelBase
    {
        public string Title { get; }

        public bool ShowBrowse { get; }

        public OperationLocker Locker { get; }
        public ObservableCollection<ModViewModel> DisplayedMods { get; } = new();

        private readonly BrowseImportManager _browseManager;

        public ModListViewModel(string title, bool showBrowse, ObservableCollection<Mod> mods, ModManager modManager, PatchingManager patchingManager, Window mainWindow, OperationLocker locker, BrowseImportManager browseManager)
        {
            Title = title;
            ShowBrowse = showBrowse;
            Locker = locker;
            _browseManager = browseManager;

            // There's probably a better way to create my ModViewModel for the mods in this ObservableCollection
            // If there if, please tell me/PR it.
            // I can't just use the mods directly because I want to add prompts for installing/uninstalling (e.g. incorrect game version)
            mods.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems != null)
                {
                    foreach (Mod mod in args.NewItems)
                    {
                        DisplayedMods.Add(new ModViewModel(mod, modManager, patchingManager, mainWindow, locker));
                    }
                }
                if(args.OldItems != null)
                {
                    foreach (Mod mod in args.OldItems)
                    {
                        DisplayedMods.Remove(DisplayedMods.Where((modView) => modView.Inner == mod).Single());
                    }
                }
            };
        }

        public async void OnBrowseClick()
        {
            await _browseManager.ShowModsBrowse();
        }
    }
}
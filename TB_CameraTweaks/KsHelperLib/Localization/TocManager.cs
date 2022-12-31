﻿using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TB_CameraTweaks.KsHelperLib.Logger;
using Timberborn.SingletonSystem;

namespace TB_CameraTweaks.KsHelperLib.Localization
{
    internal class TocManager : ILoadableSingleton

    {
        private readonly TocLangFileHandler FileHandler;
        private readonly Dictionary<string, IEnumerable<TocEntryModel>> PredefinedLanguages = new Dictionary<string, IEnumerable<TocEntryModel>>();
        private bool Initialized;
        private string LangDirPath;
        private List<string> AllLanguageTags = new List<string>();
        private LogProxy Log = new LogProxy("TocManager ", LogLevel.None);

        public TocManager()
        {
            FileHandler = new TocLangFileHandler();
        }

        public void Load()
        {
#if (!DEBUG)
            return;
#endif
            Log.LogDebug("Load()");
            Initialize();
            Log.LogDebug("Initialized");
            Log.LogInfo("Check Each Language");
            AllLanguageTags.ForEach(t => CheckFile(t));
        }

        private void Initialize()
        {
            if (Initialized) { return; }
            CreateLangFolder();
            GetPredefinedLanguagesByReflection();
            GetAllLanguageTags();
            CheckIfDefaultLanguageIsFound();
            Initialized = true;
        }

        private void CreateLangFolder()
        {
            string langPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\lang\\";
            DirectoryInfo langDir = new DirectoryInfo(langPath);
            if (!langDir.Exists) langDir.Create();
            LangDirPath = langDir.Exists ? langDir.FullName : throw new DirectoryNotFoundException($"Couldn't create folder: {langDir.FullName}");
            Log.LogDebug($"Language Folder Created: {langDir.FullName}");
        }

        private void GetPredefinedLanguagesByReflection()
        {
            var languages = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(ILanguage).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

            foreach (var language in languages)
            {
                var l = (ILanguage)Activator.CreateInstance(language);

                List<TocEntryModel> entriesWithTag = new List<TocEntryModel>();
                foreach (var entry in l.GetEntries())
                {
                    string keyWithTag = TocConfig.TocTag + "." + entry.Key;
                    TocEntryModel fixedEntry = new TocEntryModel(keyWithTag, entry.Text, entry.Comment);
                    entriesWithTag.Add(fixedEntry);
                }
                bool successfullyAdded = PredefinedLanguages.TryAdd(l.Tag, entriesWithTag);
                if (!successfullyAdded) throw new Exception($"Failed to add language: {l.Tag}");
            }
            Log.LogDebug($"Found {PredefinedLanguages.Count} Languages By Reflection");
        }

        private void GetAllLanguageTags()
        {
            AllLanguageTags.Add(TocConfig.DefaultLanguage);
            Log.LogDebug($"Total Language Tags: {AllLanguageTags.Count} - Added Default");

            foreach (var predefinedLanguage in PredefinedLanguages)
            {
                if (AllLanguageTags.Contains(predefinedLanguage.Key)) continue;
                AllLanguageTags.Add(predefinedLanguage.Key);
                Log.LogMessage($"Language Tags, Added: {predefinedLanguage}");
            }
            Log.LogDebug($"Total Language Tags: {AllLanguageTags.Count} - Added Predefined");

            foreach (var additionalLanguage in TocConfig.GetLanguages())
            {
                if (AllLanguageTags.Contains(additionalLanguage)) continue;
                AllLanguageTags.Add(additionalLanguage);
                Log.LogMessage($"Language Tags, Added: {additionalLanguage}");
            }
            Log.LogDebug($"Total Language Tags: {AllLanguageTags.Count} - Added Additional");
        }

        private void CheckIfDefaultLanguageIsFound()
        {
            if (PredefinedLanguages.ContainsKey(TocConfig.DefaultLanguage)) return;
            throw new Exception($"Default language {TocConfig.DefaultLanguage} is missing the predefined language, add a new class with the <ILanguage> interface");
        }

        private void CheckFile(string langTag)
        {
            Log.LogDebug($"CheckFile, Tag: {langTag}");

            FileInfo langFile = new($"{LangDirPath}\\{langTag}.txt");
            Log.LogDebug($"CheckFile, File: {langFile.Name}");

            if (PredefinedLanguages.ContainsKey(langTag))
            {
                Log.LogDebug("CheckFile, Predefined Language Exists");

                var updatedEntries = PredefinedLanguages[langTag];
                WriteUpdateToFile(langFile, updatedEntries);
                //VerifyLangFileContent(langFile, updatedEntries, true);
            }
            else
            {
                Log.LogDebug("CheckFile, Predefined Language Doesn't Exist");
                var updatedEntries = PredefinedLanguages[TocConfig.DefaultLanguage];
                VerifyLangFileContent(langFile, updatedEntries, false);
            }
        }

        private void VerifyLangFileContent(FileInfo langFile, IEnumerable<TocEntryModel> updatedEntries, bool writeComments = false)
        {
            //Log.LogDebug("VerifyLangFileContent, Get Current Content");
            //foreach (var item in updatedEntries)
            //{
            //    Log.LogDebug($"VerifyLangFileContent,updatedEntries: `{item.Key}` `{item.Text}` `{item.Comment}`");
            //}

            var currentEntries = FileHandler.GetCurrentContent(langFile);
            //foreach (var item in currentEntries)
            //{
            //    Log.LogDebug($"VerifyLangFileContent,currentEntries: `{item.Key}` `{item.Text}` `{item.Comment}`");
            //}

            bool inconsistent = false;
            List<TocEntryModel> newEntries = new List<TocEntryModel>();

            foreach (var currentEntry in currentEntries)
            {
                bool currentEntryStillExists = false;
                foreach (var updatedEntry in updatedEntries)
                {
                    if (currentEntry.Key == updatedEntry.Key) currentEntryStillExists = true;
                }
                if (currentEntryStillExists) newEntries.Add(currentEntry);
            }
            if (currentEntries.Count != newEntries.Count) { inconsistent = true; }
            //foreach (var item in newEntries)
            //{
            //    Log.LogDebug($"VerifyLangFileContent,newEntries: `{item.Key}` `{item.Text}` `{item.Comment}`");
            //}

            foreach (var updatedEntry in updatedEntries)
            {
                TocEntryModel newEntry = newEntries.FirstOrDefault(x => x.Key == updatedEntry.Key);
                if (newEntry != default) continue;

                if (writeComments) newEntries.Add(new TocEntryModel(updatedEntry.Key, updatedEntry.Text, updatedEntry.Comment));
                else newEntries.Add(new TocEntryModel(updatedEntry.Key, updatedEntry.Text, string.Empty));
                inconsistent = true;
            }
            //foreach (var item in newEntries)
            //{
            //    Log.LogDebug($"VerifyLangFileContent,newEntries2: `{item.Key}` `{item.Text}` `{item.Comment}`");
            //}
            //Log.LogDebug($"VerifyLangFileContent, NeedUpdate? {inconsistent} - Verify Complete: {newEntries.Count} Lines");
            if (inconsistent) WriteUpdateToFile(langFile, newEntries);
        }

        private void WriteUpdateToFile(FileInfo langFile, IEnumerable<TocEntryModel> updatedEntries)
        {
            var sortedNewEntries = updatedEntries.OrderBy(x => x.Key).ToList();
            FileHandler.WriteUpdatedContent(langFile, sortedNewEntries);
        }
    }
}
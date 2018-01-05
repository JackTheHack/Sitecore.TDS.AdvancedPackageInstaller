using System;
using log4net;
using Sitecore.Analytics;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Engines;
using Sitecore.Data.Proxies;
using Sitecore.Events;
using Sitecore.Globalization;
using Sitecore.Install.Events;
using Sitecore.Install.Framework;
using Sitecore.SecurityModel;
using Sitecore.Update.Installer;
using Sitecore.Update.Installer.Utils;
using Sitecore.Update.Update.Utils;
using Sitecore.Update.Utils;
using System.Collections.Generic;
using System.IO;

namespace HedgehogDevelopment.TDS.PackageInstallerService
{
    public class CustomInstaller : DiffInstaller
    {
        private readonly UpgradeAction _action;

        public CustomInstaller(UpgradeAction action)
            : base(action)
		{
            _action = action;

        }

        public List<ContingencyEntry> DoInstallPackage(string path, InstallMode mode, ILog installationProcessLogger, out bool hasPostAction, out string historyPath)
        {
            return this.DoInstallPackage(path, mode, installationProcessLogger, new List<ContingencyEntry>(), out hasPostAction, out historyPath);
        }
        public List<ContingencyEntry> DoInstallPackage(string path, InstallMode mode, ILog installationProcessLogger, IList<ContingencyEntry> entries, out bool hasPostAction, out string historyPath)
        {
            historyPath = null;
            return this.DoInstallPackage(path, mode, installationProcessLogger, entries, null, out hasPostAction, ref historyPath);
        }

        public List<ContingencyEntry> DoInstallPackage(string path, InstallMode mode, ILog installationProcessLogger, IList<ContingencyEntry> entries, string rollbackPackagePath, out bool hasPostAction, ref string historyPath)
        {
            List<ContingencyEntry> contingencyEntries;

            using (new SecurityDisabler())
            using (new BulkUpdateContext())
            //using (new ProxyDisabler())
            using (new DictionaryBatchOperationContext(false))
            using (new SyncOperationContext())
            {
                bool enabled = Settings.Indexing.Enabled;
                bool trackerEnabled = Tracker.Enabled;

                if (enabled)
                {
                    Settings.Indexing.Enabled = false;
                }

                if (trackerEnabled)
                {
                    Tracker.Enabled = false;
                }

                Event.RaiseEvent("packageinstall:starting", new object[]
                {
                       new InstallationEventArgs(new List<ItemUri>(), new List<Sitecore.Install.Files.FileCopyInfo>(), "packageinstall:starting")
                });
                try
                {
                    IProcessingContext processingContext = this.CreateInstallationContext(Path.GetFileNameWithoutExtension(path), rollbackPackagePath, historyPath, mode, installationProcessLogger, entries);
                    CommandInstallerContext context = CommandInstallerContext.GetContext(processingContext);
                    using (context)
                    {
                        ISource<PackageEntry> baseSource = new CustomPackageReader(path, context.InstallationInfoLogger);


                        if (_action == UpgradeAction.Upgrade)
                        {
                            context.InstallationInfoLogger.Info("Installing package: " + path);
                            context.InstallationInfoLogger.Info("Installation Mode: " + mode);
                        }
                        ISink<PackageEntry> sink = this.DoCreateInstallerSink(processingContext);
                        ItemEntrySorter itemEntrySorter = new ItemEntrySorter(baseSource);
                        itemEntrySorter.Initialize(processingContext);
                        itemEntrySorter.Populate(sink);
                        sink.Flush();
                        sink.Finish();
                        if (_action == UpgradeAction.Upgrade)
                        {
                            try
                            {
                                this.RegisterPackage(processingContext);
                            }
                            catch
                            {
                            }
                        }
                        foreach (IProcessor<IProcessingContext> current in processingContext.PostActions)
                        {
                            current.Process(processingContext, processingContext);
                        }

                        using (var context2 = CommandInstallerContext.GetContext(processingContext))
                        {

                            hasPostAction = context2.HasPostAction;
                            historyPath = context2.HistoryRoot;
                            if (_action == UpgradeAction.Upgrade)
                            {
                                context2.InstallationInfoLogger.Info(string.Format("Installation of package '{0}' has been finished.", path));
                            }
                            contingencyEntries = context2.GetContingencyEntries();
                        }
                    }
                }
                finally
                {
                    Settings.Indexing.Enabled = enabled;
                    Tracker.Enabled = trackerEnabled;

                    Event.RaiseEvent("packageinstall:ended", new object[]
                    {
                                    new InstallationEventArgs(new List<ItemUri>(), new List<Sitecore.Install.Files.FileCopyInfo>(), "packageinstall:ended")
                    });
                }
            }
            return contingencyEntries;
        }
    }
}

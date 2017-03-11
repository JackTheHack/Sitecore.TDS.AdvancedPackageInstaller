using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Proxies;
using Sitecore.Data.Serialization;
using Sitecore.Globalization;
using Sitecore.Install.Framework;
using Sitecore.IO;
using Sitecore.Security.Serialization;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Xml.Linq;
namespace HedgehogDevelopment.SitecoreProject.PackageInstallPostProcessor
{
    public class DoPostDeployActions : IPostStep
    {
        private class DeployedItemInfo
        {
            public string Name
            {
                get;
                set;
            }
            public Guid Id
            {
                get;
                set;
            }
            public Dictionary<Guid, DeployedItemInfo> Children
            {
                get; private set;
            }
            public bool KeepChildrenInSync
            {
                get;
                set;
            }
            public Guid ParentId
            {
                get;
                set;
            }
            public Item SitecoreItem
            {
                get;
                set;
            }
            public DeployedItemInfo()
            {
                this.Children = new Dictionary<Guid, DoPostDeployActions.DeployedItemInfo>();
            }
        }
        private ITaskOutput _output;
        private string _logName;
        public void Run(ITaskOutput output, NameValueCollection metaData)
        {
            this._output = output;
            this.LogMessage("Starting PostDeployActions", new string[0]);
            this.LogMessage("Ignoring PostDeployActions", new string[0]);
            try
            {
                string text = FileUtil.MapPath("/_DEV/DeployedItems.xml");
                XElement deployedItemsXml = XElement.Load(text);
                this.DeployTemplateValues(deployedItemsXml);
                this.DeployFieldValues(deployedItemsXml);
                
                this.DeployRoles(deployedItemsXml);

                this.CleanupDeploymentFolders(deployedItemsXml);
                File.Delete(text);
                this.LogMessage("TDS PostDeployActions complete.", new string[0]);
            }
            catch (Exception ex)
            {
                this.LogMessage("Exception {0}({1}):\n{2}", new string[]
				{
					ex.Message,
					ex.GetType().Name,
					ex.StackTrace
				});
            }
        }
        private void LogMessage(string formatString, params string[] args)
        {
            try
            {
                string text = string.Format("{0}-{1}: {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), string.Format(formatString, args));
                if (this._logName == null)
                {
                    this._logName = FileUtil.MapPath(string.Format("/_DEV/Temp/PostDeployActions_{0:yyyyMMdd-hhmmss}.log", DateTime.Now));
                }

                StreamWriter file = null;

                if (File.Exists(_logName))
                {
                    file = File.CreateText(_logName);
                }
                else
                {
                    file = File.AppendText(this._logName);
                }

                using (StreamWriter streamWriter = file)
                {
                    streamWriter.WriteLine(text);
                }
                if (this._output != null)
                {
                    this._output.Alert(text);
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }
        private void CleanupDeploymentFolders(XElement deployedItemsXml)
        {
            XAttribute xAttribute = deployedItemsXml.Attribute("RecursiveDeployAction");
            if (xAttribute == null)
            {
                this.LogMessage("No recursive deploy action specified.", new string[0]);
                return;
            }
            string value = xAttribute.Value;
            if (value == "Ignore")
            {
                this.LogMessage("Recursive deploy action is set to ignore", new string[0]);
                return;
            }
            Dictionary<Guid, DoPostDeployActions.DeployedItemInfo> dictionary = this.BuildDeploymentItems(deployedItemsXml);
            using (new ProxyDisabler())
            {
                foreach (DoPostDeployActions.DeployedItemInfo current in dictionary.Values)
                {
                    try
                    {
                        if (current.KeepChildrenInSync)
                        {
                            List<Item> list = new List<Item>();
                            if (current.SitecoreItem == null)
                            {
                                throw new Exception(string.Format("Could not retrieve item {0} from the database", current.Name));
                            }
                            foreach (Item item in current.SitecoreItem.Children)
                            {
                                if (!current.Children.ContainsKey(item.ID.Guid))
                                {
                                    list.Add(item);
                                }
                            }
                            foreach (Item current2 in list)
                            {
                                string text;
                                if (current2.Paths == null)
                                {
                                    text = string.Format("No path for item {0}({1})", current2.Name, current2.ID);
                                }
                                else
                                {
                                    text = current2.Paths.FullPath;
                                }
                                if (value == "SitecoreRecycle")
                                {
                                    this.LogMessage("Recycled {0}", new string[]
									{
										text
									});
                                    current2.Recycle();
                                }
                                else
                                {
                                    this.LogMessage("Deleted {0}", new string[]
									{
										text
									});
                                    current2.Delete();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogMessage("Exception {0}({1}) while checking node {3}({4}):{2}", new string[]
						{
							ex.Message,
							ex.GetType().Name,
							ex.StackTrace,
							current.Name,
							current.Id.ToString()
						});
                    }
                }
            }
        }
        private Dictionary<Guid, DoPostDeployActions.DeployedItemInfo> BuildDeploymentItems(XElement deployedItemsXml)
        {
            Dictionary<Guid, DeployedItemInfo> dictionary = new Dictionary<Guid, DeployedItemInfo>();
            var enumerable =
                from di in deployedItemsXml.Elements("DeployedItem")
                select new
                {
                    Id = new Guid(this.GetAttribute(di.Attribute("Id"))),
                    Name = this.GetAttribute(di.Attribute("Name")),
                    Parent = new Guid(this.GetAttribute(di.Attribute("Parent"))),
                    Database = this.GetAttribute(di.Attribute("Database")),
                    KeepChildrenInSync = this.GetAttribute(di.Attribute("KeepChildrenInSync")) == "true"
                };
            foreach (var current in enumerable)
            {
                Database database = Database.GetDatabase(current.Database);
                Item item = database.GetItem(new ID(current.Id));
                DoPostDeployActions.DeployedItemInfo itemInfo = this.GetItemInfo(dictionary, current.Id);
                itemInfo.Name = current.Name;
                itemInfo.ParentId = current.Parent;
                itemInfo.SitecoreItem = item;
                itemInfo.KeepChildrenInSync = current.KeepChildrenInSync;
                DoPostDeployActions.DeployedItemInfo itemInfo2 = this.GetItemInfo(dictionary, current.Parent);
                if (itemInfo2.Children.ContainsKey(itemInfo.Id))
                {
                    this.LogMessage(string.Format("Could not insert {0}({1}) into parent {2}({3}). Id already exists.", new object[]
					{
						itemInfo.Name,
						itemInfo.Id,
						itemInfo2.Name,
						itemInfo2.Id
					}), new string[0]);
                }
                else
                {
                    itemInfo2.Children.Add(itemInfo.Id, itemInfo);
                }
            }
            return dictionary;
        }
        private DoPostDeployActions.DeployedItemInfo GetItemInfo(Dictionary<Guid, DoPostDeployActions.DeployedItemInfo> deploymentItemInfos, Guid id)
        {
            if (deploymentItemInfos.ContainsKey(id))
            {
                return deploymentItemInfos[id];
            }
            DoPostDeployActions.DeployedItemInfo deployedItemInfo = new DoPostDeployActions.DeployedItemInfo();
            deploymentItemInfos.Add(id, deployedItemInfo);
            deployedItemInfo.Id = id;
            return deployedItemInfo;
        }
        private string GetAttribute(XAttribute attribute)
        {
            if (attribute == null)
            {
                return null;
            }
            return attribute.Value;
        }
        private void DeployTemplateValues(XElement deployedItemsXml)
        {
            var enumerable =
                from f in deployedItemsXml.Elements("DeployTemplate")
                select new
                {
                    ItemId = new Guid(this.GetAttribute(f.Attribute("ItemId"))),
                    TemplateId = this.GetAttribute(f.Attribute("TemplateId")),
                    Database = this.GetAttribute(f.Attribute("Database"))
                };
            foreach (var current in enumerable)
            {
                try
                {
                    Database database = Database.GetDatabase(current.Database);
                    if (database == null)
                    {
                        this.LogMessage("Can't open database '{0}'", new string[]
						{
							current.Database
						});
                        break;
                    }
                    using (new ProxyDisabler())
                    {
                        using (new SecurityDisabler())
                        {
                            Item item = database.GetItem(new ID(current.ItemId));
                            if (item == null)
                            {
                                this.LogMessage("Can't load item {0}", new string[]
								{
									current.ItemId.ToString()
								});
                            }
                            else
                            {
                                TemplateItem templateItem = database.GetItem(current.TemplateId);
                                if (templateItem == null)
                                {
                                    this.LogMessage("Can't load item {0}", new string[]
									{
										current.ItemId.ToString()
									});
                                }
                                else
                                {
                                    this.LogMessage("setting template for item '{0}' to '{1}'", new string[]
									{
										item.Paths.FullPath,
										templateItem.Name
									});
                                    bool flag = false;
                                    using (new EditContext(item, false, flag))
                                    {
                                        item.TemplateID = templateItem.ID;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.LogMessage("Exception {0}({1}):\n{2}", new string[]
					{
						ex.Message,
						ex.GetType().Name,
						ex.StackTrace
					});
                }
            }
        }
        private void DeployFieldValues(XElement deployedItemsXml)
        {
            var enumerable =
                from f in deployedItemsXml.Elements("DeployField")
                select new
                {
                    ItemId = new Guid(this.GetAttribute(f.Attribute("ItemId"))),
                    FieldName = this.GetAttribute(f.Attribute("FieldName")),
                    FieldLanguage = this.GetAttribute(f.Attribute("FieldLanguage")),
                    Database = this.GetAttribute(f.Attribute("Database")),
                    Value = f.Value
                };
            foreach (var current in enumerable)
            {
                try
                {
                    Database database = Database.GetDatabase(current.Database);
                    if (database == null)
                    {
                        this.LogMessage("Can't open database '{0}'", new string[]
						{
							current.Database
						});
                        break;
                    }
                    using (new ProxyDisabler())
                    {
                        using (new SecurityDisabler())
                        {
                            Item item;
                            if (string.IsNullOrEmpty(current.FieldLanguage))
                            {
                                item = database.GetItem(new ID(current.ItemId));
                            }
                            else
                            {
                                Language language = LanguageManager.GetLanguage(current.FieldLanguage);
                                item = database.GetItem(new ID(current.ItemId), language);
                            }
                            if (item == null)
                            {
                                this.LogMessage("Can't load item {0} for language '{1}'", new string[]
								{
									current.ItemId.ToString(),
									current.FieldLanguage
								});
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(current.FieldLanguage))
                                {
                                    this.LogMessage("Updating field '{0}' for item '{1}'", new string[]
									{
										current.FieldName,
										item.Paths.FullPath
									});
                                }
                                else
                                {
                                    this.LogMessage("Updating field '{0}.{1}' for item '{2}'", new string[]
									{
										current.FieldName,
										current.FieldLanguage,
										item.Paths.FullPath
									});
                                }
                                if (item.Fields[current.FieldName] == null)
                                {
                                    this.LogMessage("ERROR: Can't find field on item", new string[0]);
                                }
                                else
                                {
                                    if (item.Fields[current.FieldName].IsBlobField)
                                    {
                                        byte[] buffer = Convert.FromBase64String(current.Value);
                                        using (MemoryStream memoryStream = new MemoryStream(buffer))
                                        {
                                            using (new SecurityDisabler())
                                            {
                                                item.Editing.BeginEdit();
                                                item.Fields[current.FieldName].SetBlobStream(memoryStream);
                                                item.Editing.EndEdit(false, false);
                                            }
                                            continue;
                                        }
                                    }
                                    bool flag = false;
                                    using (new EditContext(item, false, flag))
                                    {
                                        item.Fields[current.FieldName].SetValue(current.Value, true);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.LogMessage("Exception {0}({1}):\n{2}", new string[]
					{
						ex.Message,
						ex.GetType().Name,
						ex.StackTrace
					});
                }
            }
        }
        private void DeployRoles(XElement deployedItemsXml)
        {
            var roleDeployService = new XmlRoleSyncService(deployedItemsXml) { LogMethod = LogMessage};
            roleDeployService.Deploy();
        }
    }
}

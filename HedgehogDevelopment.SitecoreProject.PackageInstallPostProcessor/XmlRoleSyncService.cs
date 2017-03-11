using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Sitecore.Data.Serialization;
using Sitecore.Security.Serialization;

namespace HedgehogDevelopment.SitecoreProject.PackageInstallPostProcessor
{
    public class XmlRoleSyncService
    {
        private readonly XElement _itemsXml;

        public Action<string, string[]> LogMethod { get; set; } 

        public XmlRoleSyncService(XElement deployedItemsXml)
        {
            _itemsXml = deployedItemsXml;
        }

        public void Deploy()
        {
            var enumerable =
                from r in _itemsXml.Elements("Role")
                select new
                {
                    RoleName = r.Attribute("Name").Value,
                    Role = Convert.FromBase64String(r.Value)
                };
            foreach (var current in enumerable)
            {
                RoleReference roleReference = new RoleReference(current.RoleName);
                string filePath = PathUtils.GetFilePath(roleReference);
                string directoryName = Path.GetDirectoryName(filePath);

                LogMethod($"Installing Role {0}", new string[] {current.RoleName});

                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                File.WriteAllBytes(filePath, current.Role);
                Manager.LoadRole(filePath);
            }
        }
    }
}

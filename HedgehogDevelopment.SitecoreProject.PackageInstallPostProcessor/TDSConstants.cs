using System;
namespace HedgehogDevelopment.SitecoreProject.VSIP.Utils
{
    internal static class TDSConstants
    {
        internal const string PROJECT_SITECORE_NAME = "SitecoreName";
        internal const string PROJECT_EXCLUDE_ITEM_FROM = "ExcludeItemFrom";
        internal const string PROJECT_ITEM_DEPLOYMENT = "ItemDeployment";
        internal const string PROJECT_ITEM_BASE_CODE_REGENERATION = "RegenerateBaseTemplates";
        internal const string PROJECT_ITEM_DERIVED_CODE_REGENERATION = "RegenerateDerivedTemplates";
        internal const string PROJECT_CHILD_ITEM_SYNC = "ChildItemSynchronization";
        internal const string PROJECT_DEPLOY_ALWAYS_FIELDS = "DeployAlwaysFields";
        internal const string REPLACEMENT_ASTERISK = "#Asterisk";
        internal const string REPLACEMENT_DOLLAR = "%24";
        internal const string ITEM_EXTENSION = ".item";
        internal const string CODE_GEN_TEMPLATE_FOLDER = "Code Generation Templates";
        internal const string ADDITIONAL_FILES_FOLDER = "Additional Files";
        internal const string SITECORE_ROLES_FOLDER = "Sitecore Roles";
        internal const string SITECORE_ITEM_BUILD_TYPE = "SitecoreItem";
        internal const string SITECORE_ROLE_BUILD_TYPE = "SitecoreRole";
        internal const string CODE_GEN_TEMPLATE_BUILD_TYPE = "CodeGenTemplate";
        internal const string TEMPLATE_CACHE_FOLDER = "obj\\T4RenderCache";
        internal const string NO_TEMPLATE_GENERATION = "<None>";
        internal const string DEPLOY_TEMPLATE = "!ITEM_TEMPLATE!";
        internal const string IDP_FILE_NAME = "ItemDeployProp.xml";
        internal const string MSBUILD_EXTENSION_PATH_PROPERTY_NAME = "MSBuildExtensionsPath";
        internal const string TDS_GLOBAL_FILE_NAME = "TdsGlobal.config";
        internal const string TDS_GLOBAL_USER_FILE_NAME = "TdsGlobal.config.user";
        internal const string TDS_WEBSITE_DOMAIN = "http://www.hhogdev.com";
        internal static readonly char[] ILLEGAL_CHARACTERS = new char[]
		{
			'"',
			'*',
			'/',
			':',
			';',
			'?',
			'\\',
			'|',
			'+',
			'<',
			'>',
			'\'',
			'@'
		};
        internal static string DEPLOYFOLDER_SITECORE_KERNAL_PATH = "bin\\sitecore.kernel.dll";
        internal static string DEPLOYFOLDER_WEB_CONFIG_PATH = "web.config";
        internal static string DEPLOYFOLDER_TDS_WEB_CONFIG_PATH = "_DEV\\Web.config";
        internal static string DEPLOYFOLDER_TDS_SERVICE_PATH = "_DEV\\TdsService.asmx";
        internal static string DEPLOYFOLDER_TDS_ASSEMBLYPATH = "bin\\HedgehogDevelopment.SitecoreProject.Service.dll";
        internal static string MSBUILD_SERVICE_PATH = "\\HedgehogDevelopment\\SitecoreProject\\v9.0\\bin\\HedgehogDevelopment.SitecoreProject.Service.dll";
        internal static string LOGGER_OUTPUT_WINDOW = "Team Development for Sitecore";
        internal static string URL_WHATS_NEW
        {
            get
            {
                return "http://www.hhogdev.com/help/tds/whatsnew";
            }
        }
        internal static string URL_NEWER_VERSION
        {
            get
            {
                return "http://www.hhogdev.com/Downloads/Team-Development-for-Sitecore.aspx";
            }
        }
        internal static string URL_HELP
        {
            get
            {
                return "http://www.hhogdev.com/help?key={0}-{1}";
            }
        }
        internal static string URL_HELP_ERROR
        {
            get
            {
                return "http://www.hhogdev.com/help/tds/errors#{0}";
            }
        }
        internal static string UPDATE_CHECK_URL
        {
            get
            {
                return "http://www.hhogdev.com/Services/ProductInfo.asmx";
            }
        }
    }
}

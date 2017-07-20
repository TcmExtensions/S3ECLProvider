using System;
using System.AddIn;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using S3ECLProvider.api;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider
{
    [AddIn("S3ECLProvider", Version = "1.2.0.4")]   //With Search Implemented
    public class S3Provider : IContentLibrary
    {
        private static readonly XNamespace S3Ns = "http://www.sdltridion.com/S3EclProvider/Configuration"; //"http://s3.com/services/api";
        private static readonly string IconBasePath = Path.Combine(AddInFolder, "Themes");
        internal static string MountPointId { get; private set; }
        internal static S3 S3 { get; private set; }
        internal static IHostServices HostServices { get; private set; }

        // This should probably be more generally available - maybe as an extension to IContentLibraryContext in addinbase?
        internal static string AddInFolder
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        internal static byte[] GetIconImage(string iconIdentifier, int iconSize)
        {
            int actualSize;
            // get icon directly from default theme folder
            return HostServices.GetIcon(IconBasePath, "_Default", iconIdentifier, iconSize, out actualSize);
        }

        public void Initialize(string mountPointId, string configurationXmlElement, IHostServices hostServices)
        {
            MountPointId = mountPointId;
            HostServices = hostServices;
            XElement config = XElement.Parse(configurationXmlElement);
            S3 = new S3(
                config.Element(S3Ns + "S3BucketName").Value,
                config.Element(S3Ns + "S3SecretKey").Value,
                config.Element(S3Ns + "S3AccessId").Value,
                config.Element(S3Ns + "FullBucketUrl").Value
                );
        }

        public IContentLibraryContext CreateContext(IEclSession eclSession)
        {
            return new S3MountPoint(eclSession);
        }

        public IList<IDisplayType> DisplayTypes
        {
            get
            {
                // we currently support S3 (Photo) Sets as folders and S3 Photos in them
                return new List<IDisplayType>
                           {
                               HostServices.CreateDisplayType("fld", "Folder", EclItemTypes.Folder),
                               HostServices.CreateDisplayType("fls", "File", EclItemTypes.File)                              
                            };
            }
        }

        public byte[] GetIconImage(string theme, string iconIdentifier, int iconSize)
        {
            // use static implementation
            return GetIconImage(iconIdentifier, iconSize);
        }

        public void Dispose()
        {
        }
    }
}

using System;
using System.AddIn;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Web;
using System.Xml.Linq;
using S3ECLProvider.API;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider
{
    /// <summary>
    /// Represents an external system providing data to SDL Tridion.
    /// </summary>
    /// <remarks>
    /// The <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary"/> is the starting point for
    /// implementing a Provider for the External Content Library.
    /// The External Content Library creates a single instance of the Provider's <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary"/>
    /// in its own System.AppDomain for each configured Mount Point to ensure static
    /// variables from one Mount Point are not accessible from other Mount Points, even
    /// if they use the same Provider.
    /// When the External Content Library has created the instance of the Provider's
    /// <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary"/>, it calls <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary.Initialize(System.String,System.String,Tridion.ExternalContentLibrary.V2.IHostServices)"/>
    /// before any other method is called.
    /// With the exception of the <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary.Initialize(System.String,System.String,Tridion.ExternalContentLibrary.V2.IHostServices)"/>
    /// method the implementation of <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary"/> must be tread safe.
    /// </remarks>
    [AddIn("S3ECLProvider", Version = "1.2.0.4")]
    public class S3Provider : IContentLibrary
    {
        private static readonly XNamespace S3Ns = "http://www.sdltridion.com/S3EclProvider/Configuration";
        private int _cacheTime;
        private IHostServices _hostServices;
        private ObjectCache _cache;

        /// <summary>
        /// <see cref="S3" /> exposes the S3 API functions
        /// </summary>
        /// <value>
        /// <see cref="S3"/> API functions
        /// </value>
        internal S3 S3 { get; private set; }

        /// <summary>
        /// Provides initialization information from the External Content Library to the Provider.<see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibrary" />
        /// </summary>
        /// <param name="mountPointId">The string ID of the IContentLibraryContract instance. Note that this is the <see cref="P:Tridion.ExternalContentLibrary.V2.IEclUri.MountPointId" /> part of the URI, not the full <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        /// <param name="configurationXmlElement">The XML element containing the configuration for the External Content Library.
        /// Providers can read custom elements and attributes to access configuration data specific to the Provider.</param>
        /// <param name="hostServices">Provides access to a number of services available from the External Content Library host.</param>
        /// <remarks>
        /// Initialize is called only once immediately after the <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibrary" /> is instantiated and before any other method is called. The instance is
        /// used until the Tridion process hosting the Provider is terminated, or its <see cref="T:System.AppDomain" /> is recycled.
        /// </remarks>
        public void Initialize(string mountPointId, string configurationXmlElement, IHostServices hostServices)
        {
            _hostServices = hostServices;
            _cache = new MemoryCache(mountPointId);

            XElement xmlConfig = XElement.Parse(configurationXmlElement);
            XElement s3Config = xmlConfig.Element(S3Ns + "S3ECLProvider");

            if (s3Config == null)
                throw new ConfigurationErrorsException("No S3 configuration node with namespace http://www.sdltridion.com/S3EclProvider/Configuration found.");

            Dictionary<String, String> config = 
                s3Config
                    .Elements()
                    .ToDictionary(e => e.Name.LocalName, e => e.Value);

            S3 = new S3(
                config["Region"],
                VirtualPathUtility.RemoveTrailingSlash(config["BucketName"]),
                config["AccessKeyId"],
                config["SecretAccessKey"],
                VirtualPathUtility.AppendTrailingSlash(config["BucketUrl"]),
                VirtualPathUtility.AppendTrailingSlash(config["Prefix"]));

            if (!int.TryParse(config["CacheTime"], out _cacheTime))
                _cacheTime = 120;
        }

        /// <summary>
        /// Returns requested item from provider cache
        /// </summary>
        /// <typeparam name="T"><see cref="T:S3ECLProvider.S3ListItem"/></typeparam>
        /// <param name="key">AWS S3 Key</param>
        /// <returns>Requested item or null</returns>
        public S3ListItem Cached(String key)
        {
            return _cache.Get(key) as S3ListItem;
        }

        /// <summary>
        /// Cache the <paramref name="item"/> in the provider cache
        /// </summary>
        /// <param name="item"><see cref="T:S3ECLProvider.S3ListItem"/></param>
        public void Cache(S3ListItem item)
        {
            _cache.Set(item.Id.ItemId, item, DateTime.Now.AddSeconds(_cacheTime));
        }

        /// <summary>
        /// Purges the item specified by <paramref name="key"/> from the cache
        /// </summary>
        /// <param name="item"><see cref="T:S3ECLProvider.S3ListItem"/></param>
        public void Purge(S3ListItem item)
        {
            _cache.Remove(item.Id.ItemId);
        }

        /// <summary>
        /// Gets the <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /> for an <paramref name="item"/>
        /// </summary>
        /// <param name="item"><see cref="T:S3ECLProvider.API.S3ItemData"/>.</param>
        /// <param name="contextUri"><see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        /// <returns><see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" />s</returns>
        public IEclUri GetUri(S3ItemData item, IEclUri contextUri)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            return _hostServices.CreateEclUri(
                contextUri.PublicationId,
                contextUri.MountPointId,
                item.Key,
                item.ItemType == S3ItemType.Folder ? Constants.S3_FOLDER_ID : Constants.S3_FILE_ID,
                item.ItemType == S3ItemType.Folder ? EclItemTypes.Folder : EclItemTypes.File);
        }

        /// <summary>
        /// Determine the parent uri for a given <paramref name="uri" />
        /// </summary>        
        /// <param name="uri"><see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        /// <returns>Parent <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /></returns>
        public IEclUri GetParentUri(IEclUri uri)
        {
            if (_hostServices.IsNullOrNullEclUri(uri))
                throw new ArgumentNullException("uri");

            String[] parts = uri.ItemId.Split('/');

            if (parts.Length == 1) {
                return _hostServices.CreateEclUri(
                    uri.PublicationId,
                    uri.MountPointId,
                    "root",
                    "mp",
                    EclItemTypes.MountPoint);
            }

            return _hostServices.CreateEclUri(
                uri.PublicationId,
                uri.MountPointId,
                uri.ItemId.Substring(0, uri.ItemId.Length - (parts.Last().Length + 1)),
                Constants.S3_FOLDER_ID,
                EclItemTypes.Folder);
        }

        /// <summary>
        /// Determine if a given <paramref name="uri"/> is an image
        /// </summary>        
        /// <param name="uri">><see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        /// <param name="session">Current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession" /></param>
        /// <returns><c>true</c> if the content represents an image, otherwise <c>false</c></returns>
        public bool IsImageType(IEclUri uri)
        {
            if (_hostServices.IsNullOrNullEclUri(uri))
                throw new ArgumentNullException("uri");

            if (uri.ItemType == EclItemTypes.File && (uri.SubType == Constants.S3_FILE_ID)) {
                switch (VirtualPathUtility.GetExtension(uri.ItemId).ToLowerInvariant()) {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates an <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryContext" /> allowing operations to be performed on behalf of a user.
        /// </summary>
        /// <param name="session">The <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryContext" /> the context should be created for. This gives access to the <see cref="T:Tridion.ExternalContentLibrary.V2.ITridionUser" />.</param>
        /// <returns>
        /// An <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryContext" /> if the library is available to the specified user; <c>null</c> if the library is not available.
        /// </returns>
        /// <remarks>
        /// It is important the context is created as fast as possible as it is created frequently when the user is navigating the Tridion user interface. The provider can choose
        /// to cache the data it needs to create the instance, but it should not cache the actual context instance as it will be disposed at the end of the current operation.
        /// </remarks>
        public IContentLibraryContext CreateContext(IEclSession session)
        {
            return new S3MountPoint(this, session);
        }

        /// <summary>
        /// Gets a list of human-readable strings associated with the item types the Provider can insert into the Tridion user interface.
        /// </summary>
        /// <remarks>
        /// <para>Use <see cref="P:System.Threading.Thread.CurrentUICulture" /> of the <see cref="P:System.Threading.Thread.CurrentThread" /> to determine which language the
        /// <see cref="P:Tridion.ExternalContentLibrary.V2.IDisplayType.DisplayText" /> should be returned in. The Provider should expect the display types
        /// to be retrieved regularly, unless the result is time consuming to calculate in which case the Provider should cache the result and make sure
        /// to take the requested language into account when caching.</para>
        /// <para>Use <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.CreateDisplayType(System.String,System.String,Tridion.ExternalContentLibrary.V2.EclItemTypes)" /> to create an instance of <see cref="T:Tridion.ExternalContentLibrary.V2.IDisplayType" />.</para>
        /// </remarks>
        /// <seealso cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.DisplayTypeId" />
        public IList<IDisplayType> DisplayTypes
        {
            get
            {               
                return new List<IDisplayType>
                {
                    _hostServices.CreateDisplayType(Constants.S3_FOLDER_ID, "Folder", EclItemTypes.Folder),
                    _hostServices.CreateDisplayType(Constants.S3_FILE_ID, "File", EclItemTypes.File)                              
                };
            }
        }

        /// <summary>
        /// Gets the icon for an item.
        /// </summary>
        /// <param name="theme">Specifies the theme specified in Tridion. If the icons are not theme-specific, you can ignore this parameter.</param>
        /// <param name="iconIdentifier">The identifier of the icon. The value is from <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.IconIdentifier" />
        /// or <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.IconIdentifier" />.</param>
        /// <param name="iconSize">Size of the icon.</param>
        /// <returns>
        /// The binary representation of a PNG, GIF, or JPG formatted image. PNG is the recommended format.
        /// </returns>
        public byte[] GetIconImage(string theme, string iconIdentifier, int iconSize)
        {
            return _hostServices.GetIcon(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Themes"), 
                "_Default", 
                iconIdentifier, 
                iconSize);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}

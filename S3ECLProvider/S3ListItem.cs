using System;
using S3ECLProvider.API;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider
{
    /// <summary>
    /// <see cref="S3ListItem" /> represents the listing of a content item stored in Amazon S3
    /// </summary>
    /// <remarks><see cref="S3ListItem" /> implements the SDL <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem" /> interface</remarks>
    public class S3ListItem : IContentLibraryListItem
    {
        protected readonly S3ItemData _itemData;
        private String _title;

        /// <summary>
        /// Initialize a new <see cref="S3ListItem" />
        /// </summary>
        /// <param name="provider">Associated <see cref="S3Provider"/></param>
        /// <param name="session">Current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession"/></param>
        /// <param name="uri"><see cref="Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        public S3ListItem(S3Provider provider, IEclSession session, IEclUri uri)
        {
            Provider = provider;
            Session = session;
            ParentId = provider.GetParentUri(uri);

            if (uri.ItemType == EclItemTypes.Folder)
                _itemData = provider.S3.GetFolder(uri.ItemId);
            else
                _itemData = provider.S3.GetObject(uri.ItemId);

            Id = provider.GetUri(_itemData, uri);
        }

        /// <summary>
        /// Initialize a new <see cref="S3ListItem" />
        /// </summary>
        /// <param name="provider">Associated <see cref="S3Provider"/></param>
        /// <param name="session">Current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession"/></param>
        /// <param name="parentUri"><see cref="Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        /// <param name="eclObject"><see cref="Amazon.S3.Model.S3Object"/></param>
        /// <remarks>Creating a <see cref="S3ListItem" /> directly from an <see cref="T:S3ECLProvider.API.S3ItemData"/></remarks>
        public S3ListItem(S3Provider provider, IEclSession session, IEclUri parentUri, S3ItemData eclObject)
        {
            Provider = provider;
            Session = session;
            ParentId = parentUri;

            _itemData = eclObject;
            Id = provider.GetUri(_itemData, parentUri);
        }

        /// <summary>
        /// Initialize a new <see cref="S3ListItem" /> in a given context
        /// </summary>
        /// <param name="item"><see cref="S3ListItem"/> to create a shallow clone from</param>
        public S3ListItem(S3ListItem item, IEclUri contextUri)
        {
            Provider = item.Provider;
            Session = item.Session;
            ParentId = item.ParentId;

            _itemData = item._itemData;

            Id = item.Id.GetInPublication(contextUri.PublicationId);
        }
        
        /// <summary>
        /// Associated <see cref="S3Provider"/>
        /// </summary>
        /// <value>
        /// <see cref="S3Provider"/>
        /// </value>
        protected S3Provider Provider
        {
            get;
        }

        protected IEclSession Session
        {
            get;
        }

        /// <summary>
        /// Returns the new title of the <see cref="S3ListItem" /> if set through the title property
        /// </summary>
        protected String NewTitle {
            get {
                return _title;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user can upload new multimedia items to the folder.
        /// </summary>
        /// <remarks>
        /// This value is only used for Folders. The returned value must be identical to the value set in <see cref="P:Tridion.ExternalContentLibrary.V2.IFolderContent.CanGetUploadMultimediaItemsUrl" />
        /// when getting the list of child Items. If the value is set to <c>true</c>, the Provider must return the URL of a page where the user can
        /// upload the items from the method <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.GetUploadMultimediaItemsUrl(Tridion.ExternalContentLibrary.V2.IEclUri)" />.
        /// </remarks>
        public bool CanGetUploadMultimediaItemsUrl
        {
            get {
                if (_itemData.ItemType == S3ItemType.Folder)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the provider supports searching in the folder.
        /// </summary>
        /// <remarks>
        /// The returned value must be identical to the value set in <see cref="P:Tridion.ExternalContentLibrary.V2.IFolderContent.CanSearch" />
        /// when getting the list of child items of the mount point or folder.
        /// </remarks>
        public bool CanSearch
        {
            get {
                return true;
            }
        }

        /// <summary>
        /// Gets the id of the type to display in the UI.
        /// </summary>
        /// <remarks>
        /// The id must be one of the ids returned by <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibrary.DisplayTypes" />
        /// </remarks>
        public string DisplayTypeId
        {
            get {
                return Id.SubType;
            }
        }

        /// <summary>
        /// Gets the string identifying the icon to use for this item. The identifier is passed to <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibrary.GetIconImage(System.String,System.String,System.Int32)" />.
        /// If <c>null</c> is returned, the default icon is used.
        /// </summary>
        public string IconIdentifier
        {
            get {
                return null;
            }
        }

        /// <summary>
        /// Gets the unique id of the item.
        /// </summary>
        public IEclUri Id { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a thumbnail view is available for this item.
        /// </summary>
        public bool IsThumbnailAvailable
        {
            get {
                // Folders in S3 are virtual and do not have a thumbnail
                if (_itemData.ItemType == S3ItemType.Folder)
                    return false;

                return Provider.IsImageType(Id);
            }
        }

        /// <summary>
        /// Gets the last modified date of the item, or <c>null</c> if the date is not available.
        /// </summary>
        public DateTime? Modified
        {
            get {
                // Folders in S3 are virtual and do not have a modification date
                if (_itemData.ItemType == S3ItemType.Folder)
                    return null;

                return _itemData.LastModified;
            }
        }

        /// <summary>
        /// Gets a string with the ETag (Entity Tag) of the thumbnail. Returns <c>null</c> if <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.IsThumbnailAvailable" /> returns false.
        /// </summary>
        /// <remarks>
        /// <para>The ETag is used to control caching of the thumbnail. If the ETag remains unchanged, the server side cache and browser cache assume the thumbnail is up-to-date.</para>
        /// <para>Depending on the data structure of the external system, this could for example be a string representation of the <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.Modified" /> date or a
        /// version number.</para>
        /// <para>If the Provider cannot determine whether the thumbnail should be changed, it must use a time limited ETag. For example, to cache
        /// images up to a day, the following ETag can be used: DateTime.UtcNow.ToString("yyyyMMdd").</para>
        /// </remarks>
        public string ThumbnailETag
        {
            get {
                // Folder in S3 are virtual and do not have thumbnail
                if (_itemData.ItemType == S3ItemType.Folder)
                    return null;

                return _itemData.ETag.Replace("\"", "");
            }
        }

        /// <summary>
        /// Gets or sets the title of the item.
        /// </summary>
        /// <remarks>
        /// The property should only be changed if <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.CanUpdateTitle" /> returns <c>true</c>.
        /// If <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.CanUpdateTitle" /> returns <c>false</c>, the Provider must throw a
        /// <c>NotSupportedException</c> when the property is changed.
        /// </remarks>
        public virtual string Title {
            get {                
                return Id.ItemId.Substring(Id.ItemId.LastIndexOf('/') + 1);
            }
            set {
                if (String.IsNullOrEmpty(value))
                    _title = null;
                else
                    _title = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets the id of the parent item in the tree structure.
        /// </summary>
        /// <remarks>
        /// For the top-level item, this is the URI of the Mount Point the <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/>
        /// is mounted in. This can be created using <see cref="Tridion.ExternalContentLibrary.V2.IHostServices.CreateEclUri(System.Int32,System.String)"/>.
        /// </remarks>
        public IEclUri ParentId { get; }

        /// <summary>
        /// Dispatches the specified command to the object in the Provider.
        /// </summary>
        /// <param name="command">A string representing the method to be executed. It is recommended to use a short keyword.</param>
        /// <param name="payloadVersion">A string identifying the version of the Provider the payload is created for.</param>
        /// <param name="payload">The payload containing the parameters to the method. It is recommended to encode the parameters in an XML document.</param>
        /// <param name="responseVersion">A string identifying the version of the provider handling the method.</param>
        /// <returns>
        /// The response from the method call. The Provider determines the exact format; the recommended format is an XML document.
        /// </returns>
        /// <exception cref="NotSupportedException"></exception>
        /// <remarks>
        /// <para>
        /// Use the <see cref="M:Tridion.ExternalContentLibrary.V2.IDispatchHandler.Dispatch(System.String,System.String,System.String,System.String@)" /> method in SDL Tridion templates and event handlers to perform operations supported by the Provider
        /// but not supported directly by the External Content Library API. For example, operations that needs to be performed when an item is published from SDL Tridion.
        /// The External Content Library is not aware of the content being dispatched and will not perform any verification of the data being passed.
        /// </para>
        /// <para>
        /// It is recommended to avoid communicating with the Content Manager of SDL Tridion directly from a Provider, but if itis necessary use the
        /// SDL Tridion Core Service instead of the <see cref="T:Tridion.ExternalContentLibrary.V2.IDispatchHandler" />.
        /// </para>
        /// <para>
        /// To ensure a new Provider does not break implementations, take into account the following guidelines:
        /// </para>
        /// <list type="bullet">
        ///   <item>The caller must specify the version of the Provider it was programmed against in the <paramref name="payloadVersion" /> parameter.</item>
        ///   <item>The caller must throw a NotSupportedException if the version specified in <paramref name="responseVersion" /> is not known.</item>
        ///   <item>The provider must throw a NotSupportedException if the command specified in <paramref name="command" /> is not known.</item>
        ///   <item>The provider must throw a NotSupportedException if it cannot handle the specified command according to the expected behavior of the version
        /// specified in <paramref name="payloadVersion" />. The exception message must clearly indicate which versions are supported.</item>
        ///   <item>
        /// The Provider should use the same version to identify the <paramref name="responseVersion" /> and supported <paramref name="payloadVersion" /> as
        /// it use to identify in the version in the AddIn attribute on the implementation of <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibrary" />.
        /// </item>
        ///   <item>
        /// The Provider should support the <paramref name="payload" /> of older versions.
        /// </item>
        ///   <item>
        /// The Provider documentation must include details on which <paramref name="command">commands</paramref> it supports and the exact
        /// format of the <paramref name="payload" /> and returned value.</item>
        ///   <item>
        /// It is recommended the Provider attempt to encode the response with the same version as the <paramref name="payload" />.
        /// </item>
        ///   <item>
        /// It is recommended the Provider use an XML string to represent complex types.
        /// </item>
        /// </list>
        /// </remarks>
        public string Dispatch(string command, string payloadVersion, string payload, out string responseVersion)
        {
            throw new NotSupportedException();
        }
    }
}

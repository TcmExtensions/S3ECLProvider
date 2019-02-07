using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using S3ECLProvider.API;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider
{
    /// <summary>
    /// The <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/> manages a set of
    /// operations performed on the external system for a user.
    /// </summary>
    /// <remarks>
    /// When writing SDL Tridion templates or event handlers the <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/>
    /// is available from the <see cref="Tridion.ExternalContentLibrary.V2.IEclSession.GetContentLibrary(Tridion.ExternalContentLibrary.V2.IEclUri)"/>
    /// method.
    /// When implementing a provider, the <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/>
    /// is always instantiated by the provider when the method <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary.CreateContext(Tridion.ExternalContentLibrary.V2.IEclSession)"/>
    /// is called by the External Content Library. The provider can use the <see cref="Tridion.ExternalContentLibrary.V2.IEclSession.TridionUser"/>
    /// property on the <see cref="Tridion.ExternalContentLibrary.V2.IEclSession"/> passed to the <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary.CreateContext(Tridion.ExternalContentLibrary.V2.IEclSession)"/>
    /// method to identify the Tridion User initiating the operations.
    /// The External Content Library will then use the <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/>
    /// returned from <see cref="Tridion.ExternalContentLibrary.V2.IContentLibrary.CreateContext(Tridion.ExternalContentLibrary.V2.IEclSession)"/>
    /// to perform one or more actions on the external system before it finally disposes
    /// the <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/>.
    /// The lifetime of a <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/> is
    /// short. It can for example be a single read operation initiated by the user opening
    /// an external item in SDL Tridion, or a number of requests made on an SDL Tridion
    /// template rendering a Page containing multiple external items. The objects returned
    /// from the methods and properties on <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/>
    /// will not be used in connection with any other context, and with the exception
    /// of the <see cref="Tridion.ExternalContentLibrary.V2.IContentResult"/> returned from <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryMultimediaItem.GetContent(System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.ITemplateAttribute})"/>
    /// the External Content Library will not call any methods or properties on any objects
    /// returned from the <see cref="Tridion.ExternalContentLibrary.V2.IContentLibraryContext"/> after it has been disposed.
    /// </remarks>
    public class S3MountPoint : IContentLibraryContext
    {
        private S3Provider _provider;
        private IEclSession _session;

        /// <summary>
        /// Gets the a string identifying the icon to use for this External Content Library.
        /// This identifier will be passed to <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibrary.GetIconImage(System.String,System.String,System.Int32)" />.
        /// If <c>null</c> is returned, the default icon will be used.
        /// </summary>
        public string IconIdentifier
        {
            get { return "S3"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="S3MountPoint"/> class.
        /// </summary>
        /// <param name="provider">Associated <see cref="S3Provider"/></param>
        /// <param name="session">The session.</param>
        public S3MountPoint(S3Provider provider, IEclSession session)
        {
            _provider = provider;
            _session = session;
        }

        /// <summary>
        /// Gets a value indicating whether the user can upload new multimedia items as direct child items of the mount point.
        /// </summary>
        /// <param name="publicationId">The id of the Tridion Publication items will be uploaded from.</param>
        /// <returns></returns>
        /// <remarks>
        /// The returned value must be identical to <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.CanGetUploadMultimediaItemsUrl" />
        /// If the value is set to <c>true</c>, the Provider must return the URL of a page where the user can
        /// upload the items from the method <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.GetUploadMultimediaItemsUrl(Tridion.ExternalContentLibrary.V2.IEclUri)" />.
        /// </remarks>
        public bool CanGetUploadMultimediaItemsUrl(int publicationId)
        {
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether the provider supports searching in the mount point.
        /// </summary>
        /// <param name="publicationId">The id of the Tridion Publication search will be performed for.</param>
        /// <returns>
        /// <c>true</c> if search should be enabled for the mount point in the specified Publication; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The returned value must be identical to the value set in <see cref="P:Tridion.ExternalContentLibrary.V2.IFolderContent.CanGetUploadMultimediaItemsUrl" />
        /// when getting the list of child items of the mount point.
        /// </remarks>
        public bool CanSearch(int publicationId)
        {
            return true;
        }

        /// <summary>
        /// Finds the path to an item though the folder structure.
        /// </summary>
        /// <param name="eclUri">The URI of the <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryItem" /> to get the path for.</param>
        /// <returns>
        /// <c>null</c> if the path should be resolved by calling <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.GetItem(Tridion.ExternalContentLibrary.V2.IEclUri)" /> and reading the <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.ParentId" /> property recursively,
        /// or all ascendants of the item starting with the first child Item of the <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryContext" /> and ending with the requested Item itself.
        /// </returns>
        /// <remarks>
        /// This method should only be implemented in a provider if the provider facilitates a faster way to determine the full path than getting each
        /// item one by one and reading the property <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.ParentId" /> recursively.
        /// </remarks>
        public IList<IContentLibraryListItem> FindItem(IEclUri eclUri)
        {
            // return null so we force it to call GetItem(IEclUri)
            return null;
        }

        /// <summary>
        /// Gets an <see cref="T:Tridion.ExternalContentLibrary.V2.IFolderContent" /> representing the content of an external content folder or mount point. This list is used to build a tree structure when the user browse the content of the external library.
        /// </summary>
        /// <param name="parentFolderUri">The <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /> of the parent item. If <see cref="P:Tridion.ExternalContentLibrary.V2.IEclUri.ItemType" /> is <see cref="F:Tridion.ExternalContentLibrary.V2.EclItemTypes.MountPoint" /> the top level items are requested.</param>
        /// <param name="pageIndex">The 0 based index of the page to retrieve if the folder supports pagination.</param>
        /// <param name="itemTypes">Filters the item types to return. This can be used to for example only retrieve Folders for building up the tree structure.</param>
        /// <returns>
        /// A list of child items that should be displayed under the specified Folder.
        /// </returns>
        /// <remarks>
        /// A provider can use <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.CreateFolderContent(Tridion.ExternalContentLibrary.V2.IEclUri,System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.IContentLibraryListItem},System.Boolean,System.Boolean)" />
        /// or one of the overloaded methods to initialize an instance of <see cref="T:Tridion.ExternalContentLibrary.V2.IFolderContent" />.
        /// </remarks>
        public IFolderContent GetFolderContent(IEclUri parentFolderUri, int pageIndex, EclItemTypes itemTypes)
        {
            String prefix = parentFolderUri.ItemId == "root" ? String.Empty : parentFolderUri.ItemId;

            IList<IContentLibraryListItem> items = new List<IContentLibraryListItem>();

            foreach (S3ItemData itemData in _provider.S3.GetListing(prefix)) {
                S3ListItem item = null;

                if (itemData.ItemType == S3ItemType.Folder && itemTypes.HasFlag(EclItemTypes.Folder))
                    item = new S3Folder(_provider, _session, parentFolderUri, itemData);
                else if (itemTypes.HasFlag(EclItemTypes.File))
                    item = new S3File(_provider, _session, parentFolderUri, itemData);

                if (item != null) {
                    _provider.Cache(item);
                    items.Add(item);
                }
            }

            return _session.HostServices.CreateFolderContent(
                parentFolderUri,
                items,
                CanGetUploadMultimediaItemsUrl(parentFolderUri.PublicationId),
                CanSearch(parentFolderUri.PublicationId));
        }

        /// <summary>
        /// Gets the item with the specified <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" />.
        /// </summary>
        /// <param name="eclUri">The URI specifying the item to get.</param>
        /// <returns>
        /// The <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryItem" /> for the specified URI. If the item is not available or not accessible for the <see cref="T:Tridion.ExternalContentLibrary.V2.ITridionUser" />
        /// of the current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession" /> the provider must throw an exception.
        /// </returns>
        /// <remarks>
        /// To check if the user can read an item without having an exception thrown call <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.GetItems(System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.IEclUri})" /> with a single item and check if
        /// it is returned.
        /// </remarks>
        public IContentLibraryItem GetItem(IEclUri eclUri)
        {
            S3ListItem item = _provider.Cached(eclUri.ItemId, eclUri);

            if (item == null) {

                if (eclUri.ItemType == EclItemTypes.Folder)
                    item = new S3Folder(_provider, _session, eclUri);

                if (eclUri.ItemType == EclItemTypes.File)
                    item = new S3File(_provider, _session, eclUri);

                _provider.Cache(item);
            }

            return item as IContentLibraryItem;
        }

        /// <summary>
        /// Gets the items with the specified <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri">URIs</see> if they are available.
        /// </summary>
        /// <param name="eclUris">The <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri">URIs</see> of the items to retrieve.</param>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.List`1" /> of <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryItem" /> containing the items that exists and the <see cref="T:Tridion.ExternalContentLibrary.V2.ITridionUser" />
        /// of the current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession" /> has access to.
        /// </returns>
        /// <remarks>
        /// <para>If any other error than the item does not exist or is not accessible for the <see cref="T:Tridion.ExternalContentLibrary.V2.ITridionUser" />
        /// of the current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession" /> occurs, an exception must be thrown.</para>
        /// <para>The provider should expect this method to be called for the same item across multiple Tridion publications.</para></remarks>
        public IList<IContentLibraryItem> GetItems(IList<IEclUri> eclUris)
        {
            return eclUris.Select(u => GetItem(u)).ToList();
        }

        /// <summary>
        /// Gets the thumbnail image for the specified item in the specified size.
        /// </summary>
        /// <param name="eclUri">The <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /> identifying the item a thumbnail should be retrieved for.</param>
        /// <param name="maxWidth">The maximum width if the thumbnail in pixels.</param>
        /// <param name="maxHeight">The maximum height if the thumbnail in pixels.</param>
        /// <returns>
        /// A byte array containing the thumbnail image in PNG, JPG, or GIF format, or <c>null</c> if the default thumbnail generator should be used (see remarks).
        /// </returns>
        /// <remarks>
        /// <para>Caching if the generated thumbnail image should be done by the caller, not by the provider. The provider might cache any intermediate data it retrieves.
        /// <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.CreateThumbnailImage(System.Int32,System.Int32,System.IO.Stream,System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.IThumbnailOverlay})" /> and
        /// <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.CreateThumbnailImage(System.Int32,System.Int32,System.IO.Stream,System.Int32,System.Int32,System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.IThumbnailOverlay})" />
        /// can be used to generate the thumbnail.</para>
        /// <para>It is valid to return <c>null</c> if the item the thumbnail is requested returns an <see cref="T:Tridion.ExternalContentLibrary.V2.IContentResult" /> that can be used to retrieve a png, jpg, bmp, or gif image when <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryMultimediaItem.GetContent(System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.ITemplateAttribute})" /> is called.
        /// </para></remarks>
        /// <seealso cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.IsThumbnailAvailable" />
        public byte[] GetThumbnailImage(IEclUri eclUri, int maxWidth, int maxHeight)
        {
            if (_provider.IsImageType(eclUri)) {
                using (HttpClient client = new HttpClient()) {
                    try {
                        string uri = _provider.S3.GetMediaUrl(eclUri.ItemId);
                        MemoryStream stream = new MemoryStream(client.GetByteArrayAsync(uri).Result);

                        return _session.HostServices.CreateThumbnailImage(maxWidth, maxHeight, stream, null);
                    }
                    catch (Exception) {
                        return null;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets an URL to a page that can be used to upload new items on the external system.
        /// </summary>
        /// <param name="parentFolderUri">The <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /> of the parent folder the items should be uploaded to.</param>
        /// <returns>
        /// An URL to a page that can be used to upload items to the external system.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method should always return an URL for items that returns <c>true</c> from <see cref="P:Tridion.ExternalContentLibrary.V2.IFolderContent.CanGetUploadMultimediaItemsUrl" />,
        /// <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.CanGetUploadMultimediaItemsUrl" />, or <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.CanGetUploadMultimediaItemsUrl(System.Int32)" />.
        /// </para>
        /// <para>
        /// The retrieved URL can contain a token that is only valid for a limited time period.
        /// </para></remarks>
        public string GetUploadMultimediaItemsUrl(IEclUri parentFolderUri)
        {
            NameValueCollection queryString = HttpUtility.ParseQueryString(String.Empty);
            queryString["id"] = parentFolderUri.MountPointId;
            queryString["prefix"] = parentFolderUri.ItemId == "root" ? String.Empty : parentFolderUri.ItemId;

            return String.Format("/WebUI/Editors/ECL/S3ECLUpload/upload.aspx?{0}", queryString.ToString());
        }

        /// <summary>
        /// Gets an URL that can be used to view and potentially edit the item on the external system.
        /// </summary>
        /// <param name="eclUri">The URI of the <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryItem" /> to get the URI for.</param>
        /// <returns>
        /// An URL that can be used to view the Item.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method should always return an URL for items that returns <c>true</c> from <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.CanGetViewItemUrl" />.
        /// </para>
        /// <para>
        /// The retrieved URL can contain a token that is only valid for a limited time period.
        /// </para></remarks>
        public string GetViewItemUrl(IEclUri eclUri)
        {
            return _provider.S3.GetMediaUrl(eclUri.ItemId);
        }

        /// <summary>
        /// Performs a search for external Items.
        /// </summary>
        /// <param name="contextUri">The <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /> of the Folder or Mount Point the search should be performed from.</param>
        /// <param name="searchTerm">The term to search for.</param>
        /// <param name="pageIndex">The 0 based index of the page to retrieve if the search result supports pagination.</param>
        /// <param name="numberOfItems">The number of items the user requested as the search result. If the provider supports pagination</param>
        /// <returns>
        /// A list of <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem" /> matching the search term.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method should only be called when <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.CanSearch(System.Int32)" />, <see cref="P:Tridion.ExternalContentLibrary.V2.IFolderContent.CanSearch" />, or <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.CanSearch(System.Int32)" />
        /// is <c>true</c> for the item identified by the <paramref name="contextUri" />.
        /// </para>
        /// <para>
        /// Ideally the provider should perform the search recursively across all subfolders of the Folder or Mount Point identified by the <paramref name="contextUri" />. The search
        /// should include the title and metadata on the external item.
        /// </para></remarks>
        public IFolderContent Search(IEclUri contextUri, string searchTerm, int pageIndex, int numberOfItems)
        {
            if (searchTerm != null) {
                String prefix = contextUri.ItemId == "root" ? String.Empty : contextUri.ItemId;

                IList<IContentLibraryListItem> items = new List<IContentLibraryListItem>();

                String term = searchTerm.ToLowerInvariant();

                foreach (S3ItemData itemData in _provider.S3.GetListing(prefix, true)) {

                    if (itemData.Key.ToLowerInvariant().Contains(term)) {

                        S3ListItem item = null;

                        IEclUri parentUri = _provider.GetParentUri(_provider.GetUri(itemData, contextUri));

                        if (itemData.ItemType == S3ItemType.Folder)
                            item = new S3Folder(_provider, _session, parentUri, itemData);
                        else
                            item = new S3File(_provider, _session, parentUri, itemData);

                        if (item != null) {
                            _provider.Cache(item);
                            items.Add(item);
                        }
                    }
                }

                return _session.HostServices.CreateFolderContent(contextUri, 
                    items, 
                    CanGetUploadMultimediaItemsUrl(contextUri.PublicationId), 
                    CanSearch(contextUri.PublicationId));
            }

            throw new NotSupportedException();
        }

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
        /// </list></remarks>
        public string Dispatch(string command, string payloadVersion, string payload, out string responseVersion)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Notifies the provider that a stub Component has been created.
        /// </summary>
        /// <param name="eclUri">The <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /> of the external item the stub was created for.</param>
        /// <param name="tcmUri">The TCM URI of the stub component that was created.</param>
        /// <remarks>
        /// <para>
        /// This method will be called by the External Content Library when a SDL Tridion stub Component is created for an item stored on the
        /// external system. If the Provider does not need to perform any action when a stub component is created it should simply leave
        /// the method body empty.
        /// </para>
        /// <para>
        /// The <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession" /> specified when the External Content Library creates the <see cref="T:Tridion.ExternalContentLibrary.V2.IContentLibraryContext" />
        /// will always have the <see cref="P:Tridion.ExternalContentLibrary.V2.IEclSession.TridionUser" /> set to the user configured as the privileged user for the mount point in
        /// the <c>ExternalContentLibrary.xml</c> configuration file.
        /// </para>
        /// <para>
        /// If the external system supports transactions, it can use <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.GetTransactionPropagationToken" /> to
        /// get a token that can be used with <see cref="M:System.Transactions.TransactionInterop.GetTransactionFromTransmitterPropagationToken(System.Byte[])" /> to join
        /// a distributed transaction with the SDL Tridion system. If the Provider does not join the transaction, the data written
        /// to SDL Tridion will automatically be rolled back if the Provider throws an exception from the <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.StubComponentCreated(Tridion.ExternalContentLibrary.V2.IEclUri,System.String)" /> method. In this
        /// case, the data consistency is not guaranteed as the exception may have been thrown after the external system persisted
        /// the changes.
        /// </para></remarks>
        public void StubComponentCreated(IEclUri eclUri, string tcmUri)
        {
        }

        /// <summary>
        /// Notifies the provider that a stub Component has been deleted.
        /// </summary>
        /// <param name="eclUri">The <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /> of the external item the stub represented.</param>
        /// <param name="tcmUri">The TCM URI of the stub component that was deleted.</param>
        /// <remarks>
        /// <para>
        /// This method will be called by the External Content Library when a SDL Tridion stub Component is deleted for an item stored on the
        /// external system. If the Provider does not need to perform any action when a stub component is deleted it should simply leave
        /// the method body empty.
        /// </para>
        /// <para>
        /// If the external system supports transactions, it can use <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.GetTransactionPropagationToken" /> to
        /// get a token that can be used with <see cref="M:System.Transactions.TransactionInterop.GetTransactionFromTransmitterPropagationToken(System.Byte[])" /> to join
        /// a distributed transaction with the SDL Tridion system. If the Provider does not join the transaction, the data written
        /// to SDL Tridion will automatically be rolled back if the Provider throws an exception from the <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.StubComponentCreated(Tridion.ExternalContentLibrary.V2.IEclUri,System.String)" /> method. In this
        /// case, the data consistency is not guaranteed as the exception may have been thrown after the external system persisted
        /// the changes.
        /// </para></remarks>
        public void StubComponentDeleted(IEclUri eclUri, string tcmUri)
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}

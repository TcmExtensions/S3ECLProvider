using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Xml.Linq;
using HeyRed.Mime;
using S3ECLProvider.API;
using S3ECLProvider.Extensions;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider
{
    /// <summary>
    /// <see cref="S3File" /> represents a file stored in Amazon S3
    /// </summary>
    public class S3File : S3Folder, IContentLibraryMultimediaItem
    {
        private const string NamespaceUri = "ecl:S3";
        private const string RootElementName = "S3Metadata";

        /// <summary>
        /// Initializes a new instance of the <see cref="S3File"/> class.
        /// </summary>
        /// <param name="provider">Associated <see cref="S3Provider"/></param>
        /// <param name="session">Current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession"/></param>
        /// <param name="uri"><see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        public S3File(S3Provider provider, IEclSession session, IEclUri uri): base(provider, session, uri)
        {
        }

        /// <summary>
        /// Initialize a new <see cref="S3File" />
        /// </summary>
        /// <param name="provider">Associated <see cref="S3Provider"/></param>
        /// <param name="session">Current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession"/></param>
        /// <param name="parentUri">Parent <see cref="Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        /// <param name="eclObject"><see cref="T:S3ECLProvider.API.S3ItemData"/></param>
        /// <remarks>Creating a <see cref="S3File" /> directly from an <see cref="T:S3ECLProvider.API.S3ItemData"/></remarks>
        public S3File(S3Provider provider, IEclSession session, IEclUri parentUri, S3ItemData eclObject) : base(provider, session, parentUri, eclObject)
        {
        }

        /// <summary>
        /// Initialize a new <see cref="S3File" /> in a given context
        /// </summary>
        /// <param name="item"><see cref="S3File"/> to create a shallow clone from</param>
        public S3File(S3ListItem item, IEclUri contextUri) : base(item, contextUri)
        {
        }

        /// <summary>
        /// Gets the filename including extension; <c>null</c> if the filename is not available.
        /// </summary>
        public string Filename
        {
            get {
                return Title;
            }
        }

        /// <summary>
        /// Gets the height of the multimedia item in pixels.
        /// </summary>
        /// <remarks>
        /// The width and <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryMultimediaItem.Height" /> is typically used when embedding an item into a Component or Page.
        /// If an item cannot be embedded but only linked to using <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryMultimediaItem.GetDirectLinkToPublished(System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.ITemplateAttribute})" />, a Provider
        /// may return null.
        /// </remarks>
        public int? Height
        {
            get {
                return null;
            }
        }

        /// <summary>
        /// Gets the width of the multimedia item in pixels.
        /// </summary>
        /// <remarks>
        /// The <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryMultimediaItem.Width" /> and height is typically used when embedding an item into a Component or Page.
        /// If an item cannot be embedded but only linked to using <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryMultimediaItem.GetDirectLinkToPublished(System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.ITemplateAttribute})" />, a Provider
        /// may return null.
        /// </remarks>
        public int? Width
        {
            get {
                return null;
            }
        }

        /// <summary>
        /// Gets the MIME type of the external item, or <c>null</c> if the item does not have a MIME type.
        /// </summary>
        /// <remarks>
        /// For example, if an item represents a PNG image the MIME type is "image/png". If the item
        /// represents a video player including the playback UI, so not just the actual video stream, it will not have a
        /// MIME type and it will return null.
        /// </remarks>
        /// <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryMultimediaItem.GetContent(System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.ITemplateAttribute})" />
        public string MimeType
        {
            get {
                return MimeTypesMap.GetMimeType(Filename);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user can open this item.
        /// </summary>
        /// <remarks>
        /// If the Provider returns <c>true</c><see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.GetViewItemUrl(Tridion.ExternalContentLibrary.V2.IEclUri)" />.
        /// </remarks>
        public override bool CanGetViewItemUrl
        {
            get {
                // Images in S3 do support viewing
                return Provider.IsImageType(Id);
            }
        }

        /// <summary>
        /// Gets or sets a string with the metadata associated with the item on the external system, or <c>null</c> if no metadata is available.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        /// <remarks>
        /// <para>
        /// The string must represent a valid XML document. The exact format of the XML string is determined by the Provider.
        /// </para>
        /// <para>
        /// The property should only be changed if <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.CanUpdateMetadataXml" /> returns <c>true</c>. If <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.CanUpdateMetadataXml" /> returns <c>false</c>,
        /// the Provider must throw a <c>NotSupportedException</c> when the property is changed.
        /// </para>
        /// </remarks>
        public override string MetadataXml
        {
            get {
                XElement metadata = new XElement(
                    XName.Get(RootElementName, NamespaceUri),
                    new XElement("Bucket", _itemData.BucketName),
                    new XElement("Key", _itemData.Key),
                    new XElement("Size", _itemData.Size),
                    new XElement("LastModified", _itemData.LastModified),
                    new XElement("ETag", _itemData.ETag.Replace("\"", "")),
                    new XElement("MimeType", MimeType),
                    new XElement("Url", GetDirectLinkToPublished(null))
                );

                return metadata.ToString();
            }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets a definition of the fields in the <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.MetadataXml" />.
        /// Use <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.CreateSchemaDefinition(System.String,System.String)" /> to create an instance of <see cref="T:Tridion.ExternalContentLibrary.V2.ISchemaDefinition" />.
        /// </summary>
        public override ISchemaDefinition MetadataXmlSchema
        {
            get {
                ISchemaDefinition schema = Session.HostServices.CreateSchemaDefinition(RootElementName, NamespaceUri);

                schema.Fields.Add(Session.HostServices.CreateSingleLineTextFieldDefinition("Bucket", "Bucket Name", 1, 1));
                schema.Fields.Add(Session.HostServices.CreateSingleLineTextFieldDefinition("Key", "S3 Object Key", 1, 1));
                schema.Fields.Add(Session.HostServices.CreateNumberFieldDefinition("Size", "Size (bytes)", 1, 1));
                schema.Fields.Add(Session.HostServices.CreateDateFieldDefinition("LastModified", "Last Modified", 1, 1));
                schema.Fields.Add(Session.HostServices.CreateSingleLineTextFieldDefinition("ETag", "ETag (Media Version)", 1, 1));
                schema.Fields.Add(Session.HostServices.CreateSingleLineTextFieldDefinition("MimeType", "MIME type", 1, 1));
                schema.Fields.Add(Session.HostServices.CreateSingleLineTextFieldDefinition("Url", "Media URL", 1, 1));

                return schema;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:Tridion.ExternalContentLibrary.V2.IContentResult" /> that can be used to read the binary content of the item, or <c>null</c> if the item cannot be represented by a binary stream.
        /// </summary>
        /// <param name="attributes">Allows the passing of attributes from templating code to the Provider. The supported attributes
        /// are determined by the provider.</param>
        /// <returns>
        /// Use <see cref="T:Tridion.ExternalContentLibrary.V2.IContentResult" /> to read the binary content of the item, or <c>null</c> if the item cannot be represented by a binary stream.
        /// </returns>
        /// <seealso cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.CreateContentResult(System.IO.Stream,System.Int64,System.String)" />
        public IContentResult GetContent(IList<ITemplateAttribute> attributes)
        {
            using (HttpClient client = new HttpClient()) {
                try {
                    MemoryStream stream = new MemoryStream(client.GetByteArrayAsync(Provider.S3.GetMediaUrl(Id.ItemId)).Result);
                    return Session.HostServices.CreateContentResult(stream, stream.Length, MimeType);
                } catch (Exception) {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the URI that can be used from a public Web site to link directly to the item on the external system.
        /// </summary>
        /// <param name="attributes">Allows the passing of attributes from templating code to the Provider. The supported attributes
        /// are determined by the provider.</param>
        /// <returns>
        /// A valid URI that can be used to link to the external item, or <c>null</c> if external links are not supported.
        /// </returns>
        /// <remarks>
        /// The link returned is to a publicly available version. If the external system does not have its own publishing mechanism
        /// to make the item available, the Provider should return <c>null</c>. Use <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryMultimediaItem.GetContent(System.Collections.Generic.IList{Tridion.ExternalContentLibrary.V2.ITemplateAttribute})" /> to publish
        /// the item though the Tridion publisher.
        /// </remarks>
        public string GetDirectLinkToPublished(IList<ITemplateAttribute> attributes)
        {
            return _itemData.Url;
        }

        /// <summary>
        /// Gets a text fragment, typically an HTML img or div element, that can be used to embed the item on a page.
        /// </summary>
        /// <param name="attributes">Allows the passing of attributes from templating code to the Provider. The supported attributes
        /// are determined by the Provider.</param>
        /// <returns>
        /// A text fragment that represents the item inserted on a page, or <c>null</c> if the item does not support embedding on a page.
        /// </returns>
        public string GetTemplateFragment(IList<ITemplateAttribute> attributes)
        {
            string[] supportedAttributeNames = new[] { "style", "width", "height" };
            string supportedAttributes = attributes.SupportedAttributes(supportedAttributeNames);

            if (Provider.IsImageType(Id))
                return string.Format("<img src=\"{0}\" alt=\"{1}\" {2}/>", GetDirectLinkToPublished(attributes), Title, supportedAttributes);

            // Not an image type file
            return null;
        }

        /// <summary>
        /// Saves the item.
        /// </summary>
        /// <param name="readback">If set to <c>true</c>, the Provider must return the saved item.</param>
        /// <returns>
        /// If <paramref name="readback" /> is set to <c>true</c> the saved item is returned, else <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// When calling this method on a Provider that does not support the saving of items, the Provider must
        /// throw an <c>NotSupportedException</c>. For example, when the caller attempts to update one of the writeable properties
        /// such as <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.Title" />.
        /// </para>
        /// <para>
        /// If the <paramref name="readback" /> parameter is <c>true</c>, the <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.Save(System.Boolean)" /> method must return the updated item.
        /// For most Providers, this is simply the item itself (so return <c>this</c>), but if the external system supports changing
        /// some of the saved values, for example through an event system, the item should be retrieved from the external system after the
        /// save completes to ensure the latest values are available.
        /// </para>
        /// <para>
        /// If the <paramref name="readback" /> parameter is <c>false</c> the <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.Save(System.Boolean)" /> method must return <c>null</c>.
        /// </para>
        /// <para>
        /// If the external system supports transactions, it can use <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.GetTransactionPropagationToken" /> to
        /// get a token that can be used with <see cref="M:System.Transactions.TransactionInterop.GetTransactionFromTransmitterPropagationToken(System.Byte[])" /> to join
        /// a distributed transaction with the SDL Tridion system. If the Provider does not join the transaction, the data written
        /// to SDL Tridion will automatically be rolled back if the Provider throws an exception from the <see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.Save(System.Boolean)" /> method. In this
        /// case, the data consistency is not guaranteed as the exception may have been thrown after the external system persisted
        /// the changes.
        /// </para>
        /// </remarks>
        public override IContentLibraryItem Save(bool readback)
        {
            if (!String.IsNullOrEmpty(NewTitle) && !String.Equals(NewTitle, Title, StringComparison.InvariantCultureIgnoreCase)) {

                // Replace the last part of the key with the newly updated value
                String[] parts = _itemData.EclKey.Split(new char[] { '/' });
                parts[parts.Length - 1] = NewTitle;

                Provider.S3.RenameObject(_itemData.EclKey, String.Join("/", parts));

                // Remove item from cache
                Provider.Purge(this);
            }

            return readback ? this : null;
        }
    }
}

using System;
using S3ECLProvider.API;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider
{
    /// <summary>
    /// <see cref="S3Folder" /> represents a (virtual) folder stored in Amazon S3
    /// </summary>
    public class S3Folder : S3ListItem, IContentLibraryItem
    {
        /// <summary>
        /// Initialize a new <see cref="S3Folder" />
        /// </summary>
        /// <param name="provider">Associated <see cref="S3Provider"/></param>
        /// <param name="session">Current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession"/></param>
        /// <param name="uri"><see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        public S3Folder(S3Provider provider, IEclSession session, IEclUri uri) : base(provider, session, uri)
        {
        }

        /// <summary>
        /// Initialize a new <see cref="S3Folder" />
        /// </summary>
        /// <param name="provider">Associated <see cref="S3Provider"/></param>
        /// <param name="session">Current <see cref="T:Tridion.ExternalContentLibrary.V2.IEclSession"/></param>
        /// <param name="parentUri">Parent <see cref="T:Tridion.ExternalContentLibrary.V2.IEclUri" /></param>
        /// <param name="eclObject"><see cref="T:S3ECLProvider.API.S3ItemData"/></param>
        /// <remarks>Creating a <see cref="S3Folder" /> directly from an <see cref="Amazon.S3.Model.S3Object"/></remarks>
        public S3Folder(S3Provider provider, IEclSession session, IEclUri parentUri, S3ItemData eclObject): base(provider, session, parentUri, eclObject)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the user can open this item.
        /// </summary>
        /// <remarks>
        /// If the Provider returns <c>true</c><see cref="M:Tridion.ExternalContentLibrary.V2.IContentLibraryContext.GetViewItemUrl(Tridion.ExternalContentLibrary.V2.IEclUri)" />.
        /// </remarks>
        public virtual bool CanGetViewItemUrl {
            get {
                // Folders in S3 are virtual and do not support viewing
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user can modify the <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.MetadataXml" /> of the item.
        /// </summary>
        public bool CanUpdateMetadataXml {
            get {
                // Currently metadata in Amazon S3 is not update-able, though some properties could be
                // TODO: Evaluate allowing update of metadata
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user can modify the <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryListItem.Title" /> of the item.
        /// </summary>
        public bool CanUpdateTitle {
            get {
                return true;
            }
        }

        /// <summary>
        /// Gets the date the item was created, or <c>null</c> if the date is not available.
        /// </summary>
        public virtual DateTime? Created {
            get {
                // Amazon S3 does not support creation dates
                return null;
            }
        }

        /// <summary>
        /// Gets a string identifying the user that created the item on the external system, or <c>null</c> if the information is not available.
        /// </summary>
        public string CreatedBy
        {
            get {
                // Amazon S3 objects are created through an Amazon Web Services key and secret, the user is ID is always the same.
                // Retrieving it from the Amazon S3 API is an extra overhead.
                return null;
            }
        }

        /// <summary>
        /// Gets or sets a string with the metadata associated with the item on the external system, or <c>null</c> if no metadata is available.
        /// </summary>
        /// <exception cref="NotSupportedException">Amazon S3 Folders do not support metadata.</exception>
        /// <remarks>
        /// <para>
        /// The string must represent a valid XML document. The exact format of the XML string is determined by the Provider.
        /// </para>
        /// <para>
        /// The property should only be changed if <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.CanUpdateMetadataXml" /> returns <c>true</c>. If <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.CanUpdateMetadataXml" /> returns <c>false</c>,
        /// the Provider must throw a <c>NotSupportedException</c> when the property is changed.
        /// </para>
        /// </remarks>
        public virtual string MetadataXml {
            get {
                // Folders in Amazon S3 are virtual and do not have metadata
                return null;
            }
            set {
                throw new NotSupportedException("Amazon S3 Folders do not support metadata.");
            }
        }

        /// <summary>
        /// Gets a definition of the fields in the <see cref="P:Tridion.ExternalContentLibrary.V2.IContentLibraryItem.MetadataXml" />. Use <see cref="M:Tridion.ExternalContentLibrary.V2.IHostServices.CreateSchemaDefinition(System.String,System.String)" /> to create an instance of <see cref="T:Tridion.ExternalContentLibrary.V2.ISchemaDefinition" />.
        /// </summary>
        public virtual ISchemaDefinition MetadataXmlSchema {
            get {
                // Folders in Amazon S3 are virtual and do not have metadata
                return null;
            }
        }

        /// <summary>
        /// Gets a string identifying the user that last modified the item on the external
        /// system, or null if the information is not available.
        /// </summary>
        public string ModifiedBy {
            get {
                // Amazon S3 does not store the user who last modified an object, rather only the object owner
                return null;
            }
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
        public virtual IContentLibraryItem Save(bool readback)
        {
            if (!String.IsNullOrEmpty(NewTitle) && !String.Equals(NewTitle, Title, StringComparison.InvariantCultureIgnoreCase)) {

                // Replace the last part of the key with the newly updated value
                String[] parts = Id.ItemId.Split(new char[] { '/' });
                parts[parts.Length - 1] = NewTitle;

                Provider.S3.RenameFolder(Id.ItemId, String.Join("/", parts));

                // Remove item from cache
                Provider.Purge(this);
            }

            return readback ? this : null;
        }
    }
}

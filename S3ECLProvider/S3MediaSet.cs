using System;
using S3ECLProvider;
using S3ECLProvider.api;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider
{
    /// <summary>
    /// Represents a S3 (Photo) Set with all details loaded. 
    /// </summary>
    public class S3MediaSet : ListItem, IContentLibraryItem
    {
        public S3MediaSet(IEclUri ecluri, S3Info info) : base(ecluri, info)
        {
            // if info needs to be fully loaded, do so here
        }

        public bool CanGetViewItemUrl
        {
            get { return true; }
        }

        public bool CanUpdateMetadataXml
        {
            get { return false; }
        }

        public bool CanUpdateTitle
        {
            get { return false; }
        }

        public DateTime? Created
        {
            get { return Info.Created; }
        }

        public string CreatedBy
        {
            get { return S3Provider.S3.AccessId; }
        }

        public string MetadataXml
        {
            get { return null; }
            set { throw new NotSupportedException(); }
        }

        public ISchemaDefinition MetadataXmlSchema
        {
            get { return null; }
        }

        public string ModifiedBy
        {
            get { return CreatedBy; }
        }

        public IEclUri ParentId
        {
            get
            {
                // return mountpoint uri (we only have folders in the top level)
                return S3Provider.HostServices.CreateEclUri(Id.PublicationId, Id.MountPointId);
            }
        }

        public IContentLibraryItem Save(bool readback)
        {
            // as saving isn't supported, the result of saving is always the item itself
            return readback ? this : null;
        }
    }
}

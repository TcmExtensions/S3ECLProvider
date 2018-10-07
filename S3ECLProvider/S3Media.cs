using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using S3ECLProvider;
using S3ECLProvider.api;
using S3ECLProvider.Extensions;
using Tridion.ExternalContentLibrary.V2;
using System.Xml.Linq;

namespace S3ECLProvider
{
    /// <summary>
    /// Represents a S3 Photo with all details loaded. 
    /// </summary>
    public class S3Media : ListItem, IContentLibraryMultimediaItem 
    {
        private const string NamespaceUri = "http://s3.com/services/api";
        private const string RootElementName = "Metadata";   
        
        public S3Media(IEclUri ecluri, S3Info info) : base(ecluri, info)
        {
        }

        public S3Media(IEclUri ecluri) : base(ecluri, null)
        {
        }

        public string Filename
        {
            get
            {
                return Info.Name;             
            }
        }

        public IContentResult GetContent(IList<ITemplateAttribute> attributes)
        {            
            return null;
        }

        public string GetDirectLinkToPublished(IList<ITemplateAttribute> attributes)
        {
            return Info.MediaUrl;
        }

        public string GetTemplateFragment(IList<ITemplateAttribute> attributes)
        {
            string[] supportedAttributeNames = new[] { "style", "width", "height" };
            string supportedAttributes = attributes.SupportedAttributes(supportedAttributeNames);
          
            return string.Format("<img src=\"{0}\" alt=\"{1}\" {2}/>", GetDirectLinkToPublished(attributes), Title, supportedAttributes);
        }

        public int? Height
        {
            get { return S3.MaxHeight; }
        }

        public string MimeType
        {
            get { return Info.MIMEType; }
        }

        public int? Width
        {
            get { return S3.MaxWidth; }
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
            get {
                // TODO: Obtain creator ID from file listing
                return "Unknown";
            }
        }


        public string MetadataXml
        {
            get
            {
                return string.Format(
  "<{0} xmlns=\"{1}\"><Size>{2}</Size><Filename>{3}</Filename><MimeType>{4}</MimeType><S3Url>{5}</S3Url><ETag>{6}</ETag><MetadataId>{7}</MetadataId></{0}>",
                RootElementName, NamespaceUri, Info.Size, Filename, MimeType, Info.MediaUrl, Info.ETag, Info.newGuid);
            }
            set { throw new NotSupportedException(); }
        }
        

        public ISchemaDefinition MetadataXmlSchema
        {
            get
            {
                ISchemaDefinition schema = S3Provider.HostServices.CreateSchemaDefinition(RootElementName, NamespaceUri);
                schema.Fields.Add(S3Provider.HostServices.CreateSingleLineTextFieldDefinition("Filename", "FileKey", 0, 1));
                schema.Fields.Add(S3Provider.HostServices.CreateNumberFieldDefinition("MetadataId", "FileKey based Guid", 0, 1));                
                schema.Fields.Add(S3Provider.HostServices.CreateSingleLineTextFieldDefinition("S3Url", "Media URL", 0, 1));
                schema.Fields.Add(S3Provider.HostServices.CreateNumberFieldDefinition("Size", "Size (KB)", 0, 1));
                schema.Fields.Add(S3Provider.HostServices.CreateSingleLineTextFieldDefinition("MimeType", "MIME type", 0, 1));
                schema.Fields.Add(S3Provider.HostServices.CreateNumberFieldDefinition("ETag", "ETag (Media Viersion)", 0, 1));

                return schema;
            }
        }

        public string ModifiedBy
        {
            get { return CreatedBy; }
        }

        public IEclUri ParentId
        {
            get
            {              
                return S3Provider.HostServices.CreateEclUri(Id.PublicationId, Id.MountPointId);              
            }
        }

        public IContentLibraryItem Save(bool readback)
        {          
            return readback ? this : null;
        }
    }
}

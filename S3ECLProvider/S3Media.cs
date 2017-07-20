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
using HtmlAgilityPack;

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
            // if info needs to be fully loaded, do so here            
            //_id = S3Provider.HostServices.CreateEclUri(publicationId, S3Provider.MountPointId, info.MediaUrl, "vid", EclItemTypes.File);
        }

        public S3Media(IEclUri ecluri) : base(ecluri, null)
        {
        }

        public string Filename
        {
            get
            {
                return Info.Name;
                //string url = Info.MediaUrl;//S3.GetPhotoUrl(Info, PhotoSizeEnum.Medium);
                //return url.Substring(url.LastIndexOf('/') + 1);
            }
        }

        public IContentResult GetContent(IList<ITemplateAttribute> attributes)
        {
            // in case we want SDL Tridion to publish the item, we should return the content stream for this S3 photo
            //using (WebClient webClient = new WebClient())
            //{
            //    using (Stream stream = new MemoryStream(webClient.DownloadData(Info.Url)))
            //    {
            //        return Provider.HostServices.CreateContentResult(stream, stream.Length, MimeType);
            //    }                
            //}

            // S3 photos are already published, so we can return null here
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

            // S3 photos are already published, so we can provide a template fragment ourselves
            return string.Format("<img src=\"{0}\" alt=\"{1}\" {2}/>", GetDirectLinkToPublished(attributes), Title, supportedAttributes);
        }

        public int? Height
        {
            get { return S3.MaxHeight; }
        }

        public string MimeType
        {
            get { return Info.MIMEType; } //Info.ContentType; 
            //return Info.IsFolder ? "" : Info.IsPdf ? "" : Info.IsPhoto ? "" : Info.IsVideo ? "" : Info.IsOther ? ""; } //"image/jpeg"; }
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
            get { return false; } //default false
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
                //schema.Fields.Add(S3Provider.HostServices.CreateMultiLineTextFieldDefinition("Description", "Description", 0, 1, 5));

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
                // return folder uri (S3 photoset)
                return S3Provider.HostServices.CreateEclUri(Id.PublicationId, Id.MountPointId);
                //return S3Provider.HostServices.CreateEclUri(
                //    Id.PublicationId,
                //    Id.MountPointId,
                //    Info.MediaUrl.TrimEnd(Id.ItemId.ToCharArray()), //S3.FullBucketUrl,   //"https://s3-us-west-2.amazonaws.com/"+ Info.Bucket +"/", //com-sdldev-tridion-s3ecl-vikas/
                //    DisplayTypeId,    //set
                //    EclItemTypes.Folder);
            }
        }

        public IContentLibraryItem Save(bool readback)
        {
            // as saving isn't supported, the result of saving is always the item itself
            return readback ? this : null;
        }
    }
}

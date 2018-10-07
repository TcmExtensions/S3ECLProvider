using System;
using S3ECLProvider.api;
using S3ECLProvider.Extensions;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider
{
    /// <summary>
    /// Represents the basic properties of a S3 (Photo) Set. 
    /// </summary>
    /// 

    #region    S3 Supported MIME Types
    /*
        image/png
        image/jpeg
        image/gif
        image/bmp
        image/tiff

        text/plain
        text/rtf

        application/msword
        application/zip

        audio/mpeg

        application/pdf
        application/x-zip
        application/x-compressed
        application/zip
    */
    #endregion


    public class ListItem : IContentLibraryListItem
    {
        internal readonly S3Info Info;
        private readonly IEclUri _id;
        public ListItem(IEclUri ecluri, S3Info info)
        {              
            Info = info;

            if (Info.ContentType == "Folder")
            {
                string itemId = Info.Name;
                String.Format(itemId);
                _id = S3Provider.HostServices.CreateEclUri(ecluri.PublicationId, S3Provider.MountPointId, itemId, DisplayTypeId, EclItemTypes.Folder);
            }            
            else
            {
                string itemId = Info.Name;
                _id = S3Provider.HostServices.CreateEclUri(ecluri.PublicationId, S3Provider.MountPointId, itemId, DisplayTypeId, EclItemTypes.File);
            }
        }
     
        public bool CanGetUploadMultimediaItemsUrl
        {
            get { return true; }
        }
       
        public bool CanSearch
        {
            get { return true; }
        }

        public string DisplayTypeId
        {
            get
            {
                if(Info.ContentType == "Folder")
                {
                    return Constants.S3_FOLDER_ID;
                }
                else
                {
                    return Constants.S3_FILE_ID;
                }             
            }
        }

        public string IconIdentifier
        {
            get { return null; }
        }

        public IEclUri Id
        {
            get { return _id; }
        }

        public bool IsThumbnailAvailable
        {
            get { return true; }
        }

        public DateTime? Modified
        {
            get { return Info.LastModified; }
        }

        public string ThumbnailETag
        {
            get { return Modified != null ? Modified.Value.ETag() : Info.Created.Value.ETag(); }
        }

        public bool CanUpdateTitle
        {
            get { return true; } 
        }

      
        public string Title
        {
            get
            {               
                if (Info.ContentType == "Folder")
                {
                    var nameArray = Info.Name.Split('/');
                    int count = nameArray.Length;
                    return nameArray[count - 2].TrimEnd('/');
                }
                else
                {
                    var nameArray = Info.Name.Split('/');
                    int count = nameArray.Length;
                    return nameArray[count - 1];
                }

            }
            set { throw new NotSupportedException(); }
        }
       
        public virtual string Dispatch(string command, string payloadVersion, string payload, out string responseVersion)
        {
            throw new NotSupportedException();
        }
    }
}

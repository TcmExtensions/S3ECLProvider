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
        //private readonly string hifen = "-";


        //public ListItem(int publicationId, S3Info info)
        public ListItem(IEclUri ecluri, S3Info info)
        {
            // S3 s3 = new S3();            
            Info = info;//s3.GetMediaInfo(info.Name);

            #region ContentTypes regiosn - Note: Keep updating this list and build the code. // This could be improved in a better way.
            //switch (Info.ContentType)
            //{
            //    case "image/png":
            //    case "image/jpeg":
            //    case "image/jpg":
            //    case "image/gif":
            //    case "image/bmp":
            //    case "image/tiff":
            //    case "image/x-icon":

            //        Info.IsPhoto = true;
            //        break;

            //    case "video/x-ms-wmv":
            //    case "application/octet-stream":
            //        Info.IsVideo = true;
            //        break;

            //    case "application/pdf":
            //    case "application/x-zip":
            //    case "application/x-compressed":
            //    case "application/zip":
            //    case "application/msword":
            //        Info.IsPdf = true;
            //        break;

            //    case "binary/octet-stream":
            //    case "application/x-www-form-urlencoded; charset=utf-8":
            //    case "application/x-directory":
            //    //case "application/x-amz-json-1.0":
            //    case null:
            //        Info.IsFolder = true;
            //        break;

            //    default:
            //        Info.IsOther = true;
            //        break;
            //}

            #endregion

            //if (Info.IsFolder)
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

        // for folders only
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
                    return "fld";
                }
                else
                {
                    return "fls";
                }
               //return Info.IsFolder ? "fld" : "fls";
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
            get { return true; } //default false
        }

        //below Property allowed me to set the name as I wanted in Tridion CME
        public string Title
        {
            get
            {
                //return Info.Name;
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

        // allow override of dispatch
        public virtual string Dispatch(string command, string payloadVersion, string payload, out string responseVersion)
        {
            throw new NotSupportedException();
        }
    }
}

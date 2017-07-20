using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using S3ECLProvider.api;
using Tridion.ExternalContentLibrary.V2;
using Tridion.ContentManager.CoreService.Client;
using System.ServiceModel;
using System.Security.Cryptography;

namespace S3ECLProvider
{
    class S3MountPoint : IContentLibraryContext
    {
        internal static List<S3Info> InfoList;
        internal static S3Info Info;
        public static ITridionUser _tridionUser; //private readonly ITridionUser _tridionUser;
        public static IEclSession _session;

        public S3MountPoint(IEclSession session)
        {
            _tridionUser = session.TridionUser;
            _session = session;
        }

        public bool CanGetUploadMultimediaItemsUrl(int publicationId)
        {
            return true;
        }

        public bool CanSearch(int publicationId)
        {
            return true; //false;
        }

        public IList<IContentLibraryListItem> FindItem(IEclUri eclUri)
        {
            // return null so we force it to call GetItem(IEclUri)

            return null;
        }

        public IFolderContent GetFolderContent(IEclUri parentFolderUri, int pageIndex, EclItemTypes itemTypes)
        {
            bool canSearch = false; //CanSearch(parentFolderUri.PublicationId);
            if (parentFolderUri.ItemId == "root") // With this condition, Search will only work at Root Stub
            {
                canSearch = true;
            }
            List<IContentLibraryListItem> items = new List<IContentLibraryListItem>();
            InfoList = S3Provider.S3.GetDirectories(parentFolderUri, itemTypes);
            foreach (S3Info info in InfoList)
            {
                Info = info;
                items.Add(new ListItem(parentFolderUri, info));
            }
            return S3Provider.HostServices.CreateFolderContent(parentFolderUri, items, CanGetUploadMultimediaItemsUrl(parentFolderUri.PublicationId), canSearch);
        }



        public IContentLibraryItem GetItem(IEclUri eclUri)
        {            
            if (eclUri.ItemType == EclItemTypes.File && eclUri.SubType == "fls")
            {
                return new S3Media(eclUri, S3Provider.S3.GetMediaInfo(eclUri));
                //return new S3Media(eclUri, Info);
            }

            if (eclUri.ItemType == EclItemTypes.Folder && eclUri.SubType == "fld")
            {
                return new S3Media(eclUri, S3Provider.S3.GetMediaInfo(eclUri));
                //return new S3Media(eclUri, Info);
            }

            throw new NotSupportedException();
        }

        public IList<IContentLibraryItem> GetItems(IList<IEclUri> eclUris)
        {
            List<IContentLibraryItem> items = new List<IContentLibraryItem>();


            IEnumerable<string> uniquePhotoIds = (from uri in eclUris
                                                      //where uri.ItemType == EclItemTypes.File && (uri.SubType == "fls" || uri.SubType == "img" || uri.SubType == "vid" || uri.SubType == "pdf")
                                                  where uri.ItemType == EclItemTypes.File && (uri.SubType == "fls")
                                                  select uri.ItemId).Distinct();
            foreach (string id in uniquePhotoIds)
            {
                string itemId = id;
                var urisForPhoto = from uri in eclUris
                                       //where uri.ItemType == EclItemTypes.File && (uri.SubType == "fls" || uri.SubType == "img" || uri.SubType == "vid" || uri.SubType == "pdf") && uri.ItemId == itemId
                                   where uri.ItemType == EclItemTypes.File && (uri.SubType == "fls") && uri.ItemId == itemId
                                   select uri;

                foreach (IEclUri eclUri in urisForPhoto)
                {
                    items.Add(GetItem(eclUri));
                }
            }
            return items;
        }

        public byte[] GetThumbnailImage(IEclUri eclUri, int maxWidth, int maxHeight)
        {
            if (eclUri.ItemType == EclItemTypes.File && (eclUri.SubType == "fls"))
            {
                WebClient webClient = new WebClient();
                //S3Info photoUrl = S3Provider.S3.GetMediaInfo(eclUri.ItemId);
                string photoUrl = S3Provider.S3.GetMediaUrl(eclUri.ItemId);
                byte[] thumbnailDataIs = null;
                try
                {                    
                    //thumbnailDataIs = webClient.DownloadData(photoUrl.MediaUrl);
                    thumbnailDataIs = webClient.DownloadData(photoUrl);
                    //thumbnailDataIs = webClient.DownloadData(S3.mediaUrlForThumbnail);
                    using (MemoryStream ms = new MemoryStream(thumbnailDataIs, false))
                    {
                        return S3Provider.HostServices.CreateThumbnailImage(maxWidth, maxHeight, ms, null);
                    }
                }
                catch (WebException)
                {                   
                    return null;
                }
                catch (Exception)
                {
                    //throw new NotSupportedException();
                    return null;
                }
            }

            return null;
        }

        public string GetUploadMultimediaItemsUrl(IEclUri parentFolderUri)
        {
            if (parentFolderUri.ItemType == EclItemTypes.MountPoint)
            {
                return Info.MediaUrl;
            }

            if (parentFolderUri.ItemType == EclItemTypes.Folder && parentFolderUri.SubType == "fld")
            {
                return Info.MediaUrl; 
            }

            throw new NotSupportedException();
        }

        public string GetViewItemUrl(IEclUri eclUri)
        {
            return S3Provider.S3.GetMediaUrl(eclUri.ItemId);           
            throw new NotSupportedException();
        }

        public string IconIdentifier
        {
            get { return "S3"; }
        }

        public IFolderContent Search(IEclUri contextUri, string searchTerm, int pageIndex, int numberOfItems)
        {    
            if(searchTerm != null)
            {
                List<IContentLibraryListItem> items = new List<IContentLibraryListItem>();

                //Use SearchInS3Folders() method when only want to search in specific Folder
                //Use SearchInS3() method when searchin in whole S3, Could be slow based on items in S3.
                InfoList = S3Provider.S3.SearchInS3(contextUri, contextUri.ItemType, searchTerm); 
                foreach (S3Info info in InfoList)
                {
                    Info = info;
                    items.Add(new ListItem(contextUri, info));
                }
                return S3Provider.HostServices.CreateFolderContent(contextUri, items, CanGetUploadMultimediaItemsUrl(contextUri.PublicationId), true);
            }

            throw new NotSupportedException();
            //throw new InvalidOperationException("Enter search term..");
        }

        public string Dispatch(string command, string payloadVersion, string payload, out string responseVersion)
        {
            throw new NotSupportedException();
        }

        public void StubComponentCreated(IEclUri eclUri, string tcmUri)
        {
        }

        public void StubComponentDeleted(IEclUri eclUri, string tcmUri)
        {
        }

        public void Dispose()
        {
        }
    }
}

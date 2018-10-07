using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using S3ECLProvider.Extensions;
using System;
using System.Linq;
using System.Collections.Generic;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider.api
{
    class S3
    {
        private static IAmazonS3 _S3Client;
        private static string _bucketName;

        public const int MaxWidth = 3840;
        public const int MaxHeight = 2160;

        #region properties
        public static string FullBucketUrl { get; private set; }

        public static string mediaUrlForThumbnail { get; set; }
        #endregion

        #region constructors
        public S3(string region, string bucketName, string accessId, string secretKey, string fullBucketUrl)
        {
            _bucketName = bucketName;
            FullBucketUrl = fullBucketUrl;

            Amazon.RegionEndpoint regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);

            if (regionEndpoint == null)
                throw new ArgumentException(String.Format("Unknown S3 region: \"{0}\".", region), "region");

            _S3Client = new AmazonS3Client(accessId, secretKey, regionEndpoint);

            if (!_S3Client.DoesS3BucketExist(bucketName))
                throw new ArgumentException(String.Format("S3 bucket \"{0}\" does not exist or is not accessible.", bucketName), "bucketName");
        }
        #endregion

        public string GetMediaUrl(string mediaKey)
        {
            S3FileInfo mediaInfo = new S3FileInfo(_S3Client, _bucketName, mediaKey);

            if (mediaInfo.Exists)
            {
                var mediaUrl = FullBucketUrl + mediaKey;
                mediaUrlForThumbnail = mediaUrl;

                if (mediaUrl.Contains("%"))
                {
                    mediaUrl = mediaUrl.Replace("%", "%25");
                }

                return mediaUrl;
            }
            else
            {
                return "NOT FOUND";
            }

        }

        public S3Info GetMediaInfo(IEclUri eclUri)
        {
            GetObjectResponse s3Obj;
            var mediaKey = eclUri.ItemId;
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = eclUri.ItemId,
            };

            try
            {
                s3Obj = _S3Client.GetObject(request);
                var mediaUrl = GetMediaUrl(mediaKey);
              
                S3Info s3Info = new S3Info(s3Obj, mediaUrl, eclUri.ItemType.ToString());
                return s3Info;
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException();               
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //Whole S3 Bucket Search
        public List<S3Info> SearchInS3(IEclUri parentFolderUri, EclItemTypes itemTypes, string searchTerm = null)
        {
            List<S3Info> s3SearchList = new List<S3Info>();
            ListObjectsRequest objRequest = new ListObjectsRequest();
            objRequest.BucketName = _bucketName;           
            var ItemTypes = "";
            var returnedKey = "";
            //TODO: This pick all objects/items from s3 to search, where we should target to get object based on folder we are in
            ListObjectsResponse objResponse = _S3Client.ListObjects(objRequest);            
            foreach (S3Object obS3Object in objResponse.S3Objects)
            {
                if (obS3Object.Size == 0)
                {
                    ItemTypes = "Folder";
                    var nameArray = obS3Object.Key.Split('/');
                    int count = nameArray.Length;
                    returnedKey = nameArray[count - 2].TrimEnd('/');
                }
                else
                {
                    ItemTypes = "File";
                    var nameArray = obS3Object.Key.Split('/');
                    int count = nameArray.Length;
                    returnedKey = nameArray[count - 1];

                }
                if (returnedKey.Contains(searchTerm))
                {
                    var itemUrl = FullBucketUrl + obS3Object.Key;
                    s3SearchList.Add(new S3Info(obS3Object, itemUrl, ItemTypes));
                }
            }

            return s3SearchList;
        }

        //Search based on Directory
        public List<S3Info> SearchInS3Folders(IEclUri parentFolderUri, EclItemTypes itemTypes, string searchTerm = null)
        {
            List<S3Info> s3SearchList = new List<S3Info>();
            S3DirectoryInfo s3Root = null;        
            if (parentFolderUri.ItemId == "root")
            {
                s3Root = new S3DirectoryInfo(_S3Client, _bucketName);
            }
            else
            {
                s3Root = new S3DirectoryInfo(_S3Client, _bucketName, parentFolderUri.ItemId.Replace('/', '\\').TrimEnd('/'));
            }

            /*
            //Search Folder
            foreach (var subdirectories in s3Root.GetDirectories())
            {             
                if (subdirectories.Name.Contains(searchTerm))
                {
                    var item = subdirectories.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    s3SearchList.Add(new S3Info(subdirectories, itemUrl, "Folder"));
                }
            }

            //Search Files
            foreach (var file in s3Root.GetFiles())
            {                
                if (file.Name.Contains(searchTerm))
                {
                    var item = file.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    s3SearchList.Add(new S3Info(file, itemUrl, "File"));
                }
            }
            */
            return s3SearchList;
        }

        public List<S3Info> GetDirectories(IEclUri parentFolderUri, EclItemTypes itemTypes)
        {
            List<S3Info> s3List = new List<S3Info>();
            List<S3Info> myList = new List<S3Info>();
            S3DirectoryInfo s3Root = null;

            if (parentFolderUri.ItemId == "root")
            {
                s3Root = new S3DirectoryInfo(_S3Client, _bucketName);
            }
            else
            {              
                s3Root = new S3DirectoryInfo(_S3Client, _bucketName, parentFolderUri.ItemId.Replace('/', '\\').TrimEnd('/'));
            }

            /*
            if (parentFolderUri.ItemType == EclItemTypes.MountPoint && itemTypes.HasFlag(EclItemTypes.Folder) && !itemTypes.HasFlag(EclItemTypes.File))
            {
                foreach (var subdirectories in s3Root.GetDirectories())
                {
                    var item = subdirectories.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    myList.Add(new S3Info(subdirectories, itemUrl, "Folder"));
                }
            }
            else */

            if (itemTypes.HasFlag(EclItemTypes.File) && itemTypes.HasFlag(EclItemTypes.Folder))
            {
                myList.AddRange(s3Root.GetDirectories().Select(d => new S3Info(d, FullBucketUrl)));
                myList.AddRange(s3Root.GetFiles().Select(f => new S3Info(f, FullBucketUrl)));
            }
            /*
            else if (itemTypes.HasFlag(EclItemTypes.Folder))
            {

                foreach (var subdirectories in s3Root.GetDirectories())
                {
                    var item = subdirectories.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    myList.Add(new S3Info(subdirectories, itemUrl, "Folder"));
                }
            }
            */
            /*
            else if (itemTypes.HasFlag(EclItemTypes.File))
            {

                foreach (var file in s3Root.GetFiles())
                {
                    var item = file.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    myList.Add(new S3Info(file, itemUrl, "File"));
                }
            }
            */
            else
            {
                throw new NotSupportedException();
            }          
            return myList;
        }
        public static string GetPhotoUrl(S3Info photo, PhotoSizeEnum size)
        {
            string baseUrl = photo.MediaUrl;

            string url = string.Format("{0}?{1}", baseUrl, size.Description());

            return url;
        }
        public static string GetPhotoUrl(S3Info photo, int width = MaxWidth)
        {
            if (width >= MaxWidth)
            {
                // width 1024, height 768
                return GetPhotoUrl(photo, PhotoSizeEnum.Large);
            }
            if (width >= 800)
            {
                // width 800, height 600
                return GetPhotoUrl(photo, PhotoSizeEnum.Svga);
            }
            if (width >= 640)
            {
                // width 640, height 480
                return GetPhotoUrl(photo, PhotoSizeEnum.Vga);
            }
            if (width >= 500)
            {
                // width 500, height 375
                return GetPhotoUrl(photo, PhotoSizeEnum.Medium);
            }
            if (width >= 320)
            {
                // width 320, height 240
                return GetPhotoUrl(photo, PhotoSizeEnum.Qvga);
            }
            if (width >= 240)
            {
                // width 240, height 180
                return GetPhotoUrl(photo, PhotoSizeEnum.Small);
            }
            if (width >= 150)
            {
                // width 150, height 150
                return GetPhotoUrl(photo, PhotoSizeEnum.LargeSquare);
            }
            if (width >= 100)
            {
                // width 100, height 75
                return GetPhotoUrl(photo, PhotoSizeEnum.Thumbnail);
            }
            // width 75, height 75
            return GetPhotoUrl(photo, PhotoSizeEnum.Square);
        }
    }
}

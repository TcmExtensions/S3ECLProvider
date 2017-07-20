using System;
using System.Collections.Generic;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using S3ECLProvider.Extensions;
using Tridion.ExternalContentLibrary.V2;

namespace S3ECLProvider.api
{
    class S3
    {
        private const string DefaultUrl = "https://s3-us-west-2.amazonaws.com/";
        private static IAmazonS3 s3Client;
        public const int MaxWidth = 3840;
        public const int MaxHeight = 2160;

        private const string NamespaceUri = "http://s3.com/services/api";
        private const string RootElementName = "Metadata";

        #region properties
        public static string BucketName { get; set; }
        public static string FullBucketUrl { get; set; }

        public static string mediaUrlForThumbnail { get; set; }
        public string SecretKey { get; }
        public string AccessId { get; }
        #endregion

        #region constructors
        public S3(string bucketName, string secretKey, string accessId, string fullBucketUrl)
        {
            BucketName = bucketName;
            SecretKey = secretKey;
            AccessId = accessId;

            FullBucketUrl = fullBucketUrl;
            s3Client = new AmazonS3Client(AccessId, SecretKey);
        }


        public S3(string secretKey, string accessId)
              : this(BucketName, secretKey, accessId, FullBucketUrl)
        {
        }
        #endregion

        public S3()
        { }
        public string GetMediaUrl(string mediaKey)
        {
            S3FileInfo mediaInfo = new S3FileInfo(s3Client, BucketName, mediaKey);
            if (mediaInfo.Exists)
            {
                var mediaUrl = FullBucketUrl + mediaKey; //DefaultUrl + BucketName + "/" + mediaKey;
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
                BucketName = BucketName,
                Key = eclUri.ItemId,
            };
            try
            {
                s3Obj = s3Client.GetObject(request);
                var mediaUrl = GetMediaUrl(mediaKey);
                //s3List.Add(new S3Info(objectDirResponse, itemUrl, "Folder"));
                S3Info s3Info = new S3Info(s3Obj, mediaUrl, eclUri.ItemType.ToString());

                //mediaUrl = GetMediaUrl(mediaKey);
                //S3Info s3Info = new S3Info(s3Obj, mediaUrl);

                return s3Info;
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException();
                //mediaKey = mediaKey + "/";
                //s3Obj = s3Client.GetObject(request);
                //mediaUrl = GetMediaUrl(eclUri.ItemId);

                //S3Info s3Info = new S3Info(s3Obj, mediaUrl);
                //return s3Info;
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
            objRequest.BucketName = BucketName;
            //objRequest.Delimiter = "/";
            //objRequest.Prefix = searchTerm;
            var ItemTypes = "";
            var returnedKey = "";
            //TODO: This pick all objects/items from s3 to search, where we should target to get object based on folder we are in
            ListObjectsResponse objResponse = s3Client.ListObjects(objRequest);            
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
            //var ItemTypes = "";
            //var returnedKey = "";
            if (parentFolderUri.ItemId == "root")
            {
                s3Root = new S3DirectoryInfo(s3Client, BucketName);
            }
            else
            {
                s3Root = new S3DirectoryInfo(s3Client, BucketName, parentFolderUri.ItemId.Replace('/', '\\').TrimEnd('/'));
            }

            //Search Folder
            foreach (var subdirectories in s3Root.GetDirectories())
            {
                //var nameArray = subdirectories.Name.Split('/');
                //int count = nameArray.Length;
                //returnedKey = subdirectories.Name;//nameArray[count - 2].TrimEnd('/');
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
            return s3SearchList;
        }

        public List<S3Info> GetDirectories(IEclUri parentFolderUri, EclItemTypes itemTypes)
        {
            List<S3Info> s3List = new List<S3Info>();
            List<S3Info> myList = new List<S3Info>();
            S3DirectoryInfo s3Root = null;
            GetObjectRequest getDirObjectRequest = null;
            if (parentFolderUri.ItemId == "root")
            {
                s3Root = new S3DirectoryInfo(s3Client, BucketName);
            }
            else
            {
                //{com-sdldev-tridion-s3ecl-vikas:\Home\} {com-sdldev-tridion-s3ecl-vikas:\Holi\}
                s3Root = new S3DirectoryInfo(s3Client, BucketName, parentFolderUri.ItemId.Replace('/', '\\').TrimEnd('/'));
            }



            if (parentFolderUri.ItemType == EclItemTypes.MountPoint && itemTypes.HasFlag(EclItemTypes.Folder) && !itemTypes.HasFlag(EclItemTypes.File))
            {
                foreach (var subdirectories in s3Root.GetDirectories())
                {
                    var item = subdirectories.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    myList.Add(new S3Info(subdirectories, itemUrl, "Folder"));
                }
            }


            else if (itemTypes.HasFlag(EclItemTypes.File) && itemTypes.HasFlag(EclItemTypes.Folder))
            {
                foreach (var subdirectories in s3Root.GetDirectories())
                {
                    var item = subdirectories.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    myList.Add(new S3Info(subdirectories, itemUrl, "Folder"));
                }
                foreach (var file in s3Root.GetFiles())
                {
                    var item = file.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    myList.Add(new S3Info(file, itemUrl, "File"));
                }

            }



            else if (itemTypes.HasFlag(EclItemTypes.Folder))
            {
                foreach (var subdirectories in s3Root.GetDirectories())
                {
                    var item = subdirectories.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    myList.Add(new S3Info(subdirectories, itemUrl, "Folder"));
                }
            }


            else if (itemTypes.HasFlag(EclItemTypes.File))
            {
                foreach (var file in s3Root.GetFiles())
                {
                    var item = file.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
                    var itemUrl = FullBucketUrl + item;
                    myList.Add(new S3Info(file, itemUrl, "File"));
                }

            }
            else
            {
                throw new NotSupportedException();
            }
            //return s3List;
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

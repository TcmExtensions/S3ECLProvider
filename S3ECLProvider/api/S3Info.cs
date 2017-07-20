using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Amazon.S3.Model;
using S3ECLProvider.Extensions;
using Amazon.S3.Transfer;
using Amazon.S3;
using Amazon.S3.IO;
using System.Security.Cryptography;

namespace S3ECLProvider.api
{
    //Basic S3 Media Properties:
    //Bucket:	com-sdldev-tridion-s3ecl-vikas
    //Name:	AmazonS3.png
    //Link:	//This is a public linkhttps://s3-us-west-2.amazonaws.com/com-sdldev-tridion-s3ecl-vikas/AmazonS3.png
    //Size:	5438
    //Last Modified:	Fri Feb 17 23:54:02 GMT+530 2017
    //Owner:	wcms.aws
    //ETag:	05b771fd98e81e251f8d169885af3191
    //Expiry Date:	None
    //Expiration Rule:	N/A

    //MetaData:
    //Key: Content-Type
    //value: image/png, video/x-ms-wmv, application/pdf , binary/octet-stream (this is folder)

    //Permission:
    //Grantee: Open/Download, View permission, Edit permission
    //Everyone: Open/Download

    //More Info: http://docs.aws.amazon.com/AmazonS3/latest/API/RESTCommonResponseHeaders.html


    public class S3Info
    {
        #region basic photo properties
        public string Bucket { get; private set; }
        public string Name { get; internal set; }
        public string MediaUrl { get; private set; }
        public long Size { get; private set; }
        public Owner Owner { get; private set; }
        public string ETag { get; private set; }
        public DateTime? Created { get; internal set; }
        public DateTime? LastModified { get; internal set; }

        public string Status { get; private set; }
        public string State { get; private set; }

        //MetaData
        public string ContentType { get; private set; }
        public string MIMEType { get; private set; }

        public string newGuid { get; private set; }

        //Permission
        public string objectOwner { get; internal set; }

        public bool IsFolder { get; internal set; }
        public bool IsPhoto { get; internal set; }
        public bool IsVideo { get; internal set; }
        public bool IsPdf { get; internal set; }
        public bool IsOther { get; internal set; }

        public bool IsPublic { get; private set; }


        #endregion

        #region default constructors
        /// <summary>
        /// Direct URL to the S3 photo
        /// </summary>
        public string Url { get; set; }
        //public string UrlThumb { get; set; }

        /// <summary>
        /// S3 Photo page url
        /// </summary>
        public string EditUrl { get; private set; }

        /// <summary>
        /// The S3 Set id this photo belongs to
        /// </summary>
        #endregion

        public S3Info(ListObjectsResponse objectResponse)
        {
            //return objectResponse;
        }

        public S3Info()
        {
        }


        public S3Info(GetObjectResponse objectResponse, string mediaUrl = null)
        {
            // set basic properties (for photos and sets)
            Bucket = objectResponse.BucketName;
            Name = objectResponse.Key;
            Size = objectResponse.Headers.ContentLength; //in bytes
            ETag = objectResponse.ETag.Split('"')[1];
            LastModified = objectResponse.LastModified;
            ContentType = objectResponse.Headers.ContentType;
            MIMEType = objectResponse.Headers.ContentType;
            MediaUrl = mediaUrl;
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(Name));
                newGuid = new Guid(hash).ToString("N");
            }

            //implement
            var Status = objectResponse.HttpStatusCode;
            var State = "To be implemented";

            State = objectResponse.Headers.GetType().IsPublic.ToString();
        }

        public S3Info(GetObjectResponse objectResponse, string mediaUrl, string fileType)
        {
            // set basic properties (for photos and sets)
            Bucket = objectResponse.BucketName;
            Name = objectResponse.Key;
            Size = objectResponse.Headers.ContentLength; //in bytes
            ETag = objectResponse.ETag.Split('"')[1];
            LastModified = objectResponse.LastModified;
            ContentType = fileType;//objectResponse.Headers.ContentType;
            MIMEType = objectResponse.Headers.ContentType;
            MediaUrl = mediaUrl;
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(Name));
                newGuid = new Guid(hash).ToString("N");
            }
        }

        public S3Info(S3Object s3Object)
        {
            Bucket = s3Object.BucketName;
            Name = s3Object.Key;
            Size = s3Object.Size;
            ETag = s3Object.ETag.Split('"')[1];
            LastModified = s3Object.LastModified;
            Owner = s3Object.Owner;
            ContentType = null;
            MIMEType = "NA";
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(Name));
                newGuid = new Guid(hash).ToString("N");
            }
            //MediaUrl = mediaUrl;
            IsFolder = false;
            IsPhoto = false;
            IsVideo = false;
            IsPdf = false;
            IsOther = false;
        }


        //Below model is only for search.
        public S3Info(S3Object s3Object, string mediaUrl, string fileType)
        {
            Bucket = s3Object.BucketName;
            Name = s3Object.Key;
            Size = s3Object.Size;
            ETag = s3Object.ETag.Split('"')[1];
            LastModified = s3Object.LastModified;
            Owner = s3Object.Owner;
            ContentType = fileType;
            MIMEType = "NA";
            MediaUrl = mediaUrl;
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(Name));
                newGuid = new Guid(hash).ToString("N");
            }
            
            IsFolder = false;
            IsPhoto = false;
            IsVideo = false;
            IsPdf = false;
            IsOther = false;

        }


        public S3Info(S3DirectoryInfo itemInfo, string mediaUrl, string fileType)
        {
            Bucket = itemInfo.Bucket.Name;
            Name = itemInfo.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/');
            MediaUrl = mediaUrl;
            ContentType = fileType;
            LastModified = itemInfo.LastWriteTimeUtc;
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(Name));
                newGuid = new Guid(hash).ToString("N");
            }
        }

        public S3Info(S3FileInfo itemInfo, string mediaUrl, string fileType)
        {
            Bucket = itemInfo.Directory.Bucket.Name;
            Name = itemInfo.FullName.Split(':')[1].Replace('\\', '/').TrimStart('/'); ;
            MediaUrl = mediaUrl;
            ContentType = fileType;
            LastModified = itemInfo.LastWriteTimeUtc;
            Size = itemInfo.Length;
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(Name));
                newGuid = new Guid(hash).ToString("N");
            }
        }





    }
}

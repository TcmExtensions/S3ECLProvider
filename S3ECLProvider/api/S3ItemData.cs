using System;
using Amazon.S3.Model;

namespace S3ECLProvider.API
{
    /// <summary>
    /// <see cref="S3ItemType" />
    /// </summary>
    public enum S3ItemType
    {
        /// <summary>
        /// Item is a folder
        /// </summary>
        Folder,
        /// <summary>
        /// Item is a file
        /// </summary>
        File
    }

    /// <summary>
    /// <see cref="S3ItemData" /> holds information retrieved from the Amazon S3 API for any item (virtual folder or object)
    /// </summary>
    public class S3ItemData
    {
        /// <summary>
        /// Gets AWS S3 Item Type
        /// </summary>
        /// <value>
        /// The AWS S3 Item Type
        /// </value>
        public S3ItemType ItemType { get; }

        /// <summary>
        /// Get AWS S3 Bucket Name
        /// </summary>
        /// <value>
        /// AWS S3 Bucket Name
        /// </value>
        public String BucketName { get; }

        /// <summary>
        /// Get AWS S3 Bucket Key
        /// </summary>
        /// <value>
        /// AWS S3 Bucket Key
        /// </value>
        public String Key { get; }

        /// <summary>
        /// Gets AWS S3 key as appearing in ECL (minus prefix)
        /// </summary>
        /// <value>
        ///AWS S3 ECL key.
        /// </value>
        public String EclKey { get; }

        /// <summary>
        /// Gets AWS S3 object size
        /// </summary>
        /// <value>
        /// AWS S3 object size in bytes
        /// </value>
        /// <remarks>Size is 0 for folders</remarks>
        public long Size { get; }

        /// <summary>
        /// Gets the AWS S3 last modified date
        /// </summary>
        /// <value>
        /// AWS S3 object last modified date
        /// </value>
        /// <remarks>Last modified date is null for folders</remarks>
        public DateTime? LastModified { get; }

        /// <summary>
        /// Gets the AWS S3 ETag value
        /// </summary>
        /// <value>
        /// AWS S3 ETag value
        /// </value>
        /// <remarks>ETag value is null for folders</remarks>
        public String ETag { get; }

        /// <summary>
        /// Gets the full URL to the AWS S3 object
        /// </summary>
        /// <value>
        /// AWS S3 Object Url
        /// </value>
        public String Url { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="S3ItemData"/> class.
        /// </summary>
        /// <param name="eclKey">ECL Key</param>
        /// <param name="bucketUrl">Full bucket Url</param>
        /// <param name="s3Object"><see cref="T:Amazon.S3.Model.S3Object"/></param>
        internal S3ItemData(String eclKey, String bucketUrl, S3Object s3Object)
        {
            ItemType = s3Object.StorageClass == "Folder" ? S3ItemType.Folder : S3ItemType.File;
            BucketName = s3Object.BucketName;
            Key = s3Object.Key;
            EclKey = eclKey;
            Size = s3Object.Size;
            LastModified = s3Object.LastModified;
            ETag = s3Object.ETag;
            Url = bucketUrl + s3Object.Key.Replace("%", "%25");
        }
    }
}
